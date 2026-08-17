// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.Edmx.Validation;
using Microsoft.VisualStudio.Modeling;
using System;
using System.Diagnostics;
using System.Globalization;
using EDMModelUtils = Microsoft.Data.Entity.Design.Edmx.ModelHelper;
using EntityDesignerRes = Microsoft.Data.Entity.Design.Diagrams.Properties.DiagramsResources;
using ModelRes = Microsoft.Data.Entity.Design.Edmx.EdmxResources;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{
    /// <summary>
    ///     Rule fired when an NavigationProperty changes
    /// </summary>
    [RuleOn(typeof(NavigationProperty), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class NavigationProperty_ChangeRule : ChangeRule
    {
        /// <summary>
        ///     Do the following when an Entity changes:
        ///     - Update roles in related Associations
        /// </summary>
        /// <param name="e"></param>
        public override void ElementPropertyChanged(ElementPropertyChangedEventArgs e)
        {
            base.ElementPropertyChanged(e);

            NavigationProperty changedNavigationProperty = e.ModelElement as NavigationProperty;
            Debug.Assert(changedNavigationProperty != null);
            Debug.Assert(
                changedNavigationProperty.EntityType != null && changedNavigationProperty.EntityType.EntityDesignerViewModel != null);

            if ((changedNavigationProperty != null)
                && (changedNavigationProperty.EntityType != null)
                && (changedNavigationProperty.EntityType.EntityDesignerViewModel != null))
            {
                var tx = ModelUtils.GetCurrentTx(e.ModelElement.Store);
                Debug.Assert(tx != null);
                // don't do the auto update stuff if we are in the middle of deserialization
                if (tx != null
                    && !tx.IsSerializing)
                {
                    var viewModel = changedNavigationProperty.EntityType.EntityDesignerViewModel;

                    if (e.DomainProperty.Id == NameableItem.NameDomainPropertyId)
                    {
                        // if we are creating this, the old name will be empty so there is no 'change' to do
                        if (String.IsNullOrEmpty((string)e.OldValue))
                        {
                            return;
                        }

                        if (!EdmxAttributeContentValidator.IsValidCsdlNavigationPropertyName(changedNavigationProperty.Name))
                        {
                            throw new InvalidOperationException(
                                String.Format(
                                    CultureInfo.CurrentCulture, EntityDesignerRes.Error_NavigationPropertyNameInvalid,
                                    changedNavigationProperty.Name));
                        }

                        Edmx.Entity.EntityType modelEntityType =
                            viewModel.ModelXRef.GetExisting(changedNavigationProperty.EntityType) as Edmx.Entity.EntityType;
                        Debug.Assert(modelEntityType != null, "modelEntityType is null");

                        // ensure name is unique
                        if (modelEntityType.LocalName.Value.Equals(changedNavigationProperty.Name, StringComparison.Ordinal))
                        {
                            var msg = string.Format(
                                CultureInfo.CurrentCulture, ModelRes.Error_MemberNameSameAsParent, changedNavigationProperty.Name,
                                modelEntityType.LocalName.Value);
                            throw new InvalidOperationException(msg);
                        }
                        else if (!EDMModelUtils.IsUniquePropertyName(modelEntityType, changedNavigationProperty.Name, true))
                        {
                            var msg = string.Format(
                                CultureInfo.CurrentCulture, ModelRes.Error_MemberNameNotUnique, changedNavigationProperty.Name,
                                modelEntityType.LocalName.Value);
                            throw new InvalidOperationException(msg);
                        }

                        ViewModelChangeContext.GetNewOrExistingContext(tx)
                            .ViewModelChanges.Add(new NavigationPropertyChange(changedNavigationProperty));
                    }
                }
            }
        }
    }
}
