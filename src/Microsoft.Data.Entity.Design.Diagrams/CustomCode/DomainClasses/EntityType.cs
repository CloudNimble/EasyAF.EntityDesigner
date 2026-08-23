// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.VisualStudio.Modeling;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.ViewModel
{
    internal partial class EntityType : IContainRelatedElementsToEmphasizeWhenSelected
    {
        /// <summary>
        ///     Returns all the Association related to the entity-type.
        /// </summary>
        public IEnumerable<ModelElement> RelatedElementsToEmphasizeOnSelected
        {
            get
            {
                ConceptualEntityType entityType = EntityDesignerViewModel.ModelXRef.GetExisting(this) as ConceptualEntityType;
                Debug.Assert(entityType != null, "Unable to find model EntityType for DSL EntityType:" + Name);
                if (entityType != null)
                {
                    foreach (var modelAssociation in Edmx.Entity.Association.GetAssociationsForEntityType(entityType))
                    {
                        if (EntityDesignerViewModel.ModelXRef.GetExisting(modelAssociation) is Association viewAssociation
                            && viewAssociation.IsDeleted == false)
                        {
                            yield return viewAssociation;
                        }
                    }
                }
            }
        }

        protected override bool CanMerge(ProtoElementBase rootElement, ElementGroupPrototype elementGroupPrototype)
        {
            if (rootElement != null
                && rootElement.ElementId == Guid.Empty)
            {
                var rootElementDomainInfo = Partition.DomainDataDirectory.GetDomainClass(rootElement.DomainClassId);

                if (rootElementDomainInfo.IsDerivedFrom(NavigationProperty.DomainClassId))
                {
                    return false;
                }
            }
            return base.CanMerge(rootElement, elementGroupPrototype);
        }

        protected override void OnDeleting()
        {
            base.OnDeleting();

            if (EntityDesignerViewModel != null
                && EntityDesignerViewModel.Reloading == false)
            {
                var tx = ModelUtils.GetCurrentTx(Store);
                Debug.Assert(tx != null, "tx != null");
                if (tx != null
                    && !tx.IsSerializing)
                {
                    ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new EntityTypeDelete(this));
                }
            }
        }
    }
}
