// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Mapping
{
    internal static class FunctionImportProperyMappingNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            EFElement parentItem = parent.Parent as EFElement;
            Debug.Assert(parentItem != null, "parent.Parent should be an EFElement");

            // some asserts to verify we're using this name normalizer in the correct context

            Debug.Assert(
                parentItem.GetParentOfType(typeof(ComplexProperty)) == null,
                "Use the PropertyMappingNameNormalizer to normalize children of " + parent.GetType().FullName);

            Debug.Assert(
                parentItem.GetParentOfType(typeof(FunctionComplexProperty)) == null,
                "Use the FunctionPropertyMappingNameNormalizer to normalize children of " + parent.GetType().FullName);

            Debug.Assert(
                parentItem.GetParentOfType(typeof(EntityTypeMapping)) == null,
                "Use the PropertyMappingNameNormalizer or FunctionPropertyMappingNameNormalizer to normalize children of "
                + parent.GetType().FullName);

            Debug.Assert(
                parentItem.GetParentOfType(typeof(FunctionAssociationEnd)) == null,
                "Use the FunctionPropertyMappingNameNormalizer to normalize children of " + parent.GetType().FullName);

            Debug.Assert(
                parentItem.GetParentOfType(typeof(AssociationSetMapping)) == null,
                "Use the PropertyNameNormalizer to normalize children of " + parent.GetType().FullName);
            Debug.Assert(
                parentItem.GetParentOfType(typeof(EndProperty)) == null,
                "Use the PropertyNameNormalizer to normalize children of " + parent.GetType().FullName);

            //
            // try to normalize for a FunctionImportTypeMapping
            //
            if (parentItem.GetParentOfType(typeof(FunctionImportTypeMapping)) is FunctionImportTypeMapping fitm)
            {
                if (fitm.TypeName.Status == BindingStatus.Known)
                {
                    Symbol symbol = new Symbol(fitm.TypeName.Target.NormalizedName, refName);
                    return new NormalizedName(symbol, null, null, refName);
                }
            }

            return new NormalizedName(new Symbol(refName), null, null, refName);
        }
    }
}
