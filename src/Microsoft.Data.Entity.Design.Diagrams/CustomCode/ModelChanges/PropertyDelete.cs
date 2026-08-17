// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal class PropertyDelete : ViewModelChange
    {
        internal Property Property { get; private set; }
        internal EntityType EntityType { get; private set; }

        internal PropertyDelete(Property property)
        {
            Property = property;
            EntityType = property.EntityType;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = Property.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from property:" + Property.Name);

            if (viewModel != null)
            {
                Edmx.Entity.Property property = viewModel.ModelXRef.GetExisting(Property) as Edmx.Entity.Property;
                Debug.Assert(property != null);
                DeleteEFElementCommand.DeleteInTransaction(cpc, property);
                viewModel.ModelXRef.Remove(property, Property);
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 30; }
        }
    }
}
