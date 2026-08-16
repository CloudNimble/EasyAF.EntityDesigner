// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.Data.Entity.Design.Ide;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class NetFrameworkVersioningHelperTests
    {
        [TestMethod]
        public void TargetNetFrameworkVersion_returns_targeted_version_from_valid_net_framework_moniker()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v4.7.2");

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().Be(
                NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2);
        }

        [TestMethod]
        public void TargetNetFrameworkVersion_returns_null_for_null_target_net_framework_moniker()
        {
            MockDTE mockMonikerHelper = new MockDTE(null);

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        [TestMethod]
        public void TargetNetFrameworkVersion_returns_null_for_empty_target_net_framework_moniker()
        {
            MockDTE mockMonikerHelper = new MockDTE(string.Empty);

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        [TestMethod]
        public void TargetNetFrameworkVersion_returns_null_for_non_string_target_net_framework_moniker()
        {
            MockDTE mockMonikerHelper = new MockDTE(new object());

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        [TestMethod]
        public void TargetNetFrameworkVersion_returns_null_for_invalid_target_framework_moniker()
        {
            MockDTE mockMonikerHelper = new MockDTE("abc");

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        [TestMethod]
        public void TargetNetFrameworkVersion_returns_null_for_misc_project()
        {
            const string vsMiscFilesProjectUniqueName = "<MiscFiles>";

            MockDTE mockMonikerHelper = new MockDTE("abc", vsMiscFilesProjectUniqueName);

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        [TestMethod]
        public void TargetNetFrameworkVersion_returns_null_for_non_NetFramework_project()
        {
            MockDTE mockMonikerHelper = new MockDTE("Xbox,Version=v4.0");

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        // Tests for modern .NET support (Phase 1)

        [TestMethod]
        public void TargetNetFrameworkVersion_returns_null_for_modern_dotnet_project()
        {
            // .NET 8 project should return null from TargetNetFrameworkVersion
            MockDTE mockMonikerHelper = new MockDTE(".NET,Version=v8.0");

            NetFrameworkVersioningHelper.TargetNetFrameworkVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        [TestMethod]
        public void IsModernDotNetProject_returns_true_for_net8()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NET,Version=v8.0");

            NetFrameworkVersioningHelper.IsModernDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsModernDotNetProject_returns_true_for_net9()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NET,Version=v9.0");

            NetFrameworkVersioningHelper.IsModernDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsModernDotNetProject_returns_true_for_net10()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NET,Version=v10.0");

            NetFrameworkVersioningHelper.IsModernDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsModernDotNetProject_returns_true_for_netcoreapp31()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NETCoreApp,Version=v3.1");

            NetFrameworkVersioningHelper.IsModernDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsModernDotNetProject_returns_false_for_net_framework()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v4.8");

            NetFrameworkVersioningHelper.IsModernDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void IsModernDotNetProject_returns_false_for_null_moniker()
        {
            MockDTE mockMonikerHelper = new MockDTE(null);

            NetFrameworkVersioningHelper.IsModernDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void TargetRuntimeVersion_returns_version_for_net8()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NET,Version=v8.0");

            var version = NetFrameworkVersioningHelper.TargetRuntimeVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider);

            version.Should().NotBeNull();
            version.Major.Should().Be(8);
            version.Minor.Should().Be(0);
        }

        [TestMethod]
        public void TargetRuntimeVersion_returns_version_for_net_framework()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v4.8");

            var version = NetFrameworkVersioningHelper.TargetRuntimeVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider);

            version.Should().NotBeNull();
            version.Major.Should().Be(4);
            version.Minor.Should().Be(8);
        }

        [TestMethod]
        public void TargetRuntimeVersion_returns_null_for_unsupported_framework()
        {
            MockDTE mockMonikerHelper = new MockDTE("Xbox,Version=v4.0");

            NetFrameworkVersioningHelper.TargetRuntimeVersion(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeNull();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_true_for_net8()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NET,Version=v8.0");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_true_for_net10()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NET,Version=v10.0");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_true_for_net_framework_48()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v4.8");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_false_for_net_framework_35()
        {
            // .NET Framework 3.5 is no longer supported (requires 4.7.2+)
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v3.5");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_false_for_net_framework_45()
        {
            // .NET Framework 4.5 is no longer supported (requires 4.7.2+)
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v4.5");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_false_for_net_framework_471()
        {
            // .NET Framework 4.7.1 is no longer supported (requires 4.7.2+)
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v4.7.1");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_true_for_net_framework_472()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v4.7.2");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_false_for_net_framework_20()
        {
            MockDTE mockMonikerHelper = new MockDTE(".NETFramework,Version=v2.0");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void IsSupportedDotNetProject_returns_false_for_unsupported_framework()
        {
            MockDTE mockMonikerHelper = new MockDTE("Xbox,Version=v4.0");

            NetFrameworkVersioningHelper.IsSupportedDotNetProject(
                mockMonikerHelper.Project, mockMonikerHelper.ServiceProvider).Should().BeFalse();
        }
    }
}
