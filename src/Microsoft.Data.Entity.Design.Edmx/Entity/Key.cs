// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal sealed class Key : PropertyRefContainer
    {
        internal static readonly string ElementName = "Key";

        internal Key(EFContainer parent, XElement element)
            : base(parent, element)
        {
        }

        internal override SingleItemBinding<Property>.NameNormalizer GetNameNormalizerForPropertyRef()
        {
            return NameNormalizer;
        }

        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            PropertyRef pr = parent as PropertyRef;
            Key key = pr.Parent as Key;
            EntityType entityType = key.Parent as EntityType;

            Symbol symbol = null;
            if (entityType.Parent is BaseEntityModel em)
            {
                symbol = new Symbol(em.NamespaceValue, entityType.LocalName.Value, refName);
            }

            symbol ??= new Symbol(refName);

            NormalizedName normalizedName = new NormalizedName(symbol, null, null, refName);

            return normalizedName;
        }
    }
}
