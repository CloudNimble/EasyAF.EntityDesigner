// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.Model.Mapping
{
    using System;
    using System.Xml.Linq;
    using Microsoft.Data.Entity.Design.Model;
    using Microsoft.Data.Entity.Design.Model.Mapping;
    using Microsoft.Data.Entity.Design.VersioningFacade;
    using Microsoft.Data.Tools.XmlDesignerBase.Model;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using FluentAssertions;

    [TestClass]
    public class MappingModelTests
    {
        [TestMethod]
        public void XNamespace_returns_element_namespace_if_element_not_null()
        {
            var element = new XElement("{urn:tempuri}element");
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var entityDesignArtifactMock = new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);
            entityDesignArtifactMock.Setup(a => a.SchemaVersion).Returns(EntityFrameworkVersion.Version3);

            using (var mappingModel = new MappingModel(entityDesignArtifactMock.Object, element))
            {
                mappingModel.XNamespace.Should().BeSameAs(element.Name.Namespace);
            }
        }

        [TestMethod]
        public void XNamespace_returns_root_namespace_if_element_null()
        {
            var tmpElement = new XElement("{http://schemas.microsoft.com/ado/2009/11/mapping/cs}Schema");

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var enityDesignArtifiact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider)
                    {
                        CallBase = true
                    }.Object;

            enityDesignArtifiact.SetXObject(
                XDocument.Parse("<Edmx xmlns=\"http://schemas.microsoft.com/ado/2009/11/edmx\" />"));

            using (var mappingModel = new MappingModel(enityDesignArtifiact, tmpElement))
            {
                mappingModel.SetXObject(null);
                mappingModel.XNamespace.NamespaceName.Should().Be("http://schemas.microsoft.com/ado/2009/11/mapping/cs");

                // resetting the element is required for clean up
                mappingModel.SetXObject(tmpElement);
            }
        }
    }
}
