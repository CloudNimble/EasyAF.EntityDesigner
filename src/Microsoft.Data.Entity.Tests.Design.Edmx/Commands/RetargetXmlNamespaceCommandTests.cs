// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model.Commands
{
    [TestClass]
    public class RetargetXmlNamespaceCommandTests
    {
        [TestMethod]
        public void RetargetWithMetadataConverter_does_not_modify_xml_if_converter_returns_null()
        {
            foreach (var schemaVersion in EntityFrameworkVersion.GetAllVersions())
            {
                XDocument model = new XDocument(new XElement("root"));
                model.Changed +=
                    (sender, args) => { throw new InvalidOperationException("Unexpected changes to model."); };

                Mock<MetadataConverterDriver> mockConverter = new Mock<MetadataConverterDriver>();
                mockConverter
                    .Setup(c => c.Convert(It.IsAny<XmlDocument>(), It.IsAny<Version>()))
                    .Returns(
                        (XmlDocument doc, Version version) =>
                            {
                                version.Should().BeSameAs(schemaVersion);
                                return null;
                            });

                RetargetXmlNamespaceCommand.RetargetWithMetadataConverter(model, schemaVersion, mockConverter.Object);
            }
        }

        [TestMethod]
        public void RetargetWithMetadataConverter_replaces_source_document_with_document_returned_from_converter()
        {
            const string convertedModelXml = "<newModel><parts /></newModel>";
            XDocument model = new XDocument(new XElement("root"));

            Mock<MetadataConverterDriver> mockConverter = new Mock<MetadataConverterDriver>();
            mockConverter
                .Setup(c => c.Convert(It.IsAny<XmlDocument>(), It.IsAny<Version>()))
                .Returns(
                    (XmlDocument doc, Version version) =>
                        {
                            XmlDocument convertedModel = new XmlDocument();
                            convertedModel.LoadXml(convertedModelXml);
                            return convertedModel;
                        });

            RetargetXmlNamespaceCommand.RetargetWithMetadataConverter(model, EntityFrameworkVersion.Version3, mockConverter.Object);
            XNode.DeepEquals(XDocument.Parse("<!---->\n" + convertedModelXml), model).Should().BeTrue();
        }
    }
}
