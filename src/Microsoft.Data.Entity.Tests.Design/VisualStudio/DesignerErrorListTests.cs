// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class DesignerErrorListTests
    {
        [TestMethod]
        public void DesignerErrorList_creates_non_null_ErrorListProvider()
        {
            new DesignerErrorList(new Mock<IServiceProvider>().Object).Provider.Should().NotBeNull();
        }

        [TestMethod]
        public void Can_add_clear_error_list_tasks()
        {
            Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(p => p.GetService(typeof(SVsTaskList))).Returns(new Mock<IVsTaskList>().Object);

            ErrorTask task = new ErrorTask();
            DesignerErrorList errorList = new DesignerErrorList(mockServiceProvider.Object);

            errorList.Provider.Tasks.Cast<ErrorTask>().Should().BeEmpty();
            errorList.AddItem(task);
            errorList.Provider.Tasks.Cast<ErrorTask>().Should().BeEquivalentTo(new[] { task });
            errorList.Clear();
            errorList.Provider.Tasks.Cast<ErrorTask>().Should().BeEmpty();
        }
    }
}
