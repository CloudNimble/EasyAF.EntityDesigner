// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Modeling;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{

    [RuleOn(typeof(AssociationConnector), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class AssociationConnector_DeleteRule : DeleteRule
    {
        public override void ElementDeleted(ElementDeletedEventArgs e)
        {
            base.ElementDeleted(e);

            if (e.ModelElement is AssociationConnector associationConnector)
            {
                var tx = ModelUtils.GetCurrentTx(associationConnector.Store);
                Debug.Assert(tx != null, "tx != null");
                if (tx != null
                    && !tx.IsSerializing)
                {
                    ViewModelChangeContext.GetNewOrExistingContext(tx)
                        .ViewModelChanges.Add(new AssociationConnectorDelete(associationConnector));
                }
            }
        }
    }

}
