// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Mapping
{
    internal static class PropertyMappingColumnNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            var parentItem = parent;
            Debug.Assert(parentItem != null, "parent.Parent should be an EFElement");

            Symbol symbol = null;

            if (parentItem.GetParentOfType(typeof(MappingFragment)) is MappingFragment frag
                && frag.StoreEntitySet.Status == BindingStatus.Known)
            {
                // walk up until we find our MappingFragment, then we can walk to the EntitySet in the S-Space
                // that has the EntityType that contains the column
                var storageSpaceEntitySet = frag.StoreEntitySet.Target;
                if (storageSpaceEntitySet != null)
                {
                    if (storageSpaceEntitySet.EntityType.Target != null)
                    {
                        var entityTypeSymbol = storageSpaceEntitySet.EntityType.Target.NormalizedName;
                        Symbol propertySymbol = new Symbol(entityTypeSymbol, refName);

                        var artifactSet = parent.Artifact.ModelManager.GetArtifactSet(parent.Artifact.Uri);
                        var item = artifactSet.LookupSymbol(propertySymbol);
                        if (item != null)
                        {
                            return new NormalizedName(propertySymbol, null, null, refName);
                        }
                    }
                }
            }
            else if (parentItem.GetParentOfType(typeof(AssociationSetMapping)) is AssociationSetMapping asm)
            {
                // this is a condition under an AssocationSetMapping or a ScalarProperty under an EndProperty
                // regardless, use the reference to our S-Space from the asm
                if (asm.StoreEntitySet.Status == BindingStatus.Known)
                {
                    var set = asm.StoreEntitySet.Target;

                    // Note: set.EntityType.Target can be null for e.g. an asm with a StoreEntitySet attribute
                    // which points to a non-existent EntitySet
                    if (null != set
                        && null != set.EntityType
                        && null != set.EntityType.Target
                        && set.EntityType.Status == BindingStatus.Known)
                    {
                        var type = set.EntityType.Target;
                        symbol = new Symbol(type.NormalizedName, refName);
                    }
                }
            }

            symbol ??= new Symbol(refName);

            NormalizedName normalizedName = new NormalizedName(symbol, null, null, refName);
            return normalizedName;
        }
    }
}
