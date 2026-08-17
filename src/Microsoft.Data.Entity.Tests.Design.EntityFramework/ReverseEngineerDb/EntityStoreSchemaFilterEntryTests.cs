// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    [TestClass]
    public class EntityStoreSchemaFilterEntryTests
    {
        [TestMethod]
        public void EntityStoreSchemaFilterEntry_ctor_sets_fields()
        {
            EntityStoreSchemaFilterEntry filterEntry =
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
            EntityStoreSchemaFilterEntry filterEntry =
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
            Action act = () => new EntityStoreSchemaFilterEntry(
                null,
                null,
                null,
                EntityStoreSchemaFilterObjectTypes.None,
                EntityStoreSchemaFilterEffect.Exclude);

            var exception = act.Should().Throw<ArgumentException>().Which;
            exception.Message.Should().Be(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.InvalidStringArgument,
                    "types"));
        }
    }
}
