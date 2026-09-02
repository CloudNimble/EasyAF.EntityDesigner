// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    /// <summary>
    ///     This normalizer is different from most in that there isn't a "Name" property
    ///     on an End element in an AssociationSet.  This method takes the refName sent and
    ///     uses it to come up with a unique symbol that can be used to identify the End.
    /// </summary>
    internal static class AssociationSetEndNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            Symbol symbol = new Symbol();

            if (parent is AssociationSetEnd parentAssociationSetEnd)
            {
                // we are coming up with the object's name for the first time
                if (parentAssociationSetEnd.Parent is AssociationSet assocSet)
                {
                    if (assocSet.Parent is BaseEntityContainer ec)
                    {
                        symbol = new Symbol(ec.EntityContainerName, assocSet.LocalName.Value, refName);
                    }
                }
            }
            else if (parent is EndProperty parentEndProperty)
            {
                // this end is inside an AssociationSetMapping, so we derive the end's name based on the set's name
                AssociationSetMapping asm = parentEndProperty.Parent as AssociationSetMapping;
                if (asm.Name.Status == BindingStatus.Known)
                {
                    symbol = new Symbol(asm.Name.Target.NormalizedName, refName);
                }
            }
            else if (parent is FunctionAssociationEnd parentFunctionAssociationEnd)
            {
                // this end is inside of a function mapping
                if (parentFunctionAssociationEnd.AssociationSet.Status == BindingStatus.Known)
                {
                    symbol = new Symbol(parentFunctionAssociationEnd.AssociationSet.Target.NormalizedName, refName);
                }
            }

            NormalizedName normalizedName = new NormalizedName(symbol, null, null, refName);
            return normalizedName;
        }
    }
}
