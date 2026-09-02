// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Tests.Design.Edmx.Commands
{
    [TestClass]
    public class CreatePropertyCommandTests
    {
        [TestMethod]
        public void CreateProperty_sets_Name_attribute_before_type_for_conceptual_property()
        {
            var parentEntity = CreateEntityType<ConceptualEntityType>();
            CreatePropertyCommand createPropertyCommand = new CreatePropertyCommand("test", parentEntity, "Int32", true, null);

            using (var property = createPropertyCommand.CreateProperty())
            {
                property.Should().BeOfType<ConceptualProperty>();
                parentEntity.Properties().Should().Contain(property);
                property.XElement.ToString().Should().Be(
                    "<Property Name=\"test\" Type=\"Int32\" Nullable=\"true\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" />");
            }
        }

        [TestMethod]
        public void CreateProperty_sets_Name_attribute_before_type_for_store_property()
        {
            var parentEntity = CreateEntityType<StorageEntityType>();
            CreatePropertyCommand createPropertyCommand = new CreatePropertyCommand("test", parentEntity, "Int32", false, null);
            using (var property = createPropertyCommand.CreateProperty())
            {
                property.Should().BeOfType<StorageProperty>();
                parentEntity.Properties().Should().Contain(property);
                property.XElement.ToString().Should().Be(
                    "<Property Name=\"test\" Type=\"Int32\" Nullable=\"false\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />");
            }
        }

        [TestMethod]
        public void CreateProperty_does_not_create_Nullable_attribute_if_no_value()
        {
            var parentEntity = CreateEntityType<ConceptualEntityType>();
            CreatePropertyCommand createPropertyCommand = new CreatePropertyCommand("test", parentEntity, "Int32", null, null);
            using (var property = createPropertyCommand.CreateProperty())
            {
                property.XElement.ToString().Should().Be(
                    "<Property Name=\"test\" Type=\"Int32\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" />");
            }
        }

        [TestMethod]
        public void CreateProperty_uses_insert_order_when_inserting_properties()
        {
            using (var parentEntity = CreateEntityType<ConceptualEntityType>())
            {
                Mock<ConceptualEntityModel> mockModel = new Mock<ConceptualEntityModel>(
                    parentEntity.Artifact, new XElement(parentEntity.XElement.Name.Namespace + "Schema"));
                mockModel.Setup(m => m.Parent).Returns(parentEntity.Artifact);
                mockModel.Setup(m => m.NamespaceValue).Returns(string.Empty);

                using (var model = mockModel.Object)
                {
                    Mock<ConceptualEntityType> mockEntity = Mock.Get((ConceptualEntityType)parentEntity);
                    mockEntity.Setup(e => e.Parent).Returns(model);

                    List<Property> properties = new List<Property>();
                    try
                    {
                        properties.Add(
                            new CreatePropertyCommand("Property1", parentEntity, "Int32", true, null).CreateProperty());

                        properties.Add(
                            new CreatePropertyCommand(
                                "InsertedAfterFirst", parentEntity, "Int32", true,
                                new InsertPropertyPosition(properties.First(), false)).CreateProperty());

                        properties.Add(
                            new CreatePropertyCommand(
                                "InsertedBeforeLast", parentEntity, "Int32", true,
                                new InsertPropertyPosition(properties.Last(), true)).CreateProperty());

                        parentEntity.XElement.Elements().Select(e => (string)e.Attribute("Name"))
                            .Should().Equal(new[] { "Property1", "InsertedBeforeLast", "InsertedAfterFirst" });
                    }
                    finally
                    {
                        foreach (var property in properties.Where(p => p != null))
                        {
                            property.Dispose();
                        }
                    }
                }
            }
        }

        private static EntityType CreateEntityType<T>() where T : EntityType
        {
            XNamespace ns = typeof(T) == typeof(ConceptualEntityType)
                                ? SchemaManager.GetCSDLNamespaceName(EntityFrameworkVersion.Version3)
                                : SchemaManager.GetSSDLNamespaceName(EntityFrameworkVersion.Version3);

            Mock<XmlModelProvider> mockModelProvider = new Mock<XmlModelProvider>();
            Mock<ModelManager> mockModelManager =
                new Mock<ModelManager>(new Mock<IEFArtifactFactory>().Object, new Mock<IEFArtifactSetFactory>().Object);
            mockModelManager.Setup(m => m.GetRootNamespace(It.IsAny<EFObject>())).Returns(ns);

            Mock<AttributeContentValidator> mockValidator = new Mock<AttributeContentValidator>(null);
            mockModelManager.Setup(m => m.GetAttributeContentValidator(It.IsAny<EFArtifact>()))
                .Returns(mockValidator.Object);

            Mock<EntityDesignArtifact> mockArtifact = new Mock<EntityDesignArtifact>(mockModelManager.Object, new Uri("http://tempuri"), mockModelProvider.Object);
            Mock<EFArtifactSet> mockArtifactSet = new Mock<EFArtifactSet>(mockArtifact.Object) { CallBase = true };

            mockArtifact.Setup(m => m.SchemaVersion).Returns(EntityFrameworkVersion.Version3);
            mockArtifact.Setup(m => m.ArtifactSet).Returns(mockArtifactSet.Object);
            mockModelManager.Setup(m => m.GetArtifactSet(It.IsAny<Uri>())).Returns(mockArtifactSet.Object);

            Mock<T> mockEntity = new Mock<T>(null, new XElement(ns + "EntityType")) { CallBase = true };
            mockEntity.Setup(e => e.Parent).Returns(mockArtifact.Object);
            mockEntity.Setup(e => e.Artifact).Returns(mockArtifact.Object);

            return mockEntity.Object;
        }
    }
}
