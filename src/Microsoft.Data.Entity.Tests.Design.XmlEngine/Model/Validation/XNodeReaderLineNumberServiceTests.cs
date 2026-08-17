// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Design.Model.Validation
{
    [TestClass]
    public class XNodeReaderLineNumberServiceTests
    {
        [TestMethod]
        public void HasLineInfo_returns_true()
        {
            // we use reflection inside XNodeReaderLineNumberService. If this test fails it means that the class
            // we reflect on changed and the XNodeReaderLineNumberService needs to be updated accordingly.

            Mock<XmlModelProvider> mockModelProvider = new Mock<XmlModelProvider>();
            using (var reader = new XElement("dummy").CreateReader())
            {
                using (var modelProvider = mockModelProvider.Object)
                {
                    new XNodeReaderLineNumberService(modelProvider, reader, new Uri("urn:abc")).HasLineInfo().Should().BeTrue();
                }
            }
        }

        [TestMethod]
        public void LineNumber_and_LinePosition_return_line_and_column_number_for_element_if_XNodeReader_positioned_on_element()
        {
            XElement inputXml = new XElement("dummy", "value");

            Mock<XmlModelProvider> mockModelProvider = new Mock<XmlModelProvider>();
            mockModelProvider
                .Setup(m => m.GetTextSpanForXObject(It.Is<XObject>(x => x == inputXml), It.IsAny<Uri>()))
                .Returns(new TextSpan { iStartLine = 21, iStartIndex = 42, });

            using (var reader = inputXml.CreateReader())
            {
                reader.Read();
                reader.NodeType.Should().Be(XmlNodeType.Element);
                reader.Name.Should().Be("dummy");

                using (var modelProvider = mockModelProvider.Object)
                {
                    new XNodeReaderLineNumberService(modelProvider, reader, new Uri("urn:abc")).LineNumber.Should().Be(21);
                    new XNodeReaderLineNumberService(modelProvider, reader, new Uri("urn:abc")).LinePosition.Should().Be(42);
                }
            }
        }

        [TestMethod]
        public void LineNumber_and_LinePosition_return_line_and_column_number_for_text_if_XNodeReader_positioned_on_text_node()
        {
            XElement inputXml = new XElement("dummy", "value");

            Mock<XmlModelProvider> mockModelProvider = new Mock<XmlModelProvider>();
            mockModelProvider
                .Setup(m => m.GetTextSpanForXObject(It.Is<XObject>(x => x == inputXml.FirstNode), It.IsAny<Uri>()))
                .Returns(new TextSpan { iStartLine = 21, iStartIndex = 42, });

            using (var reader = inputXml.CreateReader())
            {
                reader.Read();
                reader.Read();
                reader.NodeType.Should().Be(XmlNodeType.Text);
                reader.Value.Should().Be("value");

                using (var modelProvider = mockModelProvider.Object)
                {
                    new XNodeReaderLineNumberService(modelProvider, reader, new Uri("urn:abc")).LineNumber.Should().Be(21);
                    new XNodeReaderLineNumberService(modelProvider, reader, new Uri("urn:abc")).LinePosition.Should().Be(42);
                }
            }
        }
    }
}
