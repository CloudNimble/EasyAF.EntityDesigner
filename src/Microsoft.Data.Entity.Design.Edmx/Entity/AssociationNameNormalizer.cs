// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    internal static class AssociationNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            NormalizedName normalizedName = null;

            if (parent is Association parentAssociation)
            {
                if (parentAssociation.Parent is BaseEntityModel model)
                {
                    // we are coming up with the object's name for the first time
                    Symbol symbol = new Symbol(model.NamespaceValue, parentAssociation.LocalName.Value);
                    normalizedName = new NormalizedName(symbol, null, null, parentAssociation.LocalName.Value);
                }
            }
            else if (parent is AssociationSet parentAssociationSet)
            {
                // we are wanting to resolve a reference from an Association Set that may or may not
                // use the alias defined in the EntityModel
                normalizedName = EFNormalizableItemDefaults.DefaultNameNormalizerForEDM(parentAssociationSet, refName);
            }
            else if (parent is AssociationSetMapping parentAssociationSetMapping)
            {
                normalizedName = EFNormalizableItemDefaults.DefaultNameNormalizerForMSL(parentAssociationSetMapping, refName);
            }
            else if (parent is NavigationProperty parentNavigationProperty)
            {
                normalizedName = EFNormalizableItemDefaults.DefaultNameNormalizerForEDM(parentNavigationProperty, refName);
            }

            if (normalizedName == null)
            {
                Symbol symbol = new Symbol(refName);
                normalizedName = new NormalizedName(symbol, null, null, refName);
            }

            return normalizedName;
        }
    }
}
