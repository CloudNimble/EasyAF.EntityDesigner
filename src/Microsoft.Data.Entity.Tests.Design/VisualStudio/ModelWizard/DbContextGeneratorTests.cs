// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard
{
    [TestClass]
    public class DbContextGeneratorTests
    {
        [TestMethod]
        public void TemplateSupported_returns_true_when_targeting_NetFramework4_7_2_or_newer()
        {
            // Only .NET Framework 4.7.2+ is supported
            var targets =
                new[]
                    {
                        ".NETFramework,Version=v4.7.2",
                        ".NETFramework,Version=v4.8"
                    };

            foreach (var target in targets)
            {
                MockDTE mockMonikerHelper = new MockDTE(target);

                DbContextCodeGenerator.TemplateSupported(
                    mockMonikerHelper.Project,
                    mockMonikerHelper.ServiceProvider).Should().BeTrue();
            }
        }

        [TestMethod]
        public void TemplateSupported_returns_false_when_targeting_NetFramework_below_4_7_2()
        {
            // .NET Framework below 4.7.2 is no longer supported
            var targets =
                new[]
                    {
                        ".NETFramework,Version=v4.0",
                        ".NETFramework,Version=v4.5",
                        ".NETFramework,Version=v4.7.1"
                    };

            foreach (var target in targets)
            {
                MockDTE mockMonikerHelper = new MockDTE(target);

                DbContextCodeGenerator.TemplateSupported(
                    mockMonikerHelper.Project,
                    mockMonikerHelper.ServiceProvider).Should().BeFalse();
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
                MockDTE mockMonikerHelper = new MockDTE(target);

                DbContextCodeGenerator.TemplateSupported(
                    mockMonikerHelper.Project,
                    mockMonikerHelper.ServiceProvider).Should().BeTrue();
            }
        }

        [TestMethod]
        public void TemplateSupported_returns_false_for_NetFramework3_5()
        {
            MockDTE mockMonikerHelper =
                new MockDTE(".NETFramework,Version=v3.5");

            DbContextCodeGenerator.TemplateSupported(
                mockMonikerHelper.Project,
                mockMonikerHelper.ServiceProvider).Should().BeFalse();
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
                MockDTE mockMonikerHelper = new MockDTE(target);

                DbContextCodeGenerator.TemplateSupported(
                    mockMonikerHelper.Project,
                    mockMonikerHelper.ServiceProvider).Should().BeFalse();
            }
        }

        [TestMethod]
        public void TemplateSupported_returns_false_for_Misc_project()
        {
            const string vsMiscFilesProjectUniqueName = "<MiscFiles>";

            MockDTE mockMonikerHelper =
                new MockDTE(".NETFramework,Version=v4.0", vsMiscFilesProjectUniqueName);

            DbContextCodeGenerator.TemplateSupported(
                mockMonikerHelper.Project,
                mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }
    }
}
