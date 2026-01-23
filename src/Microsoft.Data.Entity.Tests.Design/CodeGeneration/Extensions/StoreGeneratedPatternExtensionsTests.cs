// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Extensions
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Core.Metadata.Edm;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class StoreGeneratedPatternExtensionsTests
    {
        [TestMethod]
        public void ToDatabaseGeneratedOption_converts_to_DatabaseGeneratedOption()
        {
            Assert.Equal(DatabaseGeneratedOption.Computed, StoreGeneratedPattern.Computed.ToDatabaseGeneratedOption());
            Assert.Equal(DatabaseGeneratedOption.Identity, StoreGeneratedPattern.Identity.ToDatabaseGeneratedOption());
            Assert.Equal(DatabaseGeneratedOption.None, StoreGeneratedPattern.None.ToDatabaseGeneratedOption());
        }

        [TestMethod]
        public void ToDatabaseGeneratedOption_returns_none_when_unknown()
        {
            Assert.Equal(DatabaseGeneratedOption.None, ((StoreGeneratedPattern)42).ToDatabaseGeneratedOption());
        }
    }
}
