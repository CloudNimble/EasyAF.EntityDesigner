// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{

    [RuleOn(typeof(AssociationConnector), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class AssociationConnector_ChangeRule : ChangeRule
    {
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            AssociationConnector associationConnector = e.ModelElement as AssociationConnector;
            Debug.Assert(associationConnector != null, "associationConnector != null");

            if (associationConnector != null)
            {
                // for some reason when deleting connector, DSL invokes ChangeRule, so just return if it's deleted
                if (associationConnector.IsDeleted)
                {
                    return;
                }

                var tx = ModelUtils.GetCurrentTx(e.ModelElement.Store);
                Debug.Assert(tx != null, "tx != null");
                if (tx != null
                    && !tx.IsSerializing)
                {
                    if (e.DomainProperty.Id == LinkShape.EdgePointsDomainPropertyId
                        || e.DomainProperty.Id == LinkShape.ManuallyRoutedDomainPropertyId)
                    {
                        ViewModelChangeContext.GetNewOrExistingContext(tx)
                            .ViewModelChanges.Add(new AssociationConnectorChange(associationConnector, e.DomainProperty.Id));
                    }
                }
            }
        }
    }

}
