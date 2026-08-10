// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.View;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model.Commands;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class AssociationConnectorDelete : AssociationConnectorModelChange
    {
        internal AssociationConnectorDelete(AssociationConnector associationConnector)
            : base(associationConnector)
        {
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = AssociationConnector.GetRootViewModel();
            Debug.Assert(
                viewModel != null, "Unable to find root view model from association connector: " + AssociationConnector.AccessibleName);

            if (viewModel != null)
            {
                if (viewModel.ModelXRef.GetExisting(AssociationConnector) is Model.Designer.AssociationConnector modelAssociationConnector)
                {
                    DeleteEFElementCommand.DeleteInTransaction(cpc, modelAssociationConnector);
                    viewModel.ModelXRef.Remove(modelAssociationConnector, AssociationConnector);
                }
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 0; }
        }
    }
}
