// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Design.Model.Validation
{
    [TestClass]
    public class XmlReaderProxyTests
    {
        [TestMethod]
        public void HasLineInfo_returns_lineNumberService_HasLineInfo_lineNumberService_not_null()
        {
            Mock<IXmlLineInfo> lineNumberServiceMock = new Mock<IXmlLineInfo>();
            lineNumberServiceMock
                .Setup(l => l.HasLineInfo())
                .Returns(true);

            new XmlReaderProxy(new Mock<XmlReader>().Object, new Uri("http://tempuri"), lineNumberServiceMock.Object)
                .HasLineInfo().Should().BeTrue();

            lineNumberServiceMock.Verify(l => l.HasLineInfo(), Times.Once());
        }

        [TestMethod]
        public void HasLineInfo_calls_into_underlying_XmlReader_HasLineInfo_if_XmlReader_implements_IXmlLineInfo_and_lineNumberService_null(
            )
        {
            Mock<XmlReader> xmlReaderMock = new Mock<XmlReader>();
            var xmlLineInfoMock = xmlReaderMock.As<IXmlLineInfo>();
            xmlLineInfoMock
                .Setup(l => l.HasLineInfo())
                .Returns(true);

            new XmlReaderProxy(xmlReaderMock.Object, new Uri("http://tempuri"), null)
                .HasLineInfo().Should().BeTrue();

            xmlLineInfoMock.Verify(l => l.HasLineInfo(), Times.Once());
        }

        [TestMethod]
        public void HasLineInfo_returns_false_if_lineNumberService_null_and_XmlReader_is_not_IXmlLineInfo()
        {
            new XmlReaderProxy(new Mock<XmlReader>().Object, new Uri("http://tempuri"), null)
                .HasLineInfo().Should().BeFalse();
        }

        [TestMethod]
        public void LineNumber_returns_lineNumberService_LineNumber_lineNumberService_not_null()
        {
            Mock<IXmlLineInfo> lineNumberServiceMock = new Mock<IXmlLineInfo>();
            lineNumberServiceMock
                .Setup(l => l.LineNumber)
                .Returns(42);

            new XmlReaderProxy(new Mock<XmlReader>().Object, new Uri("http://tempuri"), lineNumberServiceMock.Object)
                .LineNumber.Should().Be(42);

            lineNumberServiceMock.Verify(l => l.LineNumber, Times.Once());
        }

        [TestMethod]
        public void LineNumber_calls_into_underlying_XmlReader_LineNumber_if_XmlReader_implements_IXmlLineInfo_and_lineNumberService_null()
        {
            Mock<XmlReader> xmlReaderMock = new Mock<XmlReader>();
            var xmlLineInfoMock = xmlReaderMock.As<IXmlLineInfo>();
            xmlLineInfoMock
                .Setup(l => l.LineNumber)
                .Returns(42);

            new XmlReaderProxy(xmlReaderMock.Object, new Uri("http://tempuri"), null)
                .LineNumber.Should().Be(42);

            xmlLineInfoMock.Verify(l => l.LineNumber, Times.Once());
        }

        [TestMethod]
        public void LineNumber_returns_0_if_lineNumberService_null_and_XmlReader_is_not_IXmlLineInfo()
        {
            new XmlReaderProxy(new Mock<XmlReader>().Object, new Uri("http://tempuri"), null)
                .LineNumber.Should().Be(0);
        }

        [TestMethod]
        public void LinePosition_returns_LinePositionService_LinePosition_LinePositionService_not_null()
        {
            Mock<IXmlLineInfo> linePositionServiceMock = new Mock<IXmlLineInfo>();
            linePositionServiceMock
                .Setup(l => l.LinePosition)
                .Returns(42);

            new XmlReaderProxy(new Mock<XmlReader>().Object, new Uri("http://tempuri"), linePositionServiceMock.Object)
                .LinePosition.Should().Be(42);

            linePositionServiceMock.Verify(l => l.LinePosition, Times.Once());
        }

        [TestMethod]
        public void
            LinePosition_calls_into_underlying_XmlReader_LinePosition_if_XmlReader_implements_IXmlLineInfo_and_LinePositionService_null()
        {
            Mock<XmlReader> xmlReaderMock = new Mock<XmlReader>();
            var xmlLineInfoMock = xmlReaderMock.As<IXmlLineInfo>();
            xmlLineInfoMock
                .Setup(l => l.LinePosition)
                .Returns(42);

            new XmlReaderProxy(xmlReaderMock.Object, new Uri("http://tempuri"), null)
                .LinePosition.Should().Be(42);

            xmlLineInfoMock.Verify(l => l.LinePosition, Times.Once());
        }

        [TestMethod]
        public void LinePosition_returns_0_if_LinePositionService_null_and_XmlReader_is_not_IXmlLineInfo()
        {
            new XmlReaderProxy(new Mock<XmlReader>().Object, new Uri("http://tempuri"), null)
                .LinePosition.Should().Be(0);
        }
    }
}
