// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Tools.XmlDesignerBase;
using Moq;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Design.Model.Validation
{
    [TestClass]
    public class ErrorInfoTests
    {
        [TestMethod]
        public void Edmx_ErrorInfo_initialized_correctly()
        {
            Mock<EFObject> mockEFObject = new Mock<EFObject>();
            mockEFObject.Setup(o => o.GetLineNumber()).Returns(76);
            mockEFObject.Setup(o => o.GetColumnNumber()).Returns(12);
            mockEFObject.Setup(o => o.Uri).Returns(new Uri(@"c:\project\model.edmx"));

            ErrorInfo edmxErrorInfo =
                new ErrorInfo(ErrorInfo.Severity.ERROR, "test", mockEFObject.Object, 42, ErrorClass.Runtime_CSDL);

            edmxErrorInfo.GetLineNumber().Should().Be(76);
            edmxErrorInfo.GetColumnNumber().Should().Be(12);
            edmxErrorInfo.IsError().Should().BeTrue();
            edmxErrorInfo.IsWarning().Should().BeFalse();
            edmxErrorInfo.IsInfo().Should().BeFalse();
            edmxErrorInfo.Message.Should().Be(string.Format(Resources.Error_Message_With_Error_Code_Prefix, 42, "test"));
            edmxErrorInfo.Item.Should().BeSameAs(mockEFObject.Object);
            edmxErrorInfo.ItemPath.Should().Be(@"c:\project\model.edmx");
            edmxErrorInfo.ErrorCode.Should().Be(42);
            edmxErrorInfo.ErrorClass.Should().Be(ErrorClass.Runtime_CSDL);
        }

        [TestMethod]
        public void Code_first_ErrorInfo_initialized_correctly()
        {
            ErrorInfo edmxErrorInfo =
                new ErrorInfo(ErrorInfo.Severity.WARNING, "test", @"c:\project\model.edmx" , 17, ErrorClass.None);

            edmxErrorInfo.GetLineNumber().Should().Be(0);
            edmxErrorInfo.GetColumnNumber().Should().Be(0);
            edmxErrorInfo.IsError().Should().BeFalse();
            edmxErrorInfo.IsWarning().Should().BeTrue();
            edmxErrorInfo.IsInfo().Should().BeFalse();

            edmxErrorInfo.Message.Should().Be(string.Format(Resources.Error_Message_With_Error_Code_Prefix, 17, "test"));
            edmxErrorInfo.Item.Should().BeNull();
            edmxErrorInfo.ItemPath.Should().Be(@"c:\project\model.edmx");
            edmxErrorInfo.ErrorCode.Should().Be(17);
            edmxErrorInfo.ErrorClass.Should().Be(ErrorClass.None);
        }
    }
}
