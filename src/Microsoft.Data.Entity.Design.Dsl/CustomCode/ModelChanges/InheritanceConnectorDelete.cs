// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.View;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model.Commands;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class InheritanceConnectorDelete : InheritanceConnectorModelChange
    {
        internal InheritanceConnectorDelete(InheritanceConnector inheritanceConnector)
            : base(inheritanceConnector)
        {
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = InheritanceConnector.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from inheritance connector: " + InheritanceConnector);

            if (viewModel != null)
            {
                if (viewModel.ModelXRef.GetExisting(InheritanceConnector) is Model.Designer.InheritanceConnector modelInheritanceConnector)
                {
                    viewModel.ModelXRef.Remove(modelInheritanceConnector, InheritanceConnector);
                    DeleteEFElementCommand.DeleteInTransaction(cpc, modelInheritanceConnector);
                }
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 0; }
        }
    }
}
