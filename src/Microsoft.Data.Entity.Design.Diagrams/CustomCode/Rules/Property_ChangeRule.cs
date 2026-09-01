// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using System;
using System.Diagnostics;
using EDMModelUtils = Microsoft.Data.Entity.Design.Edmx.ModelHelper;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{
    /// <summary>
    ///     Rule fired when a Property changes
    /// </summary>
    [RuleOn(typeof(Property), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class Property_ChangeRule : ChangeRule
    {
        /// <summary>
        ///     Do the following when an Entity changes:
        ///     - Update roles in related Associations
        /// </summary>
        /// <param name="e"></param>
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            base.ElementPropertyChanged(e);

            Property changedProperty = e.ModelElement as Property;

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

                // if EntityType property was changed and EntityDesignerSurface's DisplayNameAndType flag is set to true, we need to refresh the entity shape diagram.
                if (e.DomainProperty.Id == Property.TypeDomainPropertyId
                    && null != diagram
                    && diagram.DisplayNameAndType)
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
                    var viewModel = changedProperty.EntityType.EntityDesignerViewModel;
                    // ensure name is unique and valid if the name has changed
                    if (e.DomainProperty.Id == NameableItem.NameDomainPropertyId)
                    {
                        // if we are creating this, the old name will be empty so there is no 'change' to do
                        if (String.IsNullOrEmpty((string)e.OldValue))
                        {
                            return;
                        }

                        Edmx.Entity.Property modelProperty = viewModel.ModelXRef.GetExisting(changedProperty) as Edmx.Entity.Property;
                        Debug.Assert(modelProperty != null, "modelProperty is null");

                        if (!EDMModelUtils.ValidatePropertyName(modelProperty, changedProperty.Name, true, out string errorMessage))
                        {
                            throw new InvalidOperationException(errorMessage);
                        }

                        ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new PropertyChange(changedProperty));
                    }
                }
            }
        }
    }
}
