// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    [TestClass]
    public class EntityFrameworkVersionTests
    {
        [TestMethod]
        public void GetAllVersions_returns_only_Version3_for_modern_development()
        {
            // For modern development, GetAllVersions only returns Version3
            List<Version> versions = EntityFrameworkVersion.GetAllVersions().ToList();
            versions.Should().HaveCount(1);
            versions.Should().Contain(EntityFrameworkVersion.Version3);
        }

        [TestMethod]
        public void IsValidVersion_returns_true_for_Version3()
        {
            // Only Version3 is valid
            EntityFrameworkVersion.IsValidVersion(new Version(3, 0, 0, 0)).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidVersion_returns_false_for_invalid_versions()
        {
            EntityFrameworkVersion.IsValidVersion(null).Should().BeFalse();
            EntityFrameworkVersion.IsValidVersion(new Version(0, 0, 0, 0)).Should().BeFalse();
            EntityFrameworkVersion.IsValidVersion(new Version(1, 0, 0, 0)).Should().BeFalse();
            EntityFrameworkVersion.IsValidVersion(new Version(2, 0, 0, 0)).Should().BeFalse();
            EntityFrameworkVersion.IsValidVersion(new Version(4, 0, 0, 0)).Should().BeFalse();
            EntityFrameworkVersion.IsValidVersion(new Version(3, 0)).Should().BeFalse();
        }

        [TestMethod]
        public void DoubleToVersion_returns_valid_version_for_known_double_versions()
        {
            EntityFrameworkVersion.DoubleToVersion(3.0).Should().Be(EntityFrameworkVersion.Version3);
        }

        [TestMethod]
        public void VersionToDouble_returns_valid_version_for_known_double_versions()
        {
            EntityFrameworkVersion.VersionToDouble(EntityFrameworkVersion.Version3).Should().Be(3.0);
        }

        [TestMethod]
        public void Latest_EF_version_is_really_latest()
        {
            EntityFrameworkVersion.Latest.Should().Be(
                EntityFrameworkVersion.GetAllVersions().OrderByDescending(v => v).First());
        }
    }
}
