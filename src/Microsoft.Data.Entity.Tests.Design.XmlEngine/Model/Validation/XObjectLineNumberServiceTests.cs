// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Design.Model.Validation
{
    [TestClass]
    public class XObjectLineNumberServiceTests
    {
        [TestMethod]
        public void GetLineNumber_returns_line_number_from_text_span()
        {
            Mock<XmlModelProvider> mockModelProvider = new Mock<XmlModelProvider>();
            mockModelProvider
                .Setup(m => m.GetTextSpanForXObject(It.IsAny<XObject>(), It.IsAny<Uri>()))
                .Returns(new TextSpan { iStartLine = 42 });

            using (var mockModel = mockModelProvider.Object)
            {
                new XObjectLineNumberService(mockModel).GetLineNumber(null, null).Should().Be(42);
            }
        }

        [TestMethod]
        public void GetColumnNumber_returns_column_number_from_text_span()
        {
            Mock<XmlModelProvider> mockModelProvider = new Mock<XmlModelProvider>();
            mockModelProvider
                .Setup(m => m.GetTextSpanForXObject(It.IsAny<XObject>(), It.IsAny<Uri>()))
                .Returns(new TextSpan { iStartIndex = 42 });

            using (var mockModel = mockModelProvider.Object)
            {
                new XObjectLineNumberService(mockModel).GetColumnNumber(null, null).Should().Be(42);
            }
        }
    }
}
