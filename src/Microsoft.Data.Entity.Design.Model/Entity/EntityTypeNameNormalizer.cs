// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal static class EntityTypeNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            NormalizedName normalizedName = null;

            if (refName == null)
            {
                return null;
            }

            if (parent is EndProperty parentEndProperty)
            {
                normalizedName = EFNormalizableItemDefaults.DefaultNameNormalizerForMSL(parentEndProperty, refName);
            }
            else
            {
                var parentItem = parent;
                normalizedName = EFNormalizableItemDefaults.DefaultNameNormalizerForEDM(parentItem, refName);
            }

            normalizedName ??= new NormalizedName(new Symbol(refName), null, null, refName);

            return normalizedName;
        }
    }
}
