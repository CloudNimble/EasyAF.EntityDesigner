// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Tests.Design.Model.Entity
{
    [TestClass]
    public class ConceptualEntityModelTests
    {
        [TestMethod]
        public void XNamespace_returns_element_namespace_if_element_not_null()
        {
            XElement element = new XElement("{urn:tempuri}element");
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock = new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);
            entityDesignArtifactMock.Setup(a => a.SchemaVersion).Returns(EntityFrameworkVersion.Version3);

            using (ConceptualEntityModel conceptualModel = new ConceptualEntityModel(entityDesignArtifactMock.Object, element))
            {
                conceptualModel.XNamespace.Should().BeSameAs(element.Name.Namespace);
            }
        }

        [TestMethod]
        public void XNamespace_returns_root_namespace_if_element_null()
        {
            XElement tmpElement = new XElement("{http://schemas.microsoft.com/ado/2009/11/edm}Schema");

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var enityDesignArtifiact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider)
                    {
                        CallBase = true
                    }.Object;

            enityDesignArtifiact.SetXObject(
                XDocument.Parse("<Edmx xmlns=\"http://schemas.microsoft.com/ado/2009/11/edmx\" />"));

            using (ConceptualEntityModel conceptualModel = new ConceptualEntityModel(enityDesignArtifiact, tmpElement))
            {
                conceptualModel.SetXObject(null);
                conceptualModel.XNamespace.NamespaceName.Should().Be("http://schemas.microsoft.com/ado/2009/11/edm");

                // resetting the element is required for clean up
                conceptualModel.SetXObject(tmpElement);
            }
        }
    }
}
