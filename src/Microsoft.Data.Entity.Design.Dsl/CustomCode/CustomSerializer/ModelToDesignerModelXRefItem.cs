// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.VisualStudio.Modeling;

namespace Microsoft.Data.Entity.Design.EntityDesigner.CustomSerializer
{
    /// <summary>
    ///     This class contains XRef between DSL Model and Escher Model.
    /// </summary>
    internal class ModelToDesignerModelXRefItem
    {
        private readonly Dictionary<EFObject, ModelElement> _modelToViewModel;
        private readonly Dictionary<ModelElement, EFObject> _viewModelToModel;

        internal ModelToDesignerModelXRefItem()
        {
            _modelToViewModel = [];
            _viewModelToModel = [];
        }

        internal void Add(EFObject obj, ModelElement viewElement, EditingContext context)
        {
            EntityDesignerViewModel viewModel = viewElement as EntityDesignerViewModel;
            viewModel?.EditingContext = context;

            Remove(obj);
            Remove(viewElement);

            _modelToViewModel.Add(obj, viewElement);
            _viewModelToModel.Add(viewElement, obj);
        }

        internal void Remove(EFObject obj, ModelElement viewElement)
        {
            _modelToViewModel.Remove(obj);
            _viewModelToModel.Remove(viewElement);
        }

        internal void Remove(ModelElement viewElement)
        {

            if (_viewModelToModel.TryGetValue(viewElement, out EFObject efObject))
            {
                Remove(efObject, viewElement);
            }
        }

        internal void Remove(EFObject efObject)
        {

            if (_modelToViewModel.TryGetValue(efObject, out ModelElement viewElement))
            {
                Remove(efObject, viewElement);
            }
        }

        internal ModelElement GetExisting(EFObject obj)
        {
            _modelToViewModel.TryGetValue(obj, out ModelElement result);
            return result;
        }

        internal EFObject GetExisting(ModelElement viewElement)
        {
            _viewModelToModel.TryGetValue(viewElement, out EFObject result);
            return result;
        }

        internal bool ContainsKey(EFObject obj)
        {
            return _modelToViewModel.ContainsKey(obj);
        }

        internal void Clear()
        {
            _modelToViewModel.Clear();
            _viewModelToModel.Clear();
        }

        internal ICollection<ModelElement> ReferencedViewElements
        {
            get
            {
                List<ModelElement> objects = new List<ModelElement>();
                foreach (var melem in _viewModelToModel.Keys)
                {
                    objects.Add(melem);
                }

                return objects.AsReadOnly();
            }
        }
    }
}
