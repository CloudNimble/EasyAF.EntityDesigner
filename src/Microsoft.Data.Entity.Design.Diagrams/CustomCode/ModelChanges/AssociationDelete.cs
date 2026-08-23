// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal class AssociationDelete : ViewModelChange
    {
        private readonly Association _association;

        internal AssociationDelete(Association association)
        {
            _association = association;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = _association.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from association: " + _association.Name);

            if (viewModel != null)
            {
                Edmx.Entity.Association association = viewModel.ModelXRef.GetExisting(_association) as Edmx.Entity.Association;
                Debug.Assert(association != null, "association != null");
                DeleteEFElementCommand.DeleteInTransaction(cpc, association);
                viewModel.ModelXRef.Remove(association, _association);
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 20; }
        }
    }
}
