// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data.Entity.Core.Common;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Tests.Design.Edmx.Entity
{
    [TestClass]
    public class StorageEntityModelTests
    {
        private static DbProviderServices SqlProviderServicesInstance
        {
            get
            {
                Type type = Type.GetType("System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer");
                if (type != null)
                {
                    var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    return (DbProviderServices)instanceProperty?.GetValue(null);
                }
                return null;
            }
        }

        [TestMethod, Ignore("Updated binary has updated types")]
        public void StoreTypeNameToStoreTypeMap_returns_type_map()
        {
            XElement ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (StorageEntityModel storageModel = new StorageEntityModel(null, ssdl))
            {
                var typeMap = storageModel.StoreTypeNameToStoreTypeMap;

                typeMap.Keys.Should().BeEquivalentTo(
                    SqlProviderServicesInstance.GetProviderManifest("2008").GetStoreTypes().Select(t => t.Name));

                typeMap.Any(t => t.Key != t.Value.Name).Should().BeFalse();
            }
        }

        [TestMethod]
        public void XNamespace_returns_element_namespace_if_element_not_null()
        {
            XElement element = new XElement("{urn:tempuri}element");
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock = new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);
            entityDesignArtifactMock.Setup(a => a.SchemaVersion).Returns(EntityFrameworkVersion.Version3);

            using (StorageEntityModel storageModel = new StorageEntityModel(entityDesignArtifactMock.Object, element))
            {
                storageModel.XNamespace.Should().BeSameAs(element.Name.Namespace);
            }
        }

        [TestMethod]
        public void XNamespace_returns_root_namespace_if_element_null()
        {
            XElement tmpElement = new XElement("{http://schemas.microsoft.com/ado/2009/11/edm/ssdl}Schema");

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var enityDesignArtifiact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider)
                    {
                        CallBase = true
                    }.Object;

            enityDesignArtifiact.SetXObject(
                XDocument.Parse("<Edmx xmlns=\"http://schemas.microsoft.com/ado/2009/11/edmx\" />"));

            using (StorageEntityModel storageModel = new StorageEntityModel(enityDesignArtifiact, tmpElement))
            {
                storageModel.SetXObject(null);
                storageModel.XNamespace.NamespaceName.Should().Be("http://schemas.microsoft.com/ado/2009/11/edm/ssdl");

                // resetting the element is required for clean up
                storageModel.SetXObject(tmpElement);
            }
        }

        [TestMethod]
        public void GetStoragePrimitiveType_returns_type_name_for_valid_type()
        {
            XElement ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (StorageEntityModel storageModel = new StorageEntityModel(null, ssdl))
            {
                storageModel.GetStoragePrimitiveType("tinyint").Name.Should().Be("tinyint");
            }
        }

        [TestMethod]
        public void GetStoragePrimitiveType_returns_null_for_unknown_type()
        {
            XElement ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (StorageEntityModel storageModel = new StorageEntityModel(null, ssdl))
            {
                storageModel.GetStoragePrimitiveType("foo").Should().BeNull();
            }
        }

        [TestMethod]
        public void StoreTypeNameToStoreTypeMap_works_with_MicrosoftDataSqlClient()
        {
            // This test verifies that the provider resolution works correctly for Microsoft.Data.SqlClient
            // The StoreTypeNameToStoreTypeMap property triggers DependencyResolver.GetService<DbProviderServices>(Provider.Value)
            // which should return SqlProviderServices (not LegacyDbProviderServicesWrapper)
            XElement ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"Microsoft.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (StorageEntityModel storageModel = new StorageEntityModel(null, ssdl))
            {
                // This should NOT throw - if Microsoft.Data.SqlClient isn't properly registered,
                // it would try to use LegacyDbProviderServicesWrapper which would fail
                var typeMap = storageModel.StoreTypeNameToStoreTypeMap;

                typeMap.Should().NotBeNull();
                (typeMap.Count > 0).Should().BeTrue();

                // Verify we get the expected SQL Server types
                typeMap.ContainsKey("int").Should().BeTrue();
                typeMap.ContainsKey("varchar").Should().BeTrue();
                typeMap.ContainsKey("nvarchar").Should().BeTrue();
                typeMap.ContainsKey("datetime").Should().BeTrue();
            }
        }

        [TestMethod]
        public void GetStoragePrimitiveType_works_with_MicrosoftDataSqlClient()
        {
            XElement ssdl =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"Microsoft.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (StorageEntityModel storageModel = new StorageEntityModel(null, ssdl))
            {
                // This exercises the full code path: Provider.Value -> DependencyResolver -> SqlProviderServices
                storageModel.GetStoragePrimitiveType("int").Name.Should().Be("int");
                storageModel.GetStoragePrimitiveType("nvarchar").Name.Should().Be("nvarchar");
            }
        }

        [TestMethod]
        public void MicrosoftDataSqlClient_and_SystemDataSqlClient_return_same_types()
        {
            XElement ssdlMds =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"Microsoft.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");
            XElement ssdlSds =
                XElement.Parse(
                    "<Schema Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" Alias=\"Self\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");

            using (StorageEntityModel mdsModel = new StorageEntityModel(null, ssdlMds))
            using (StorageEntityModel sdsModel = new StorageEntityModel(null, ssdlSds))
            {
                // Both providers should return the same store types since they both use SqlProviderServices
                var mdsTypes = mdsModel.StoreTypeNameToStoreTypeMap;
                var sdsTypes = sdsModel.StoreTypeNameToStoreTypeMap;

                mdsTypes.Count.Should().Be(sdsTypes.Count);
                foreach (var typeName in sdsTypes.Keys)
                {
                    mdsTypes.ContainsKey(typeName).Should().BeTrue($"Microsoft.Data.SqlClient missing type: {typeName}");
                }
            }
        }
    }
}
