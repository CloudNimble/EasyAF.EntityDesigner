// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal static class FunctionNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            NormalizedName normalizedName = null;

            if (refName == null)
            {
                return null;
            }

            if (parent is ModificationFunction modfunc)
            {
                normalizedName = EFNormalizableItemDefaults.DefaultNameNormalizerForMSL(modfunc, refName);
            }
            else
            {
                var parentItem = parent;
                normalizedName = EFNormalizableItemDefaults.DefaultNameNormalizerForEDM(parentItem, refName);
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
