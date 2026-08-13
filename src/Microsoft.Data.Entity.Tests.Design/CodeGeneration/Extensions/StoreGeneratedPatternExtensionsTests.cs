// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Core.Metadata.Edm;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Extensions
{
    [TestClass]
    public class StoreGeneratedPatternExtensionsTests
    {
        [TestMethod]
        public void ToDatabaseGeneratedOption_converts_to_DatabaseGeneratedOption()
        {
            StoreGeneratedPattern.Computed.ToDatabaseGeneratedOption().Should().Be(DatabaseGeneratedOption.Computed);
            StoreGeneratedPattern.Identity.ToDatabaseGeneratedOption().Should().Be(DatabaseGeneratedOption.Identity);
            StoreGeneratedPattern.None.ToDatabaseGeneratedOption().Should().Be(DatabaseGeneratedOption.None);
        }

        [TestMethod]
        public void ToDatabaseGeneratedOption_returns_none_when_unknown()
        {
            ((StoreGeneratedPattern)42).ToDatabaseGeneratedOption().Should().Be(DatabaseGeneratedOption.None);
        }
    }
}
