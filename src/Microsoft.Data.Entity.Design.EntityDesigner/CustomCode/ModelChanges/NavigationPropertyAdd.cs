// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using NavigationProperty = Microsoft.Data.Entity.Design.EntityDesigner.ViewModel.NavigationProperty;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class NavigationPropertyAdd : ViewModelChange
    {
        private readonly NavigationProperty _property;

        internal NavigationPropertyAdd(NavigationProperty property)
        {
            _property = property;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = _property.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from property: " + _property.Name);

            if (viewModel != null)
            {
                Model.Entity.EntityType entityType = viewModel.ModelXRef.GetExisting(_property.EntityType) as Model.Entity.EntityType;
                ConceptualEntityType cet = entityType as ConceptualEntityType;
                Debug.Assert(entityType != null ? cet != null : true, "EntityType is not ConceptualEntityType");
                Debug.Assert(entityType != null);

                if (cet != null)
                {
                    var property = CreateNavigationPropertyCommand.CreateDefaultProperty(cpc, _property.Name, cet);
                    viewModel.ModelXRef.Add(property, _property, viewModel.EditingContext);
                }
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 110; }
        }
    }
}
