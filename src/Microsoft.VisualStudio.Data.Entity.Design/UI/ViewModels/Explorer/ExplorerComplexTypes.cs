// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    // <summary>
    //     Dummy element which contains the EntityTypes, EnumTypes and ComplexTypes
    // </summary>
    internal class ExplorerComplexTypes : EntityDesignExplorerEFElement
    {
        private readonly TypedChildList<ExplorerComplexType> _complexTypes =
            new TypedChildList<ExplorerComplexType>();

        public ExplorerComplexTypes(string name, EditingContext context, ExplorerEFElement parent)
            : base(context, null, parent)
        {
            if (name != null)
            {
                base.Name = name;
            }
        }

        public IList<ExplorerComplexType> ComplexTypes
        {
            get { return _complexTypes.ChildList; }
        }

        private void LoadComplexTypesFromModel()
        {
            // load children from model
            // note: have to go to parent to get this as this is a dummy node
            if (Parent.ModelItem is ConceptualEntityModel entityModel)
            {
                foreach (var child in entityModel.ComplexTypes())
                {
                    _complexTypes.Insert(
                        (ExplorerComplexType)
                        ModelToExplorerModelXRef.GetNewOrExisting(_context, child, this, typeof(ExplorerComplexType)));
                }
            }
        }

        protected override void LoadChildrenFromModel()
        {
            LoadComplexTypesFromModel();
        }

        protected override void LoadWpfChildrenCollection()
        {
            _children.Clear();
            foreach (var child in ComplexTypes)
            {
                _children.Add(child);
            }
        }

        protected override void InsertChild(EFElement efElementToInsert)
        {
            if (efElementToInsert is ComplexType complexType)
            {
                var explorerComplexType = AddComplexType(complexType);
                var index = _complexTypes.IndexOf(explorerComplexType);
                _children.Insert(index, explorerComplexType);
            }
            else
            {
                base.InsertChild(efElementToInsert);
            }
        }

        protected override bool RemoveChild(ExplorerEFElement efElementToRemove)
        {
            if (efElementToRemove is not ExplorerComplexType explorerComplexType)
            {
                Debug.Fail(
                    string.Format(
                        CultureInfo.CurrentCulture, Resources.BadRemoveBadChildType,
                        efElementToRemove.GetType().FullName, Name, GetType().FullName));
                return false;
            }

            var indexOfRemovedChild = _complexTypes.Remove(explorerComplexType);
            return (indexOfRemovedChild < 0) ? false : true;
        }

        private ExplorerComplexType AddComplexType(ComplexType complexType)
        {
            ExplorerComplexType explorerComplexType =
                ModelToExplorerModelXRef.GetNew(_context, complexType, this, typeof(ExplorerComplexType)) as ExplorerComplexType;
            _complexTypes.Insert(explorerComplexType);
            return explorerComplexType;
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "FolderPngIcon"; }
        }
    }
}