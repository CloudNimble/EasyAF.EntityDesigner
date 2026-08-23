// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Property = Microsoft.Data.Entity.Design.Edmx.Entity.Property;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal class ScalarPropertyKeyChange : ViewModelChange
    {
        private readonly ScalarProperty _property;

        internal ScalarPropertyKeyChange(ScalarProperty property)
        {
            _property = property;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = _property.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from property: " + _property.Name);

            if (viewModel != null)
            {
                Property property = viewModel.ModelXRef.GetExisting(_property) as Property;
                Debug.Assert(property != null, "property != null");
                SetKeyPropertyCommand cmd = new SetKeyPropertyCommand(property, _property.EntityKey);
                CommandProcessor.InvokeSingleCommand(cpc, cmd);
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 210; }
        }
    }
}
