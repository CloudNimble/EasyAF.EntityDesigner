// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.VisualStudio.Modeling;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Data.Entity.Design.Diagrams.ViewModel
{
    internal partial class Association : IContainRelatedElementsToEmphasizeWhenSelected
    {
        public NavigationProperty SourceNavigationProperty { get; set; }

        public NavigationProperty TargetNavigationProperty { get; set; }

        /// <summary>
        ///     The property should return the following:
        ///     - Source and Target EntityTypes.
        ///     - Source and Target NavigationProperties.
        ///     - All ForeignKey Properties.
        /// </summary>
        public IEnumerable<ModelElement> RelatedElementsToEmphasizeOnSelected
        {
            get
            {
                // Return SourceEntityType if it is available and not deleted.
                if (SourceEntityType != null
                    && SourceEntityType.IsDeleted == false)
                {
                    yield return SourceEntityType;
                }

                // Return TargeEntityType if it is available and not deleted and not same as SourceEntityType (SelfAssociation scenario).
                if (TargetEntityType != null
                    && TargetEntityType.IsDeleted == false
                    && SourceEntityType != TargetEntityType)
                {
                    yield return TargetEntityType;
                }

                // Return SourceNavigationProperty if it is available and not deleted.
                if (SourceNavigationProperty != null
                    && SourceNavigationProperty.IsDeleted == false)
                {
                    yield return SourceNavigationProperty;
                }

                // Return TargetNavigationProperty if it is available and not deleted.
                if (TargetNavigationProperty != null
                    && TargetNavigationProperty.IsDeleted == false)
                {
                    yield return TargetNavigationProperty;
                }

                Debug.Assert(SourceEntityType != null, "Association's SourceEntityType is null.");
                if (SourceEntityType != null)
                {
                    var viewModel = SourceEntityType.EntityDesignerViewModel;
                    Edmx.Entity.Association modelAssociation = viewModel.ModelXRef.GetExisting(this) as Edmx.Entity.Association;
                    Debug.Assert(modelAssociation != null, "Unable to get model association for DSL association:" + Name);

                    if (modelAssociation != null)
                    {
                        foreach (var prop in modelAssociation.PrincipalRoleProperties.Union(modelAssociation.DependentRoleProperties))
                        {
                            if (viewModel.ModelXRef.GetExisting(prop) is Property viewProperty
                                && viewProperty.IsDeleted == false)
                            {
                                yield return viewProperty;
                            }
                        }
                    }
                }
            }
        }

        protected override void OnDeleting()
        {
            base.OnDeleting();

            if (SourceEntityType != null
                && SourceEntityType.EntityDesignerViewModel != null
                && SourceEntityType.EntityDesignerViewModel.Reloading == false)
            {
                var tx = ModelUtils.GetCurrentTx(Store);
                Debug.Assert(tx != null, "tx != null");
                if (tx != null
                    && !tx.IsSerializing)
                {
                    ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new AssociationDelete(this));
                }
            }
        }
    }
}
