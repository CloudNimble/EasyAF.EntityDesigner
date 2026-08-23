// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Modeling;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{

    [RuleOn(typeof(InheritanceConnector), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class InheritanceConnector_AddRule : AddRule
    {
        public override void ElementAdded(ElementAddedEventArgs e)
        {
            InheritanceConnector inheritanceConnector = e.ModelElement as InheritanceConnector;
            Debug.Assert(inheritanceConnector != null, "inheritanceConnector != null");

            var tx = ModelUtils.GetCurrentTx(inheritanceConnector.Store);
            Debug.Assert(tx != null, "tx != null");
            if (tx != null
                && !tx.IsSerializing)
            {
                ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new InheritanceConnectorAdd(inheritanceConnector));
            }
        }
    }

}
