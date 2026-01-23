// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.Metadata
{
    using System;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class CsdlVersionTests
    {
        [TestMethod]
        public void GetAllVersions_returns_all_declared_versions()
        {
            var declaredVersions =
                typeof(CsdlVersion)
                    .GetFields()
                    .Where(f => f.FieldType == typeof(Version))
                    .Select(f => f.GetValue(null))
                    .OrderByDescending(v => v);

            (declaredVersions.SequenceEqual(CsdlVersion.GetAllVersions().OrderByDescending(v => v)));
        }

        [TestMethod]
        public void IsValidVersion_returns_true_for_valid_versions()
        {
            CsdlVersion.IsValidVersion(new Version(1, 0, 0, 0.Should().BeTrue()));
            CsdlVersion.IsValidVersion(new Version(1, 1, 0, 0.Should().BeTrue()));
            CsdlVersion.IsValidVersion(new Version(2, 0, 0, 0.Should().BeTrue()));
            CsdlVersion.IsValidVersion(new Version(3, 0, 0, 0.Should().BeTrue()));
        }

        [TestMethod]
        public void IsValidVersion_returns_false_for_invalid_versions()
        {
            CsdlVersion.IsValidVersion(null.Should().BeFalse());
            CsdlVersion.IsValidVersion(new Version(0, 0, 0, 0.Should().BeFalse()));
            CsdlVersion.IsValidVersion(new Version(4, 0, 0, 0.Should().BeFalse()));
            CsdlVersion.IsValidVersion(new Version(3, 0.Should().BeFalse()));
            CsdlVersion.IsValidVersion(new Version(2, 0, 0.Should().BeFalse()));
        }

        [TestMethod]
        public void CsdlVersion_was_added_if_EntityFramework_version_was_added()
        {
            // +1 to account for CSDL version 1.1
            Assert.Equal(
                EntityFrameworkVersion.GetAllVersions().Count() + 1,
                CsdlVersion.GetAllVersions().Count());
        }
    }
}
