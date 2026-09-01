// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators;
using Microsoft.Data.Entity.Design.DatabaseGeneration.Properties;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Tests.Design.DatabaseGeneration
{
    [TestClass]
    public class EdmExtensionTests
    {
        private const string Ssdl =
            "<Schema Namespace='AdventureWorksModel.Store' Provider='System.Data.SqlClient' ProviderManifestToken='2008' xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'>"
            +
            "  <EntityContainer Name='AdventureWorksModelStoreContainer'>" +
            "    <EntitySet Name='Entities' EntityType='AdventureWorksModel.Store.Entities' Schema='dbo' />" +
            "  </EntityContainer>" +
            "  <EntityType Name='Entities'>" +
            "    <Key>" +
            "      <PropertyRef Name='Id' />" +
            "    </Key>" +
            "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />" +
            "    <Property Name='Name' Type='nvarchar(max)' Nullable='false' />" +
            "  </EntityType>" +
            "</Schema>";

        private const string Csdl =
            "<Schema Namespace='AdventureWorksModel' Alias='Self' p1:UseStrongSpatialTypes='false' xmlns:annotation='http://schemas.microsoft.com/ado/2009/02/edm/annotation' xmlns:p1='http://schemas.microsoft.com/ado/2009/02/edm/annotation' xmlns='http://schemas.microsoft.com/ado/2009/11/edm'>"
            +
            "   <EntityContainer Name='AdventureWorksEntities3' p1:LazyLoadingEnabled='true' >" +
            "       <EntitySet Name='Entities' EntityType='AdventureWorksModel.Entity' />" +
            "   </EntityContainer>" +
            "   <EntityType Name='Entity'>" +
            "       <Key>" +
            "           <PropertyRef Name='Id' />" +
            "       </Key>" +
            "       <Property Type='Int32' Name='Id' Nullable='false' annotation:StoreGeneratedPattern='Identity' />" +
            "       <Property Type='String' Name='Name' Nullable='false' />" +
            "   </EntityType>" +
            "</Schema>";

        private const string Msl =
            "<Mapping Space='C-S' xmlns='http://schemas.microsoft.com/ado/2009/11/mapping/cs'>" +
            "  <EntityContainerMapping StorageEntityContainer='AdventureWorksModelStoreContainer' CdmEntityContainer='AdventureWorksEntities3'>"
            +
            "    <EntitySetMapping Name='Entities'>" +
            "      <EntityTypeMapping TypeName='IsTypeOf(AdventureWorksModel.Entity)'>" +
            "        <MappingFragment StoreEntitySet='Entities'>" +
            "          <ScalarProperty Name='Id' ColumnName='Id' />" +
            "          <ScalarProperty Name='Name' ColumnName='Name' />" +
            "        </MappingFragment>" +
            "      </EntityTypeMapping>" +
            "    </EntitySetMapping>" +
            "  </EntityContainerMapping>" +
            "</Mapping>";

        private readonly IDbDependencyResolver resolver;

        public EdmExtensionTests()
        {
            // Get SqlProviderServices.Instance via reflection to avoid compile-time dependency
            Type sqlProviderServicesType = Type.GetType(
                "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer",
                throwOnError: true);
            var instanceProperty = sqlProviderServicesType.GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            DbProviderServices providerServices = (DbProviderServices)instanceProperty.GetValue(null);

            Mock<IDbDependencyResolver> mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver.Setup(
                r => r.GetService(
                    It.Is<Type>(t => t == typeof(DbProviderServices)),
                    It.IsAny<string>())).Returns(providerServices);

            resolver = mockResolver.Object;
        }

        [TestMethod]
        public void CreateAndValidateEdmItemCollection_throws_ArgumentNullException_for_null_csdl()
        {
            Action act = () => EdmExtension.CreateAndValidateEdmItemCollection(null, new Version(1, 0, 0, 0));
            var exception = act.Should().Throw<ArgumentNullException>().Which;
            exception.ParamName.Should().Be("csdl");
        }

        [TestMethod]
        public void CreateAndValidateEdmItemCollection_throws_ArgumentNullException_for_null_targetFrameworkVersion()
        {
            Action act = () => EdmExtension.CreateAndValidateEdmItemCollection(string.Empty, null);
            var exception = act.Should().Throw<ArgumentNullException>().Which;
            exception.ParamName.Should().Be("targetFrameworkVersion");
        }

        [TestMethod]
        public void CreateAndValidateEdmItemCollection_throws_ArgumentException_for_incorrect_targetFrameworkVersion()
        {
            Action act = () => EdmExtension.CreateAndValidateEdmItemCollection(string.Empty, new Version(0, 0));
            var exception = act.Should().Throw<ArgumentException>().Which;
            exception.ParamName.Should().Be("targetFrameworkVersion");
            exception.Message.Should().StartWith(
                string.Format(CultureInfo.CurrentCulture, DatabaseGenerationResources.ErrorNonValidTargetVersion, "0.0"));
        }

        [TestMethod]
        public void CreateAndValidateEdmItemCollection_throws_for_invalid_csdl()
        {
            XDocument invalidCsdl = XDocument.Parse(Csdl);
            invalidCsdl.Descendants("{http://schemas.microsoft.com/ado/2009/11/edm}PropertyRef").Remove();
            invalidCsdl
                .Descendants("{http://schemas.microsoft.com/ado/2009/11/edm}EntityType")
                .Single()
                .Add(new XElement("{http://schemas.microsoft.com/ado/2009/11/edm}InvalidElement"));

            Action act = () => EdmExtension.CreateAndValidateEdmItemCollection(invalidCsdl.ToString(), new Version(3, 0, 0, 0));
            var exception = act.Should().Throw<InvalidOperationException>().Which;

            exception.Message.Should().StartWith(DatabaseGenerationResources.ErrorCsdlNotValid.Replace("{0}", string.Empty));
            var errorMessages = exception.Message.Split('\n');
            errorMessages.Length.Should().Be(3);
            errorMessages[0].Should().Contain("PropertyRef");
            errorMessages[1].Should().Contain("InvalidElement");
            errorMessages[2].Should().Contain("InvalidElement");
        }

        [TestMethod]
        public void CreateAndValidateEdmItemCollection_throws_for_unsupported_version()
        {
            // Version2 is no longer supported - only Version3 is valid
            Action act = () => EdmExtension.CreateAndValidateEdmItemCollection(Csdl, new Version(2, 0, 0, 0));
            var exception = act.Should().Throw<ArgumentException>().Which;

            exception.Message.Should().Contain("2.0.0.0");
            exception.ParamName.Should().Be("targetFrameworkVersion");
        }

        [TestMethod]
        public void CreateAndValidateEdmItemCollection_throws_for_invalid_csdl_with_Version3()
        {
            // Test that invalid CSDL with valid Version3 throws appropriate errors
            XDocument invalidCsdl = XDocument.Parse(Csdl);
            invalidCsdl.Descendants("{http://schemas.microsoft.com/ado/2009/11/edm}PropertyRef").Remove();
            invalidCsdl
                .Descendants("{http://schemas.microsoft.com/ado/2009/11/edm}EntityType")
                .Single()
                .Add(new XElement("{http://schemas.microsoft.com/ado/2009/11/edm}InvalidElement"));

            Action act = () => EdmExtension.CreateAndValidateEdmItemCollection(invalidCsdl.ToString(), new Version(3, 0, 0, 0));
            var exception = act.Should().Throw<InvalidOperationException>().Which;

            exception.Message.Should().StartWith(DatabaseGenerationResources.ErrorCsdlNotValid.Replace("{0}", string.Empty));
            var errorMessages = exception.Message.Split('\n');
            errorMessages.Length.Should().Be(3);
            errorMessages[0].Should().Contain("PropertyRef");
            errorMessages[1].Should().Contain("InvalidElement");
            errorMessages[2].Should().Contain("InvalidElement");
        }

        [TestMethod]
        public void CreateAndValidateEdmItemCollection_creates_EdmItemCollection_for_valid_csdl_and_targetFrameworkVersion()
        {
            var edmItemCollection = EdmExtension.CreateAndValidateEdmItemCollection(Csdl, new Version(3, 0, 0, 0));

            edmItemCollection.Should().NotBeNull();
            edmItemCollection.GetItem<EntityType>("AdventureWorksModel.Entity").Should().NotBeNull();
        }

        [TestMethod]
        public void CreateStoreItemCollection_throws_ArgumentNullException_for_null_ssdl()
        {
            Action act = () => EdmExtension.CreateStoreItemCollection(
                null,
                new Version(1, 0, 0, 0),
                null,
                out IList<EdmSchemaError> schemaErrors);
            var exception = act.Should().Throw<ArgumentNullException>().Which;
            exception.ParamName.Should().Be("ssdl");
        }

        [TestMethod]
        public void CreateStoreItemCollection_throws_ArgumentNullException_for_null_targetFrameworkVersion()
        {

            Action act = () => EdmExtension.CreateStoreItemCollection(
                string.Empty,
                null,
                null,
                out IList<EdmSchemaError> schemaErrors);
            var exception = act.Should().Throw<ArgumentNullException>().Which;
            exception.ParamName.Should().Be("targetFrameworkVersion");
        }

        [TestMethod]
        public void CreateStoreItemCollection_throws_ArgumentException_for_incorrect_targetFrameworkVersion()
        {

            Action act = () => EdmExtension.CreateStoreItemCollection(
                string.Empty,
                new Version(0, 0),
                null,
                out IList<EdmSchemaError> schemaErrors);
            var exception = act.Should().Throw<ArgumentException>().Which;
            exception.ParamName.Should().Be("targetFrameworkVersion");
            exception.Message.Should().StartWith(
                string.Format(CultureInfo.CurrentCulture, DatabaseGenerationResources.ErrorNonValidTargetVersion, "0.0"));
        }

        [TestMethod]
        public void CreateStoreItemCollection_returns_errors_StoreItemCollections_for_invalid_ssdl()
        {
            XDocument invalidSsdl = XDocument.Parse(Ssdl);
            invalidSsdl.Descendants("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}" + "PropertyRef").Remove();

            var storeItemCollection = EdmExtension.CreateStoreItemCollection(
                invalidSsdl.ToString(),
                new Version(3, 0, 0, 0),
                resolver,
                out IList<EdmSchemaError> schemaErrors);

            storeItemCollection.Should().BeNull();
            schemaErrors.Count.Should().Be(1);
            schemaErrors[0].Message.Should().Contain("PropertyRef");
        }

        [TestMethod]
        public void CreateStoreItemCollection_creates_StoreItemCollection_for_valid_ssdl_and_targetFrameworkVersion()
        {
            var storeItemCollection =
                EdmExtension.CreateStoreItemCollection(
                    Ssdl,
                    new Version(3, 0, 0, 0),
                    resolver,
                    out IList<EdmSchemaError> schemaErrors);

            storeItemCollection.Should().NotBeNull();
            schemaErrors.Count.Should().Be(0);
            storeItemCollection.GetItem<EntityType>("AdventureWorksModel.Store.Entities").Should().NotBeNull();
        }

        [TestMethod]
        public void CreateAndValidateStoreItemCollection_throws_ArgumentNullException_for_null_ssdl()
        {
            Action act = () => EdmExtension.CreateAndValidateStoreItemCollection(null, new Version(1, 0, 0, 0), null, true);
            var exception = act.Should().Throw<ArgumentNullException>().Which;
            exception.ParamName.Should().Be("ssdl");
        }

        [TestMethod]
        public void CreateAndValidateStoreItemCollection_throws_ArgumentNullException_for_null_targetFrameworkVersion()
        {
            Action act = () => EdmExtension.CreateAndValidateStoreItemCollection(string.Empty, null, null, true);
            var exception = act.Should().Throw<ArgumentNullException>().Which;
            exception.ParamName.Should().Be("targetFrameworkVersion");
        }

        [TestMethod]
        public void CreateAndValidateStoreItemCollection_throws_ArgumentException_for_incorrect_targetFrameworkVersion()
        {
            Action act = () => EdmExtension.CreateAndValidateStoreItemCollection(string.Empty, new Version(0, 0), null, true);
            var exception = act.Should().Throw<ArgumentException>().Which;
            exception.ParamName.Should().Be("targetFrameworkVersion");
            exception.Message.Should().StartWith(
                string.Format(CultureInfo.CurrentCulture, DatabaseGenerationResources.ErrorNonValidTargetVersion, "0.0"));
        }

        [TestMethod]
        public void CreateAndValidateStoreItemCollection_throws_for_invalid_ssdl_catchThrowNamingConflicts_false()
        {
            XDocument invalidSsdl = XDocument.Parse(Ssdl);
            var entityTypeElement =
                invalidSsdl.Descendants("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}EntityType").Single();
            entityTypeElement.AddAfterSelf(new XElement("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}InvalidElement"));
            entityTypeElement.AddAfterSelf(entityTypeElement);

            Action act = () => EdmExtension.CreateAndValidateStoreItemCollection(
                invalidSsdl.ToString(),
                new Version(3, 0, 0, 0),
                resolver,
                catchThrowNamingConflicts: false);
            var exception = act.Should().Throw<InvalidOperationException>().Which;

            exception.Message.Should().StartWith(DatabaseGenerationResources.ErrorNonValidSsdl.Replace("{0}", string.Empty));
            IList<EdmSchemaError> exceptionData = (IList<EdmSchemaError>)exception.Data["ssdlErrors"];
            exceptionData.Count.Should().Be(3);
            exceptionData.All(e => exception.Message.Contains(e.Message)).Should().BeTrue();
        }

        [TestMethod]
        public void CreateAndValidateStoreItemCollection_rewrites_exception_for_naming_conflicts_when_catchThrowNamingConflicts_true()
        {
            XDocument invalidSsdl = XDocument.Parse(Ssdl);
            var entityTypeElement =
                invalidSsdl.Descendants("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}EntityType").Single();
            entityTypeElement.AddAfterSelf(new XElement("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}InvalidElement"));
            entityTypeElement.AddAfterSelf(entityTypeElement);

            Action act = () => EdmExtension.CreateAndValidateStoreItemCollection(
                invalidSsdl.ToString(),
                new Version(3, 0, 0, 0),
                resolver,
                catchThrowNamingConflicts: true);
            var exception = act.Should().Throw<InvalidOperationException>().Which;

            IList<EdmSchemaError> exceptionData = (IList<EdmSchemaError>)exception.Data["ssdlErrors"];
            exception.Message.Should().Be(string.Format(DatabaseGenerationResources.ErrorNameCollision, exceptionData[0].Message));
            exceptionData.Count.Should().Be(3);
            exceptionData[0].Message.Should().Contain("'AdventureWorksModel.Store.Entities'");
            exceptionData[1].Message.Should().Contain("InvalidElement");
            exceptionData[2].Message.Should().Contain("InvalidElement");
        }

        [TestMethod]
        public void CreateAndValidateStoreItemCollection_creates_StoreItemCollection_for_valid_ssdl_and_targetFrameworkVersion()
        {
            var storeItemCollection =
                EdmExtension.CreateAndValidateStoreItemCollection(
                    Ssdl,
                    new Version(3, 0, 0, 0),
                    resolver,
                    catchThrowNamingConflicts: true);

            storeItemCollection.Should().NotBeNull();
            storeItemCollection.GetItem<EntityType>("AdventureWorksModel.Store.Entities").Should().NotBeNull();
        }

        [TestMethod]
        public void CreateStorageMappingItemCollection_returns_errors_for_invalid_ssdl()
        {
            Version v3 = new Version(3, 0, 0, 0);
            var edmItemCollection = EdmExtension.CreateAndValidateEdmItemCollection(Csdl, v3);
            var storeItemCollection =
                EdmExtension.CreateAndValidateStoreItemCollection(
                    Ssdl,
                    v3,
                    resolver,
                    false);

            XDocument invalidMsl = XDocument.Parse(Msl);
            invalidMsl
                .Descendants("{http://schemas.microsoft.com/ado/2009/11/mapping/cs}ScalarProperty")
                .First()
                .SetAttributeValue("Name", "Non-existing-property");

            var storageMappingItemCollection =
                EdmExtension.CreateStorageMappingItemCollection(
                    edmItemCollection,
                    storeItemCollection,
                    invalidMsl.ToString(),
                    out IList<EdmSchemaError> edmErrors);

            storageMappingItemCollection.Should().BeNull();
            edmErrors.Count.Should().Be(1);
            edmErrors[0].Message.Should().Contain("Non-existing-property");
        }

        [TestMethod]
        public void CreateStorageMappingItemCollection_creates_storage_mapping_item_collection_for_valid_artifacts()
        {
            Version v3 = new Version(3, 0, 0, 0);
            var edmItemCollection = EdmExtension.CreateAndValidateEdmItemCollection(Csdl, v3);
            var storeItemCollection =
                EdmExtension.CreateAndValidateStoreItemCollection(
                    Ssdl,
                    v3,
                    resolver,
                    false);

            var storageMappingItemCollection = EdmExtension.CreateStorageMappingItemCollection(
                edmItemCollection, storeItemCollection, Msl, out IList<EdmSchemaError> edmErrors);

            storageMappingItemCollection.Should().NotBeNull();
            edmErrors.Count.Should().Be(0);
            storageMappingItemCollection.GetItem<GlobalItem>("AdventureWorksEntities3").Should().NotBeNull();
        }

        [TestClass]
        public class CopyToSSDLTests
        {
            [TestMethod]
            public void CopyToSSDL_true_causes_CopyExtendedPropertiesToSsdlElement_to_copy_property()
            {
                foreach (var version in EntityFrameworkVersion.GetAllVersions())
                {
                    var csdlEntityTypeWithVersionedEdmxNamespaceCopyToSSDL = CreateEntityTypeWithExtendedProperty(
                        SchemaManager.GetEDMXNamespaceName(version), "true");
                    XElement ssdlEntityTypeElement = new XElement(
                        (XNamespace)(SchemaManager.GetSSDLNamespaceName(version)) + "EntityType",
                        new XAttribute("Name", "TestEntityType"));
                    OutputGeneratorHelpers.CopyExtendedPropertiesToSsdlElement(
                        csdlEntityTypeWithVersionedEdmxNamespaceCopyToSSDL, ssdlEntityTypeElement);
                    ssdlEntityTypeElement.Elements().First().ToString().Should().Be(
                        "<MyProp p1:MyAttribute=\"MyValue\" xmlns:p1=\"http://myExtendedProperties\" xmlns=\"http://myExtendedProperties\" />");
                }
            }

            [TestMethod]
            public void CopyToSSDL_false_causes_CopyExtendedPropertiesToSsdlElement_to_not_copy_property()
            {
                foreach (var version in EntityFrameworkVersion.GetAllVersions())
                {
                    var csdlEntityTypeWithVersionedEdmxNamespaceCopyToSSDL = CreateEntityTypeWithExtendedProperty(
                        SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3), "false");
                    XElement ssdlEntityTypeElement = new XElement(
                        (XNamespace)(SchemaManager.GetSSDLNamespaceName(EntityFrameworkVersion.Version3)) + "EntityType",
                        new XAttribute("Name", "TestEntityType"));
                    OutputGeneratorHelpers.CopyExtendedPropertiesToSsdlElement(
                        csdlEntityTypeWithVersionedEdmxNamespaceCopyToSSDL, ssdlEntityTypeElement);
                    ssdlEntityTypeElement.Elements().Should().BeEmpty();
                }
            }

            [TestMethod]
            public void CopyToSSDL_in_non_edmx_namespace_causes_CopyExtendedPropertiesToSsdlElement_to_not_copy_property()
            {
                var csdlEntityTypeWithNonEdmxNamespaceCopyToSSDL = CreateEntityTypeWithExtendedProperty(
                    "http://SomeOtherNamespace", "true");

                foreach (var version in EntityFrameworkVersion.GetAllVersions())
                {
                    XElement ssdlEntityTypeElement = new XElement(
                        (XNamespace)(SchemaManager.GetSSDLNamespaceName(EntityFrameworkVersion.Version3)) + "EntityType",
                        new XAttribute("Name", "TestEntityType"));
                    OutputGeneratorHelpers.CopyExtendedPropertiesToSsdlElement(
                        csdlEntityTypeWithNonEdmxNamespaceCopyToSSDL, ssdlEntityTypeElement);
                    ssdlEntityTypeElement.Elements().Should().BeEmpty();
                }
            }

            private static EntityType CreateEntityTypeWithExtendedProperty(XNamespace copyToSSDLNamespace, string copyToSSDLValue)
            {
                XElement extendedPropertyContents =
                    new XElement(
                        (XNamespace)"http://myExtendedProperties" + "MyProp",
                        new XAttribute(
                            (XNamespace)"http://myExtendedProperties" + "MyAttribute", "MyValue"),
                        new XAttribute(
                            copyToSSDLNamespace + "CopyToSSDL", copyToSSDLValue));
                MetadataProperty extendedPropertyMetadataProperty =
                    MetadataProperty.Create(
                        "http://myExtendedProperties:MyProp",
                        TypeUsage.CreateStringTypeUsage(
                            PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String),
                            true,
                            false),
                        extendedPropertyContents
                        );
                return EntityType.Create(
                    "TestEntityType",
                    "Model1",
                    DataSpace.CSpace,
                    new[] { "Id" },
                    new[] { EdmProperty.CreatePrimitive("Id", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)) },
                    new[] { extendedPropertyMetadataProperty });
            }
        }
    }
}
