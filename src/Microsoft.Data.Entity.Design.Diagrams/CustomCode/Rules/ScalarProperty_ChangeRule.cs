// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{
    /// <summary>
    ///     Rule fired when a ScalarProperty changes
    /// </summary>
    [RuleOn(typeof(ScalarProperty), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class ScalarProperty_ChangeRule : ChangeRule
    {
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            base.ElementPropertyChanged(e);

            ScalarProperty changedProperty = e.ModelElement as ScalarProperty;

            Debug.Assert(changedProperty != null, "changedProperty != null");

            // this rule will fire if a PropertyRef gets deleted (this happens if a keyed property that has a sibling keyed property is deleted),
            // in which case we ignore this change.
            if (changedProperty.IsDeleted)
            {
                return;
            }

            Debug.Assert(changedProperty.EntityType != null && changedProperty.EntityType.EntityDesignerViewModel != null, "changedProperty.EntityType != null && changedProperty.EntityType.EntityDesignerViewModel != null");

            if (changedProperty != null
                && changedProperty.EntityType != null
                && changedProperty.EntityType.EntityDesignerViewModel != null)
            {
                var diagram = changedProperty.EntityType.EntityDesignerViewModel.GetDiagram();
                Debug.Assert(diagram != null, "EntityDesignerSurface is null");

                // if EntityKey property changed, we need to invalidate properties compartment for this property to refresh the icon
                if (e.DomainProperty.Id == ScalarProperty.EntityKeyDomainPropertyId)
                {
                    foreach (var pe in PresentationViewsSubject.GetPresentation(changedProperty.EntityType))
                    {
                        EntityTypeShape entityShape = pe as EntityTypeShape;
                        entityShape?.PropertiesCompartment.Invalidate(true);
                    }
                }

                var tx = ModelUtils.GetCurrentTx(e.ModelElement.Store);
                Debug.Assert(tx != null, "tx != null");
                // don't do the auto update stuff if we are in the middle of deserialization
                if (tx != null
                    && !tx.IsSerializing)
                {
                    if (e.DomainProperty.Id == ScalarProperty.EntityKeyDomainPropertyId)
                    {
                        ViewModelChangeContext.GetNewOrExistingContext(tx)
                            .ViewModelChanges.Add(new ScalarPropertyKeyChange(changedProperty));
                    }
                }
            }
        }
    }
}
