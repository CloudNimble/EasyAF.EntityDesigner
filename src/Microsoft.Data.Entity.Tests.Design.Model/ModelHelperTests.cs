// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.Model
{
    using System;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Data.Entity.Design.Model.Commands;
    using Microsoft.Data.Entity.Design.Model.Designer;
    using Microsoft.Data.Entity.Design.VersioningFacade;
    using Microsoft.Data.Tools.XmlDesignerBase.Model;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class ModelHelperTests
    {
        [TestMethod]
        public void GetPrimitiveTypeFromString_returns_correct_type_for_valid_EDM_type_names()
        {
            Assert.Equal(PrimitiveTypeKind.Binary, ModelHelper.GetPrimitiveTypeFromString("Binary").PrimitiveTypeKind);
        }

        [TestMethod]
        public void GetPrimitiveTypeFromString_returns_null_for_invalid_EDM_type()
        {
            ModelHelper.GetPrimitiveTypeFromString("Int23".Should().BeNull());
        }

        [TestMethod]
        public void IsValidFacet_returns_true_for_valid_facet()
        {
            ModelHelper.IsValidModelFacet("String", "MaxLength".Should().BeTrue());
        }

        [TestMethod]
        public void IsValidFacet_returns_false_for_invalid_facet()
        {
            ModelHelper.IsValidModelFacet("Int32", "MaxLength".Should().BeFalse());
            ModelHelper.IsValidModelFacet("Int32", "42".Should().BeFalse());
        }

        [TestMethod]
        public void TryGetFacet_returns_true_and_facet_description_for_valid_facet()
        {
            FacetDescription facetDescription;

            (ModelHelper.TryGetFacet(
                    PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), "MaxLength", out facetDescription));
            facetDescription.Should().NotBeNull();
            facetDescription.FacetName.Should().Be("MaxLength");
        }

        [TestMethod]
        public void TryGetFacet_returns_false_and_null_for_invalid_facet()
        {
            FacetDescription facetDescription;

            Assert.False(
                ModelHelper.TryGetFacet(
                    PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), "MaxLength", out facetDescription));
            facetDescription.Should().BeNull();
        }

        [TestMethod]
        public void TryGetFacet_returns_false_and_null_for_null_type()
        {
            FacetDescription facetDescription;

            ModelHelper.TryGetFacet(null, "MaxLength", out facetDescription.Should().BeFalse());
            facetDescription.Should().BeNull();
        }

        [TestMethod, Ignore("Updated binary has updated types")]
        public void GetAllPrimitiveTypes_returns_all_primitive_types_for_version()
        {
            foreach (var schemaVersion in EntityFrameworkVersion.GetAllVersions())
            {
                // remove geo spatial types for schema versions V1 and V2
                var expectedTypes =
                    PrimitiveType
                        .GetEdmPrimitiveTypes()
                        .Where(t => schemaVersion == EntityFrameworkVersion.Version3 || !IsGeoSpatialType(t))
                        .Select(t => t.Name);

                Assert.Equal(expectedTypes, ModelHelper.AllPrimitiveTypes(schemaVersion));
            }
        }

        [TestMethod]
        public void CreateSetDesignerPropertyValueCommandFromArtifact_returns_non_null_for_existing_property_with_non_matching_value()
        {
            // this test just tests that CreateSetDesignerPropertyValueCommandFromArtifact() calls
            // CreateSetDesignerPropertyCommandInsideDesignerInfo(). The tests that the latter works
            // correctly are below.
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (var designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                const string designerPropertyName = "TestPropertyName";
                designerInfoRoot
                    .AddDesignerInfo(
                        "Options",
                        SetupOptionsDesignerInfo(designerPropertyName, "TestValue"));

                entityDesignArtifactMock
                    .Setup(a => a.DesignerInfo)
                    .Returns(designerInfoRoot);

                Assert.NotNull(
                    ModelHelper.CreateSetDesignerPropertyValueCommandFromArtifact(
                        entityDesignArtifactMock.Object, "Options", designerPropertyName, "NewValue"));
            }
        }

        [TestMethod]
        public void CreateSetDesignerPropertyCommandInsideDesignerInfo_returns_null_for_existing_property_with_matching_value()
        {
            const string designerPropertyName = "TestPropertyName";
            const string designerPropertyValue = "TestValue";
            using (var designerInfo = SetupOptionsDesignerInfo(designerPropertyName, designerPropertyValue))
            {
                Assert.Null(
                    ModelHelper.CreateSetDesignerPropertyCommandInsideDesignerInfo(
                        designerInfo,
                        designerPropertyName,
                        designerPropertyValue));
            }
        }

        [TestMethod]
        public void
            CreateSetDesignerPropertyCommandInsideDesignerInfo_returns_UpdateDefaultableValueCommand_for_existing_property_with_non_matching_value
            ()
        {
            const string designerPropertyName = "TestPropertyName";
            using (var designerInfo = SetupOptionsDesignerInfo(designerPropertyName, "TestValue"))
            {
                Assert.IsType<UpdateDefaultableValueCommand<string>>(
                    ModelHelper.CreateSetDesignerPropertyCommandInsideDesignerInfo(
                        designerInfo,
                        designerPropertyName,
                        "DifferentValue"));
            }
        }

        [TestMethod]
        public void CreateSetDesignerPropertyCommandInsideDesignerInfo_passed_null_value_returns_null_for_non_existent_property()
        {
            using (var designerInfo = SetupOptionsDesignerInfo(null, null))
            {
                Assert.Null(
                    ModelHelper.CreateSetDesignerPropertyCommandInsideDesignerInfo(
                        designerInfo,
                        "TestPropertyName",
                        null));
            }
        }

        [TestMethod]
        public void
            CreateSetDesignerPropertyCommandInsideDesignerInfo_passed_non_null_value_returns_ChangeDesignerPropertyCommand_for_non_existent_property
            ()
        {
            using (var designerInfo = SetupOptionsDesignerInfo(null, null))
            {
                Assert.IsType<ChangeDesignerPropertyCommand>(
                    ModelHelper.CreateSetDesignerPropertyCommandInsideDesignerInfo(
                        designerInfo, "TestPropertyName",
                        "NewValue"));
            }
        }

        private DesignerInfo SetupOptionsDesignerInfo(string designerPropertyName, string designerPropertyValue)
        {
            var designerInfo =
                new OptionsDesignerInfo(
                    null,
                    XElement.Parse(
                        "<Options xmlns='http://schemas.microsoft.com/ado/2009/11/edmx' />"));
            var designerInfoPropertySet =
                new DesignerInfoPropertySet(
                    designerInfo,
                    XElement.Parse(
                        "<DesignerInfoPropertySet xmlns='http://schemas.microsoft.com/ado/2009/11/edmx' />"));
            if (designerPropertyName != null)
            {
                var designerProperty =
                    new DesignerProperty(
                        designerInfoPropertySet,
                        XElement.Parse(
                            "<DesignerProperty Name='" + designerPropertyName + "' Value='" +
                            designerPropertyValue +
                            "' xmlns='http://schemas.microsoft.com/ado/2009/11/edmx' />"));
                designerInfoPropertySet.AddDesignerProperty(designerPropertyName, designerProperty);
            }

            designerInfo.PropertySet = designerInfoPropertySet;
            return designerInfo;
        }

        private static bool IsGeoSpatialType(PrimitiveType type)
        {
            return type.PrimitiveTypeKind >= PrimitiveTypeKind.Geometry &&
                   type.PrimitiveTypeKind <= PrimitiveTypeKind.GeographyCollection;
        }
    }
}
