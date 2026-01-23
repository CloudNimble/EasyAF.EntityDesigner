// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard
{
    using Microsoft.Data.Entity.Tests.Design.TestHelpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class DbContextGeneratorTests
    {
        [TestMethod]
        public void TemplateSupported_returns_true_when_targeting_NetFramework4_or_newer()
        {
            var targets =
                new[]
                    {
                        ".NETFramework,Version=v4.0",
                        ".NETFramework,Version=v4.5",
                        ".NETFramework,Version=v4.5.1"
                    };

            foreach (var target in targets)
            {
                var mockMonikerHelper = new MockDTE(target);

                (DbContextCodeGenerator.TemplateSupported(
                        mockMonikerHelper.Project,
                        mockMonikerHelper.ServiceProvider));
            }
        }

        [TestMethod]
        public void TemplateSupported_returns_true_when_targeting_modern_DotNet()
        {
            var targets =
                new[]
                    {
                        ".NET,Version=v5.0",
                        ".NET,Version=v6.0",
                        ".NET,Version=v7.0",
                        ".NET,Version=v8.0",
                        ".NET,Version=v9.0",
                        ".NET,Version=v10.0",
                        ".NETCoreApp,Version=v3.1"
                    };

            foreach (var target in targets)
            {
                var mockMonikerHelper = new MockDTE(target);

                (DbContextCodeGenerator.TemplateSupported(
                        mockMonikerHelper.Project,
                        mockMonikerHelper.ServiceProvider));
            }
        }

        [TestMethod]
        public void TemplateSupported_returns_false_for_NetFramework3_5()
        {
            var mockMonikerHelper =
                new MockDTE(".NETFramework,Version=v3.5");

            Assert.False(
                DbContextCodeGenerator.TemplateSupported(
                    mockMonikerHelper.Project,
                    mockMonikerHelper.ServiceProvider));
        }

        [TestMethod]
        public void TemplateSupported_returns_false_when_for_non_NetFramework_projects()
        {
            var targets =
                new[]
                    {
                        ".NETFramework,Version=v1.1",
                        "XBox,Version=v4.0",
                        "abc",
                        string.Empty,
                        null
                    };

            foreach (var target in targets)
            {
                var mockMonikerHelper = new MockDTE(target);

                Assert.False(
                    DbContextCodeGenerator.TemplateSupported(
                        mockMonikerHelper.Project,
                        mockMonikerHelper.ServiceProvider));
            }
        }

        [TestMethod]
        public void TemplateSupported_returns_false_for_Misc_project()
        {
            const string vsMiscFilesProjectUniqueName = "<MiscFiles>";

            var mockMonikerHelper =
                new MockDTE(".NETFramework,Version=v4.0", vsMiscFilesProjectUniqueName);

            Assert.False(
                DbContextCodeGenerator.TemplateSupported(
                    mockMonikerHelper.Project,
                    mockMonikerHelper.ServiceProvider));
        }
    }
}
