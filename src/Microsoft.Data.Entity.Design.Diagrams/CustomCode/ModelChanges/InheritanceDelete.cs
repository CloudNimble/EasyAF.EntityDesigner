// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal class InheritanceDelete : InheritanceModelChange
    {
        internal InheritanceDelete(Inheritance inheritance)
            : base(inheritance)
        {
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = Inheritance.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from inheritance: " + Inheritance);
            if (viewModel != null)
            {
                ConceptualEntityType derivedEntity = viewModel.ModelXRef.GetExisting(Inheritance.TargetEntityType) as ConceptualEntityType;
                Debug.Assert(derivedEntity != null);
                if (derivedEntity != null)
                {
                    viewModel.ModelXRef.Remove(derivedEntity.BaseType, Inheritance);
                    DeleteInheritanceCommand cmd = new DeleteInheritanceCommand(derivedEntity);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 10; }
        }
    }
}
