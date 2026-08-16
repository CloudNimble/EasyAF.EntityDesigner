// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.Data.Entity.Design.Ide;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resources = Microsoft.VisualStudio.Data.Entity.Design.Resources;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class RuntimeVersionTests
    {
        [TestMethod]
        public void Latest_returns_version()
        {
            RuntimeVersion.Latest.Should().NotBeNull();
        }

        [TestMethod]
        public void Version5_always_returns_net45()
        {
            // Version5 always returns Version5Net45 (legacy .NET 4.0 support removed)
            RuntimeVersion.Version5(targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2)
                .Should().Be(RuntimeVersion.Version5Net45);
            RuntimeVersion.Version5(targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2)
                .Should().Be(RuntimeVersion.Version5Net45);
        }

        [TestMethod]
        public void GetName_returns_formatted_name()
        {
            Version entityFrameworkVersion = new Version(1, 2, 3, 4);

            var entityFrameworkVersionName = RuntimeVersion.GetName(entityFrameworkVersion, null);

            entityFrameworkVersionName.Should().Be(
                string.Format(
                    Resources.EntityFrameworkVersionName,
                    new Version(entityFrameworkVersion.Major, entityFrameworkVersion.Minor)));
        }

        [TestMethod]
        public void GetName_fixes_up_net45_ef5()
        {
            // Version5Net40 has been removed - only Version5Net45 is supported
            var entityFrameworkVersion = RuntimeVersion.Version5Net45;

            var entityFrameworkVersionName = RuntimeVersion.GetName(entityFrameworkVersion, null);

            entityFrameworkVersionName.Should().Be(
                string.Format(Resources.EntityFrameworkVersionName, new Version(5, 0)));
        }

        [TestMethod]
        public void GetName_returns_raw_version_for_pre_ef6()
        {
            // Legacy EF5/System.Data.Entity version fixup has been removed
            // Now we just return the version as-is for pre-EF6 versions
            var netFrameworkVersions =
                new[]
                    {
                        new Version(4, 7, 2), // .NET Framework 4.7.2
                        new Version(4, 8), // .NET Framework 4.8
                        new Version(42, 0, 0, 0) // a future version of .NET Framework
                    };

            Version entityFrameworkVersion = new Version(4, 0, 0, 0);

            foreach (var targetNetFrameworkVersion in netFrameworkVersions)
            {
                var entityFrameworkVersionName =
                    RuntimeVersion.GetName(entityFrameworkVersion, targetNetFrameworkVersion);

                entityFrameworkVersionName.Should().Be(
                    string.Format(Resources.EntityFrameworkVersionName, new Version(4, 0)));
            }
        }

        [TestMethod]
        public void RequiresLegacyProvider_always_returns_false()
        {
            // Legacy provider support removed - always returns false
            RuntimeVersion.RequiresLegacyProvider(new Version(4, 0, 0, 0)).Should().BeFalse();
            RuntimeVersion.RequiresLegacyProvider(RuntimeVersion.Version6).Should().BeFalse();
            RuntimeVersion.RequiresLegacyProvider(new Version(7, 0, 0, 0)).Should().BeFalse();
        }

        [TestMethod]
        public void GetTargetSchemaVersion_always_returns_Version3()
        {
            // GetTargetSchemaVersion always returns Version3 for modern development
            RuntimeVersion.GetTargetSchemaVersion(null, NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2)
                .Should().Be(EntityFrameworkVersion.Version3);
            RuntimeVersion.GetTargetSchemaVersion(null, new Version(4, 8))
                .Should().Be(EntityFrameworkVersion.Version3);
            RuntimeVersion.GetTargetSchemaVersion(RuntimeVersion.Version5Net45, NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2)
                .Should().Be(EntityFrameworkVersion.Version3);
            RuntimeVersion.GetTargetSchemaVersion(RuntimeVersion.Version6, NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2)
                .Should().Be(EntityFrameworkVersion.Version3);
        }

        [TestMethod]
        public void IsSchemaVersionLatestForAssemblyVersion_returns_true_only_for_Version3()
        {
            // For modern development, Version3 is always the latest schema version
            RuntimeVersion.IsSchemaVersionLatestForAssemblyVersion(
                EntityFrameworkVersion.Version3, new Version(6, 0, 0, 0), NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2).Should().BeTrue();
            RuntimeVersion.IsSchemaVersionLatestForAssemblyVersion(
                EntityFrameworkVersion.Version3, new Version(6, 0, 0, 0), new Version(4, 8)).Should().BeTrue();
        }

        [TestMethod]
        public void GetSchemaVersionForNetFrameworkVersion_always_returns_Version3()
        {
            // For modern development, always returns Version3
            RuntimeVersion.GetSchemaVersionForNetFrameworkVersion(new Version(4, 5, 1)).Should().Be(EntityFrameworkVersion.Version3);
            RuntimeVersion.GetSchemaVersionForNetFrameworkVersion(new Version(4, 5)).Should().Be(EntityFrameworkVersion.Version3);
            RuntimeVersion.GetSchemaVersionForNetFrameworkVersion(new Version(4, 0)).Should().Be(EntityFrameworkVersion.Version3);
            RuntimeVersion.GetSchemaVersionForNetFrameworkVersion(new Version(3, 5)).Should().Be(EntityFrameworkVersion.Version3);
        }
    }
}
