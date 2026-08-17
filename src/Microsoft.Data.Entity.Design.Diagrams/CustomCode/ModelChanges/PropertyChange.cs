// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal class PropertyChange : ViewModelChange
    {
        private readonly Property _property;

        internal PropertyChange(Property property)
        {
            _property = property;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = _property.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from property: " + _property.Name);

            if (viewModel != null)
            {
                Edmx.Entity.Property property = viewModel.ModelXRef.GetExisting(_property) as Edmx.Entity.Property;
                Debug.Assert(property != null);

                Command c = new EntityDesignRenameCommand(property, _property.Name, true);
                CommandProcessor cp = new CommandProcessor(cpc, c);
                cp.Invoke();
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 210; }
        }
    }
}
