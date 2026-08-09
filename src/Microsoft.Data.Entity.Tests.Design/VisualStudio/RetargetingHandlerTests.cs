// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using EnvDTE;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Moq;
using Moq.Protected;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VSLangProj;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class RetargettingHandlerTests
    {
        [TestMethod]
        public void RetargetFilesInProject_retargets_Edmx_files_in_project()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.7.2", references: new Reference[0]);

            var projectItems =
                new[]
                    {
                        MockDTE.CreateProjectItem("C:\\model1.edmx"),
                        MockDTE.CreateProjectItem("D:\\model2.EDMX")
                    };

            var fileInfos =
                projectItems.Select(
                    i =>
                    new VSFileFinder.VSFileInfo
                        {
                            Hierarchy = mockDte.Hierarchy,
                            ItemId = mockDte.AddProjectItem(i),
                            Path = i.Object.get_FileNames(1)
                        }).ToArray();

            Mock<RetargetingHandler> mockRetargetingHandler =
                new Mock<RetargetingHandler>(mockDte.Hierarchy, mockDte.ServiceProvider)
                    {
                        CallBase = true
                    };

            mockRetargetingHandler
                .Protected()
                .Setup<IEnumerable<VSFileFinder.VSFileInfo>>("GetEdmxFileInfos")
                .Returns(fileInfos);

            mockRetargetingHandler
                .Protected()
                .Setup<XmlDocument>("RetargetFile", ItExpr.IsAny<string>(), ItExpr.IsAny<Version>()).Returns(new XmlDocument());

            mockRetargetingHandler
                .Protected()
                .Setup("WriteModifiedFiles", ItExpr.IsAny<Project>(), ItExpr.IsAny<Dictionary<string, object>>())
                .Callback(
                    (Project project, Dictionary<string, object> documentMap) =>
                    documentMap.Keys.Should().BeEquivalentTo(fileInfos.Select(f => f.Path)));

            mockRetargetingHandler.Object.RetargetFilesInProject();

            mockRetargetingHandler.Protected().Verify("RetargetFile", Times.Exactly(2), ItExpr.IsAny<string>(), ItExpr.IsAny<Version>());
        }

    }
}
