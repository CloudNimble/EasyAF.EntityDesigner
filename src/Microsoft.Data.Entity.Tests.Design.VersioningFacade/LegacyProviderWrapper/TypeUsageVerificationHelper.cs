// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using LegacyMetadata = System.Data.Metadata.Edm;
using LegacySpatial = System.Data.Spatial;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.LegacyProviderWrapper
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Spatial;
    using System.Linq;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    internal class TypeUsageVerificationHelper
    {
        public static void VerifyEdmTypesEquivalent(LegacyMetadata.EdmType legacyEdmType, EdmType edmType)
        {
            edmType.FullName.Should().Be(legacyEdmType.FullName);

            ((legacyEdmType.BaseType == null && edmType.BaseType == null) ||
                legacyEdmType.BaseType.FullName == edmType.BaseType.FullName);
            Assert.Equal(legacyEdmType.BuiltInTypeKind.ToString(), edmType.BuiltInTypeKind.ToString());
            Assert.Equal(
                ((LegacyMetadata.DataSpace)typeof(LegacyMetadata.EdmType)
                                               .GetProperty("DataSpace", BindingFlags.Instance | BindingFlags.NonPublic)
                                               .GetValue(legacyEdmType)).ToString(),
                ((DataSpace)typeof(EdmType)
                                .GetProperty("DataSpace", BindingFlags.Instance | BindingFlags.NonPublic)
                                .GetValue(edmType)).ToString());

            if (edmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveTypeKind)
            {
                var prmitiveEdmType = (PrimitiveType)edmType;
                var legacyPrmitiveEdmType = (LegacyMetadata.PrimitiveType)legacyEdmType;

                // EF5 geospatial types should be converted to EF6 spatial types
                var expectedClrEquivalentType =
                    legacyPrmitiveEdmType.ClrEquivalentType == typeof(LegacySpatial.DbGeography)
                        ? typeof(DbGeography)
                        : legacyPrmitiveEdmType.ClrEquivalentType == typeof(LegacySpatial.DbGeometry)
                              ? typeof(DbGeometry)
                              : legacyPrmitiveEdmType.ClrEquivalentType;

                prmitiveEdmType.ClrEquivalentType.Should().Be(expectedClrEquivalentType);
                Assert.Equal(legacyPrmitiveEdmType.GetEdmPrimitiveType().FullName, prmitiveEdmType.GetEdmPrimitiveType().FullName);
            }
        }

        public static void VerifyTypeUsagesEquivalent(LegacyMetadata.TypeUsage legacyTypeUsage, TypeUsage typeUsage)
        {
            if (typeUsage.EdmType.BuiltInTypeKind == BuiltInTypeKind.CollectionType)
            {
                VerifyTypeUsagesEquivalent(
                    ((LegacyMetadata.CollectionType)legacyTypeUsage.EdmType).TypeUsage,
                    ((CollectionType)typeUsage.EdmType).TypeUsage);
            }
            else
            {
                VerifyEdmTypesEquivalent(legacyTypeUsage.EdmType, typeUsage.EdmType);
            }

            var legacyTypeFacets = legacyTypeUsage.Facets.OrderBy(f => f.Name).ToArray();
            var typeFacets = typeUsage.Facets.OrderBy(f => f.Name).ToArray();

            typeFacets.Length.Should().Be(legacyTypeFacets.Length);
            for (var i = 0; i < legacyTypeFacets.Length; i++)
            {
                VerifyFacetsEquivalent(legacyTypeFacets[i], typeFacets[i]);
            }
        }

        public static void VerifyFacetsEquivalent(LegacyMetadata.Facet legacyFacet, Facet facet)
        {
            facet.Name.Should().Be(legacyFacet.Name);
            facet.FacetType.FullName.Should().Be(legacyFacet.FacetType.FullName);

            // Specialcase Variable, Max and Identity facet values - they are internal singleton objects.
            if (legacyFacet.Value != null
                && (new[] { "Max", "Variable", "Identity" }.Contains(legacyFacet.Value.ToString())
                    || facet.Name == "ConcurrencyMode"))
            {
                // this is to make sure we did not stick EF6 Max/Variable/Identity on legacy facet as the value
                Assert.Equal(typeof(LegacyMetadata.EdmType).Assembly, legacyFacet.Value.GetType().Assembly);

                facet.Value.Should().NotBeNull();
                Assert.Equal(legacyFacet.Value.ToString(), facet.Value.ToString());
            }
            else
            {
                facet.Value.Should().Be(legacyFacet.Value);
            }

            facet.IsUnbounded.Should().Be(legacyFacet.IsUnbounded);
            legacyFacet.BuiltInTypeKind.Should().Be(LegacyMetadata.BuiltInTypeKind.Facet);
            facet.BuiltInTypeKind.Should().Be(BuiltInTypeKind.Facet);
        }

        public static void VerifyFacetDescriptionsEquivalent(
            FacetDescription facetDescription, LegacyMetadata.FacetDescription legacyFacetDescription)
        {
            legacyFacetDescription.FacetName.Should().Be(facetDescription.FacetName);
            VerifyEdmTypesEquivalent(legacyFacetDescription.FacetType, facetDescription.FacetType);
            (// .ToString makes it easier to compare default values like "Variable"
                facetDescription.DefaultValue.ToString() == legacyFacetDescription.DefaultValue.ToString() ||
                facetDescription.DefaultValue == legacyFacetDescription.DefaultValue);
            legacyFacetDescription.IsConstant.Should().Be(facetDescription.IsConstant);
            legacyFacetDescription.IsRequired.Should().Be(facetDescription.IsRequired);
            legacyFacetDescription.MaxValue.Should().Be(facetDescription.MaxValue);
            legacyFacetDescription.MinValue.Should().Be(facetDescription.MinValue);
        }
    }
}
