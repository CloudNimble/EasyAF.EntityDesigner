// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    internal class AssociationSetNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            var entityContainerName = string.Empty;

            AssociationSetMapping parentAssociationSetMapping = parent as AssociationSetMapping;
            FunctionAssociationEnd parentFunctionAssociationEnd = parent as FunctionAssociationEnd;

            if (parent is AssociationSet parentAssociationSet)
            {
                // are we trying to normalize the name of actual association set in the EC?
                if (parentAssociationSet.Parent is BaseEntityContainer ec)
                {
                    entityContainerName = ec.LocalName.Value;
                }
            }
            else if (parentAssociationSetMapping != null
                     || parentFunctionAssociationEnd != null)
            {
                // we need to resolve a reference from the MSL to the AssociationSet
                if (parent.GetParentOfType(typeof(EntityContainerMapping)) is EntityContainerMapping ecm)
                {
                    entityContainerName = ecm.CdmEntityContainer.RefName;
                }
            }

            Symbol symbol = null;
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
