// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.Model.Entity
{
    using System;
    using System.Data.Entity.SqlServer;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Data.Entity.Design.VersioningFacade;
    using Microsoft.Data.Tools.XmlDesignerBase.Model;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class StorageEntityModelTests
    {
        [TestMethod, Ignore("Updated binary has updated types")]
        public void StoreTypeNameToStoreTypeMap_returns_type_map()
        {
            var ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (var storageModel = new StorageEntityModel(null, ssdl))
            {
                var typeMap = storageModel.StoreTypeNameToStoreTypeMap;

                Assert.Equal(
                    SqlProviderServices.Instance.GetProviderManifest("2008").GetStoreTypes().Select(t => t.Name),
                    typeMap.Keys);

                typeMap.Any(t => t.Key != t.Value.Name.Should().BeFalse());
            }
        }

        [TestMethod]
        public void XNamespace_returns_element_namespace_if_element_not_null()
        {
            var element = new XElement("{urn:tempuri}element");
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var entityDesignArtifactMock = new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);
            entityDesignArtifactMock.Setup(a => a.SchemaVersion).Returns(EntityFrameworkVersion.Version3);

            using (var storageModel = new StorageEntityModel(entityDesignArtifactMock.Object, element))
            {
                storageModel.XNamespace.Should().BeSameAs(element.Name.Namespace);
            }
        }

        [TestMethod]
        public void XNamespace_returns_root_namespace_if_element_null()
        {
            var tmpElement = new XElement("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}Schema");

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var enityDesignArtifiact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider)
                    {
                        CallBase = true
                    }.Object;

            enityDesignArtifiact.SetXObject(
                XDocument.Parse("<Edmx xmlns=\"http://schemas.microsoft.com/ado/2009/11/edmx\" />"));

            using (var storageModel = new StorageEntityModel(enityDesignArtifiact, tmpElement))
            {
                storageModel.SetXObject(null);
                storageModel.XNamespace.Should().Be("http://schemas.microsoft.com/ado/2009/11/edm/ssdl");

                // resetting the element is required for clean up
                storageModel.SetXObject(tmpElement);
            }
        }

        [TestMethod]
        public void GetStoragePrimitiveType_returns_type_name_for_valid_type()
        {
            var ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (var storageModel = new StorageEntityModel(null, ssdl))
            {
                Assert.Equal(
                    "tinyint",
                    storageModel.GetStoragePrimitiveType("tinyint").Name);
            }
        }

        [TestMethod]
        public void GetStoragePrimitiveType_returns_null_for_unknown_type()
        {
            var ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (var storageModel = new StorageEntityModel(null, ssdl))
            {
                storageModel.GetStoragePrimitiveType("foo".Should().BeNull());
            }
        }

        [TestMethod]
        public void StoreTypeNameToStoreTypeMap_works_with_MicrosoftDataSqlClient()
        {
            // This test verifies that the provider resolution works correctly for Microsoft.Data.SqlClient
            // The StoreTypeNameToStoreTypeMap property triggers DependencyResolver.GetService<DbProviderServices>(Provider.Value)
            // which should return SqlProviderServices (not LegacyDbProviderServicesWrapper)
            var ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"Microsoft.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (var storageModel = new StorageEntityModel(null, ssdl))
            {
                // This should NOT throw - if Microsoft.Data.SqlClient isn't properly registered,
                // it would try to use LegacyDbProviderServicesWrapper which would fail
                var typeMap = storageModel.StoreTypeNameToStoreTypeMap;

                typeMap.Should().NotBeNull();
                typeMap.Count > 0.Should().BeTrue();

                // Verify we get the expected SQL Server types
                typeMap.ContainsKey("int".Should().BeTrue());
                typeMap.ContainsKey("varchar".Should().BeTrue());
                typeMap.ContainsKey("nvarchar".Should().BeTrue());
                typeMap.ContainsKey("datetime".Should().BeTrue());
            }
        }

        [TestMethod]
        public void GetStoragePrimitiveType_works_with_MicrosoftDataSqlClient()
        {
            var ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"Microsoft.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (var storageModel = new StorageEntityModel(null, ssdl))
            {
                // This exercises the full code path: Provider.Value -> DependencyResolver -> SqlProviderServices
                Assert.Equal("int", storageModel.GetStoragePrimitiveType("int").Name);
                Assert.Equal("nvarchar", storageModel.GetStoragePrimitiveType("nvarchar").Name);
            }
        }

        [TestMethod]
        public void MicrosoftDataSqlClient_and_SystemDataSqlClient_return_same_types()
        {
            var ssdlMds =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"Microsoft.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");
            var ssdlSds =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (var mdsModel = new StorageEntityModel(null, ssdlMds))
            using (var sdsModel = new StorageEntityModel(null, ssdlSds))
            {
                // Both providers should return the same store types since they both use SqlProviderServices
                var mdsTypes = mdsModel.StoreTypeNameToStoreTypeMap;
                var sdsTypes = sdsModel.StoreTypeNameToStoreTypeMap;

                mdsTypes.Count.Should().Be(sdsTypes.Count);
                foreach (var typeName in sdsTypes.Keys)
                {
                    mdsTypes.ContainsKey(typeName.Should().BeTrue(), $"Microsoft.Data.SqlClient missing type: {typeName}");
                }
            }
        }
    }
}
