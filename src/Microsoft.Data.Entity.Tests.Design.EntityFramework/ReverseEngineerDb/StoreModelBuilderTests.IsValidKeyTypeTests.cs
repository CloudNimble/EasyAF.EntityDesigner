// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        [TestMethod]
        public void IsValidKeyType_returns_true_for_valid_key_type()
        {
            StoreModelBuilder.IsValidKeyType(
                EntityFrameworkVersion.Version3,
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32)).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidKeyType_returns_false_for_non_primitive_type()
        {
            EntityType entityType = EntityType.Create("dummy", "namespace", DataSpace.SSpace, null, new EdmMember[0], null);
            StoreModelBuilder.IsValidKeyType(EntityFrameworkVersion.Version3, entityType).Should().BeFalse();
        }

        [TestMethod]
        public void IsValidKeyType_returns_true_for_Binary_type()
        {
            // Binary type is valid as key type in Version3
            StoreModelBuilder.IsValidKeyType(
                EntityFrameworkVersion.Version3,
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Binary)).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidKeyType_returns_false_for_Geometry_type()
        {
            StoreModelBuilder.IsValidKeyType(
                EntityFrameworkVersion.Version3,
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Geometry)).Should().BeFalse();
        }

        [TestMethod]
        public void IsValidKeyType_returns_false_for_Geography_type()
        {
            StoreModelBuilder.IsValidKeyType(
                EntityFrameworkVersion.Version3,
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Geography)).Should().BeFalse();
        }
    }
}
