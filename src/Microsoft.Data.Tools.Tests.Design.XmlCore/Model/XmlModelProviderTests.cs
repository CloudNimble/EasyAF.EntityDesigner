// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Tools.Tests.Design.XmlCore.Model
{
    using System;
    using System.Xml.Linq;
    using Microsoft.Data.Tools.XmlDesignerBase.Model;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using FluentAssertions;

    [TestClass]
    public class XmlModelProviderTests
    {
        [TestMethod]
        public void GetTextSpanForXObject_calls_XmlModel_GetTextSpan_if_xobject_not_null()
        {
            var textSpan = new TextSpan { iStartLine = 42, iStartIndex = 43, iEndLine = 44, iEndIndex = 45 };

            var xmlModelMock = new Mock<XmlModel>();
            xmlModelMock
                .Setup(m => m.GetTextSpan(It.IsAny<XObject>()))
                .Returns(textSpan);

            var modelProviderMock = new Mock<XmlModelProvider> { CallBase = true };
            modelProviderMock
                .Setup(m => m.GetXmlModel(It.IsAny<Uri>()))
                .Returns(xmlModelMock.Object);

            modelProviderMock.Object.GetTextSpanForXObject(new XText("2.71828"), new Uri("http://tempuri"))
                .Should().Be(textSpan);
        }

        [TestMethod]
        public void GetTextSpanForXObject_creates_empty_TextSpan_if_xobject_null()
        {
            var textSpan = new Mock<XmlModelProvider> { CallBase = true }.Object
                .GetTextSpanForXObject(null, new Uri("http://tempuri"));

            textSpan.iStartLine.Should().Be(0);
            textSpan.iStartIndex.Should().Be(0);
            textSpan.iEndLine.Should().Be(0);
            textSpan.iEndIndex.Should().Be(0);
        }
    }
}
