// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Mapping
{
    internal static class FunctionPropertyMappingNameNormalizer
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

            // shouldn't use this normalizer for a ComplexProperty
            Debug.Assert(
                parentItem.GetParentOfType(typeof(ComplexProperty)) == null,
                "Use the PropertyNameNormalizer to normalize children of " + parent.GetType().FullName);

            // shouldn't use this normalizer for an AssociationSetMapping
            Debug.Assert(
                parentItem.GetParentOfType(typeof(AssociationSetMapping)) == null,
                "Use the PropertyNameNormalizer to normalize children of " + parent.GetType().FullName);
            Debug.Assert(
                parentItem.GetParentOfType(typeof(EndProperty)) == null,
                "Use the PropertyNameNormalizer to normalize children of " + parent.GetType().FullName);

            // shouldn't use this normalizer for a FunctionImportTypeMapping
            Debug.Assert(
                parentItem.GetParentOfType(typeof(FunctionImportTypeMapping)) == null,
                "Use the FunctionImportPropertyMappingNameNormalizer to normalize children of " + parent.GetType().FullName);

            NormalizedName normalizedName = null;

            //
            // try to normalize for a FunctionComplexProperty
            //
            FunctionComplexProperty fcp = parentItem.GetParentOfType(typeof(FunctionComplexProperty)) as FunctionComplexProperty;
            normalizedName = NormalizeNameRelativeToFunctionComplexProperty(fcp, refName);
            if (normalizedName != null)
            {
                return normalizedName;
            }

            //
            // try to normalize for an EntityTyepMapping with no FunctionAssociationEnd 
            //
            EntityTypeMapping etm = parentItem.GetParentOfType(typeof(EntityTypeMapping)) as EntityTypeMapping;
            if (parentItem.GetParentOfType(typeof(FunctionAssociationEnd)) is not FunctionAssociationEnd fae)
            {
                normalizedName = ProperyMappingNameNormalizer.NormalizePropertyNameRelativeToEntityTypeMapping(etm, parent, refName);
                if (normalizedName != null)
                {
                    return normalizedName;
                }
            }
            else
            {
                //
                // try to normalize for a FunctionAssociationEnd
                //
                normalizedName = NormalizePropertyNameRelativeToFunctionAssociationEnd(fae, parent, refName);
                if (normalizedName != null)
                {
                    return normalizedName;
                }
            }

            //
            //  Default case...
            //
            return new NormalizedName(new Symbol(refName), null, null, refName);
        }

        /// <summary>
        ///     Normalize a refName where the refName is a child of a FunctionComplexProperty
        /// </summary>
        /// <param name="fcp"></param>
        /// <param name="refName"></param>
        /// <returns></returns>
        internal static NormalizedName NormalizeNameRelativeToFunctionComplexProperty(FunctionComplexProperty fcp, string refName)
        {
            NormalizedName nn = null;
            if (fcp != null)
            {
                // for FunctionComplexProperty mapping we create Symbol from corresponding ComplexType
                if (fcp.Name.Status == BindingStatus.Known)
                {
                    var complexType = fcp.Name.Target.ComplexType.Target;
                    if (complexType != null)
                    {
                        Symbol symbol = new Symbol(complexType.NormalizedName, refName);
                        nn = new NormalizedName(symbol, null, null, refName);
                    }
                }
            }
            return nn;
        }

        /// <summary>
        ///     Normalize a refName where the refName is a child of a AssociationEnd
        /// </summary>
        /// <param name="fae"></param>
        /// <param name="parent"></param>
        /// <param name="refName"></param>
        /// <returns></returns>
        private static NormalizedName NormalizePropertyNameRelativeToFunctionAssociationEnd(
            FunctionAssociationEnd fae, EFElement parent, string refName)
        {
            NormalizedName nn = null;
            if (fae != null)
            {
                if (fae.To.Status == BindingStatus.Known)
                {
                    nn = ProperyMappingNameNormalizer.NormalizeNameFromAssociationSetEnd(fae.To.Target, parent, refName);
                }
            }
            return nn;
        }
    }
}
