// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.Metadata
{
    [TestClass]
    public class CsdlVersionTests
    {
        [TestMethod]
        public void GetAllVersions_returns_only_Version3_for_modern_development()
        {
            // For modern development, GetAllVersions only returns Version3
            List<Version> versions = CsdlVersion.GetAllVersions().ToList();
            versions.Should().HaveCount(1);
            versions.Should().Contain(CsdlVersion.Version3);
        }

        [TestMethod]
        public void IsValidVersion_returns_true_for_valid_versions()
        {
            // IsValidVersion still validates all known versions for backward compatibility
            CsdlVersion.IsValidVersion(new Version(1, 0, 0, 0)).Should().BeTrue();
            CsdlVersion.IsValidVersion(new Version(1, 1, 0, 0)).Should().BeTrue();
            CsdlVersion.IsValidVersion(new Version(2, 0, 0, 0)).Should().BeTrue();
            CsdlVersion.IsValidVersion(new Version(3, 0, 0, 0)).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidVersion_returns_false_for_invalid_versions()
        {
            CsdlVersion.IsValidVersion(null).Should().BeFalse();
            CsdlVersion.IsValidVersion(new Version(0, 0, 0, 0)).Should().BeFalse();
            CsdlVersion.IsValidVersion(new Version(4, 0, 0, 0)).Should().BeFalse();
            CsdlVersion.IsValidVersion(new Version(3, 0)).Should().BeFalse();
            CsdlVersion.IsValidVersion(new Version(2, 0, 0)).Should().BeFalse();
        }

        [TestMethod]
        public void GetAllVersions_and_EntityFrameworkVersions_return_same_count()
        {
            // Both should return only Version3 for new model creation
            EntityFrameworkVersion.GetAllVersions().Count().Should().Be(CsdlVersion.GetAllVersions().Count());
        }
    }
}
