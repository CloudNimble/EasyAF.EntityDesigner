// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Tests.Design.Edmx
{
    [TestClass]
    public class EntityDesignArtifactTests
    {
        [TestMethod]
        public void GetModelGenErrors_on_EntityDesignArtifact_always_returns_null()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            using (EntityDesignArtifact artifact = new EntityDesignArtifact(modelManager, new Uri("urn:dummy"), modelProvider))
            {
                artifact.GetModelGenErrors().Should().BeNull();
            }
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_true_if_namespaces_in_sync()
        {
            // Only Version3 is supported for modern development
            DetermineIfArtifactIsVersionSafe(
                edmxVersion: EntityFrameworkVersion.Version3,
                storeModelVersion: EntityFrameworkVersion.Version3,
                conceptualModelVersion: EntityFrameworkVersion.Version3,
                mappingModelVersion: EntityFrameworkVersion.Version3).Should().BeTrue();
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_false_if_XDocument_root_has_unknown_namespace()
        {
            // Test version safety check when EDMX has an unknown namespace
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> mockEntityDesignArtifact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };

            // Use an unknown namespace to simulate a version mismatch
            mockEntityDesignArtifact
                .Setup(a => a.XDocument)
                .Returns(
                    new XDocument(
                        new XElement(
                            XName.Get("Edmx", "http://unknown.namespace.example.com"))));

            var artifact = mockEntityDesignArtifact.Object;
            artifact.DetermineIfArtifactIsVersionSafe();

            // Should be false since the namespace is unknown/invalid
            artifact.IsVersionSafe.Should().BeFalse();
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_true_if_models_not_set()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> mockEntityDesignArtifact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };

            mockEntityDesignArtifact
                .Setup(a => a.XDocument)
                .Returns(
                    new XDocument(
                        new XElement(
                            XName.Get(
                                "Edmx",
                                SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3)))));

            mockEntityDesignArtifact.Setup(m => m.ConceptualModel).Returns((ConceptualEntityModel)null);
            mockEntityDesignArtifact.Setup(m => m.StorageModel).Returns((StorageEntityModel)null);
            mockEntityDesignArtifact.Setup(m => m.MappingModel).Returns((MappingModel)null);

            var artifact = mockEntityDesignArtifact.Object;
            artifact.DetermineIfArtifactIsVersionSafe();

            artifact.IsVersionSafe.Should().BeTrue();
        }

        private static bool DetermineIfArtifactIsVersionSafe(
            Version edmxVersion, Version storeModelVersion, Version conceptualModelVersion, Version mappingModelVersion)
        {
            var artifact = SetupArtifact(edmxVersion, storeModelVersion, conceptualModelVersion, mappingModelVersion);
            artifact.DetermineIfArtifactIsVersionSafe();
            return artifact.IsVersionSafe;
        }

        private static EntityDesignArtifact SetupArtifact(
            Version edmxVersion, Version storeModelVersion, Version conceptualModelVersion, Version mappingModelVersion)
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> mockEntityDesignArtifact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };

            Mock<ConceptualEntityModel> mockConceptualModel =
                new Mock<ConceptualEntityModel>(
                    mockEntityDesignArtifact.Object,
                    new XElement(XName.Get("Schema", SchemaManager.GetCSDLNamespaceName(conceptualModelVersion))));

            mockConceptualModel
                .Setup(m => m.XNamespace)
                .Returns(SchemaManager.GetCSDLNamespaceName(conceptualModelVersion));

            mockEntityDesignArtifact
                .Setup(m => m.ConceptualModel)
                .Returns(mockConceptualModel.Object);

            Mock<StorageEntityModel> mockStoreModel =
                new Mock<StorageEntityModel>(
                    mockEntityDesignArtifact.Object,
                    new XElement(XName.Get("Schema", SchemaManager.GetSSDLNamespaceName(storeModelVersion))));

            mockStoreModel
                .Setup(m => m.XNamespace)
                .Returns(SchemaManager.GetSSDLNamespaceName(storeModelVersion));

            mockEntityDesignArtifact
                .Setup(m => m.StorageModel)
                .Returns(mockStoreModel.Object);

            Mock<MappingModel> mockMappingModel =
                new Mock<MappingModel>(
                    mockEntityDesignArtifact.Object,
                    new XElement(XName.Get("Mapping", SchemaManager.GetMSLNamespaceName(mappingModelVersion))));

            mockMappingModel
                .Setup(m => m.XNamespace)
                .Returns(SchemaManager.GetMSLNamespaceName(mappingModelVersion));

            mockEntityDesignArtifact
                .Setup(m => m.MappingModel)
                .Returns(mockMappingModel.Object);

            mockEntityDesignArtifact
                .Setup(a => a.XDocument)
                .Returns(
                    new XDocument(
                        new XElement(XName.Get("Edmx", SchemaManager.GetEDMXNamespaceName(edmxVersion)))));

            return mockEntityDesignArtifact.Object;
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_false_if_XDocument_is_null()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> mockEntityDesignArtifact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };

            mockEntityDesignArtifact
                .Setup(a => a.XDocument)
                .Returns((XDocument)null);

            var artifact = mockEntityDesignArtifact.Object;
            artifact.DetermineIfArtifactIsVersionSafe();

            artifact.IsVersionSafe.Should().BeFalse();
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_false_if_XDocument_does_not_have_root()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> mockEntityDesignArtifact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };

            mockEntityDesignArtifact
                .Setup(a => a.XDocument)
                .Returns(new XDocument());

            var artifact = mockEntityDesignArtifact.Object;
            artifact.DetermineIfArtifactIsVersionSafe();

            artifact.IsVersionSafe.Should().BeFalse();
        }
    }
}
