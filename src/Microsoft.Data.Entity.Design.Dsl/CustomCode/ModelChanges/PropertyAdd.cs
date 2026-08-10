// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model.Commands;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class PropertyAdd : ViewModelChange
    {
        private readonly Property _property;

        internal PropertyAdd(Property property)
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
                Debug.Assert(entityType != null);
                Model.Entity.Property property = null;
                if (_property is ScalarProperty)
                {
                    property = CreatePropertyCommand.CreateDefaultProperty(cpc, _property.Name, entityType);
                    if (_property is ScalarProperty scalarProperty
                        && scalarProperty.EntityKey)
                    {
                        CommandProcessor.InvokeSingleCommand(cpc, new SetKeyPropertyCommand(property, true));
                    }
                }
                else
                {
                    property = CreateComplexPropertyCommand.CreateDefaultProperty(cpc, _property.Name, entityType);
                }
                viewModel.ModelXRef.Add(property, _property, viewModel.EditingContext);
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 110; }
        }
    }
}
