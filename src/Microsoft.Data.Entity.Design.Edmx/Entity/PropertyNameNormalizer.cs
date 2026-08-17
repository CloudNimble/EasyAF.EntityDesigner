// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    internal static class PropertyNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }
            Property property = parent as Property;
            NavigationProperty navigationProperty = parent as NavigationProperty;

            Symbol symbol = null;

            if (parent is EntityType entityType)
            {
                if (entityType.Parent is BaseEntityModel em)
                {
                    symbol = new Symbol(em.NamespaceValue, entityType.LocalName.Value, refName);
                }
            }
            else if (parent is ComplexType complexType)
            {
                if (complexType.Parent is BaseEntityModel em)
                {
                    symbol = new Symbol(em.NamespaceValue, complexType.LocalName.Value, refName);
                }
            }
            else if (property != null
                     || navigationProperty != null)
            {
                if (parent.Parent is EntityType et)
                {
                    if (et.Parent is BaseEntityModel em)
                    {
                        symbol = new Symbol(em.NamespaceValue, et.LocalName.Value, refName);
                    }
                }
                else
                {
                    if (parent.Parent is ComplexType ct)
                    {
                        if (ct.Parent is BaseEntityModel em)
                        {
                            symbol = new Symbol(em.NamespaceValue, ct.LocalName.Value, refName);
                        }
                    }
                }
            }

            symbol ??= new Symbol(refName);

            NormalizedName normalizedName = new NormalizedName(symbol, null, null, refName);
            return normalizedName;
        }
    }
}
