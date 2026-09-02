// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    internal static class EntitySetNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            Symbol symbol = null;

            if (refName == null)
            {
                return null;
            }

            var entityContainerName = string.Empty;

            if (parent is EntitySet parentEntitySet)
            {
                // we are trying to normalize the name of actual entity set in the EC
                if (parentEntitySet.Parent is BaseEntityContainer ec)
                {
                    entityContainerName = ec.EntityContainerName;
                }
            }
            else if (parent is EntitySetMapping parentEntitySetMapping)
            {
                // we are trying to normalize the name reference to an EntitySet in the
                // C-space from an EntitySetMapping
                if (parentEntitySetMapping.Parent is EntityContainerMapping ecm)
                {
                    entityContainerName = ecm.CdmEntityContainer.RefName;
                }
            }
            else if (parent is MappingFragment parentMappingFragment)
            {
                // we are trying to normalize the name reference to an EntitySet in the
                // S-space from a MappingFragment
                if (parentMappingFragment.GetParentOfType(typeof(EntityContainerMapping)) is EntityContainerMapping ecm)
                {
                    entityContainerName = ecm.StorageEntityContainer.RefName;
                }
            }
            else if (parent is AssociationSetMapping parentAssociationSetMapping)
            {
                // we are trying to normalize the name reference "TableName" in an
                // AssociationSetMapping back to an EntitySet in S-Space
                if (parentAssociationSetMapping.GetParentOfType(typeof(EntityContainerMapping)) is EntityContainerMapping ecm)
                {
                    entityContainerName = ecm.StorageEntityContainer.RefName;
                }
            }

            if (!string.IsNullOrEmpty(entityContainerName))
            {
                symbol = new Symbol(entityContainerName, refName);
            }

            symbol ??= new Symbol(refName);

            NormalizedName normalizedName = new NormalizedName(symbol, null, null, refName);
            return normalizedName;
        }
    }
}
