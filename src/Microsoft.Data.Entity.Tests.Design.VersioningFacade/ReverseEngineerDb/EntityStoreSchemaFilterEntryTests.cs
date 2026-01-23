// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    using System;
    using System.Globalization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class EntityStoreSchemaFilterEntryTests
    {
        [TestMethod]
        public void EntityStoreSchemaFilterEntry_ctor_sets_fields()
        {
            var filterEntry =
                new EntityStoreSchemaFilterEntry(
                    "catalog",
                    "schema",
                    "name",
                    EntityStoreSchemaFilterObjectTypes.Table,
                    EntityStoreSchemaFilterEffect.Exclude);

            filterEntry.Catalog.Should().Be("catalog");
            filterEntry.Schema.Should().Be("schema");
            filterEntry.Name.Should().Be("name");
            filterEntry.Types.Should().Be(EntityStoreSchemaFilterObjectTypes.Table);
            filterEntry.Effect.Should().Be(EntityStoreSchemaFilterEffect.Exclude);
        }

        [TestMethod]
        public void EntityStoreSchemaFilterEntry_ctor_sets_types_All_and_effect_Allows_by_default()
        {
            var filterEntry =
                new EntityStoreSchemaFilterEntry("catalog", "schema", "name");

            filterEntry.Catalog.Should().Be("catalog");
            filterEntry.Schema.Should().Be("schema");
            filterEntry.Name.Should().Be("name");
            filterEntry.Types.Should().Be(EntityStoreSchemaFilterObjectTypes.All);
            filterEntry.Effect.Should().Be(EntityStoreSchemaFilterEffect.Allow);
        }

        [TestMethod]
        public void EntityStoreSchemaFilterEntry_ctor_throws_for_Types_None()
        {
            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.InvalidStringArgument,
                    "types"),
                Assert.Throws<ArgumentException>(
                    () => new EntityStoreSchemaFilterEntry(
                              null,
                              null,
                              null,
                              EntityStoreSchemaFilterObjectTypes.None,
                              EntityStoreSchemaFilterEffect.Exclude)).Message);
        }
    }
}
