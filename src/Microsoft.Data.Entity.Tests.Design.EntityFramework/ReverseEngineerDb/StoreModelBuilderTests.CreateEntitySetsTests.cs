// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        private const string StoreTypeMetadataPropertyName =
            "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator:Type";

        [TestMethod]
        public void CreateEntitySets_creates_EntitySets_for_valid_non_readonly_table_entities()
        {
            var inputTableDetailsRows =
                new[]
                    {
                        CreateRow("catalog", "dbo", "Customer", "Id", 0, false, "int", isPrimaryKey: true),
                        CreateRow("catalog", "dbo", "OrderLine", "Id", 0, false, "int", isPrimaryKey: true),
                        CreateRow("catalog", "dbo", "Customer", "Name", 1, false, "nvarchar", isPrimaryKey: false),
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;
            List<EntitySet> entitySetsForReadOnlyEntities = new List<EntitySet>();

            CreateStoreModelBuilder()
                .CreateEntitySets(inputTableDetailsRows, entityRegister, entitySetsForReadOnlyEntities, DbObjectType.Table);

            entitySets.Count.Should().Be(2);
            entityTypes.Count.Should().Be(2);
            entitySetsForReadOnlyEntities.Should().BeEmpty();
            entitySets.Select(s => s.ElementType.Name).Should().BeEquivalentTo(entityTypes.Select(t => t.Name));

            entitySets
                .All(
                    s =>
                    s.MetadataProperties.Any(
                        p => p.Name == StoreTypeMetadataPropertyName && (string)p.Value == "Tables")).Should().BeTrue();
        }

        [TestMethod]
        public void CreateEntitySets_creates_EntitySets_for_valid_non_readonly_view_entities()
        {
            var inputTableDetailsRows =
                new[]
                    {
                        CreateRow("catalog", "dbo", "Customer", "Id", 0, false, "int", isPrimaryKey: true),
                        CreateRow("catalog", "dbo", "OrderLine", "Id", 0, false, "int", isPrimaryKey: true),
                        CreateRow("catalog", "dbo", "Customer", "Name", 1, false, "nvarchar", isPrimaryKey: false),
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;
            List<EntitySet> entitySetsForReadOnlyEntities = new List<EntitySet>();

            CreateStoreModelBuilder()
                .CreateEntitySets(inputTableDetailsRows, entityRegister, entitySetsForReadOnlyEntities, DbObjectType.View);

            entitySets.Count.Should().Be(2);
            entityTypes.Count.Should().Be(2);
            entitySetsForReadOnlyEntities.Should().BeEmpty();
            entitySets.Select(s => s.ElementType.Name).Should().BeEquivalentTo(entityTypes.Select(t => t.Name));

            entitySets
                .All(
                    s =>
                    s.MetadataProperties.Any(
                        p => p.Name == StoreTypeMetadataPropertyName && (string)p.Value == "Views")).Should().BeTrue();
        }

        [TestMethod]
        public void CreateEntitySets_creates_EntitySets_with_schema_if_schema_defined()
        {
            var inputTableDetailsRows =
                new[]
                    {
                        CreateRow("catalog", "dbo", "Customer", "Id", 0, false, "int", isPrimaryKey: true),
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;
            List<EntitySet> entitySetsForReadOnlyEntities = new List<EntitySet>();

            CreateStoreModelBuilder()
                .CreateEntitySets(inputTableDetailsRows, entityRegister, entitySetsForReadOnlyEntities, DbObjectType.Table);

            entitySets.Count.Should().Be(1);
            entitySets[0].Schema.Should().Be("dbo");
            entitySetsForReadOnlyEntities.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateEntitySets_creates_EntitySets_without_schema_if_schema_not_defined()
        {
            var inputTableDetailsRows =
                new[]
                    {
                        CreateRow("catalog", null, "Customer", "Id", 0, false, "int", isPrimaryKey: true),
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;
            List<EntitySet> entitySetsForReadOnlyEntities = new List<EntitySet>();

            CreateStoreModelBuilder()
                .CreateEntitySets(inputTableDetailsRows, entityRegister, entitySetsForReadOnlyEntities, DbObjectType.Table);

            entitySets.Count.Should().Be(1);
            entitySets[0].Schema.Should().BeNull();
            entitySetsForReadOnlyEntities.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateEntitySets_uses_table_when_entity_type_name_different_than_table_name()
        {
            const string tableName = "Customer.Details";
            const string entityTypeName = "Customer_Details";

            var inputTableDetailsRows =
                new[]
                    {
                        CreateRow("catalog", "dbo", tableName, "Id", 0, false, "int", isPrimaryKey: true),
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;
            List<EntitySet> entitySetsForReadOnlyEntities = new List<EntitySet>();

            CreateStoreModelBuilder()
                .CreateEntitySets(inputTableDetailsRows, entityRegister, entitySetsForReadOnlyEntities, DbObjectType.Table);

            var entitySet = entitySets[0];
            entitySet.Table.Should().Be(tableName);
            entitySet.Name.Should().Be(entityTypeName);
        }

        [TestMethod]
        public void CreateEntitySets_does_not_create_EntitySet_for_invalid_EntityType()
        {
            var inputTableDetailsRows =
                new[]
                    {
                        CreateRow("catalog", null, "Customer", "Id", 0, /*isNullable*/ true, "geography", isPrimaryKey: true),
                        CreateRow("catalog", null, "Customer", "location", 0, false, "geometry", isPrimaryKey: true)
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;
            List<EntitySet> entitySetsForReadOnlyEntities = new List<EntitySet>();

            CreateStoreModelBuilder()
                .CreateEntitySets(inputTableDetailsRows, entityRegister, entitySetsForReadOnlyEntities, DbObjectType.Table);

            entitySets.Should().BeEmpty();
            entityTypes.Count.Should().Be(1);
            entitySetsForReadOnlyEntities.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateEntitySets_creates_EntitySets_for_tables_and_views()
        {
            var tableDetailsRowsForTables =
                new[]
                    {
                        CreateRow("catalog", "dbo", "Customer", "Id", 0, false, "int", isPrimaryKey: true),
                    };

            var tableDetailsRowsForViews =
                new[]
                    {
                        CreateRow("catalog", "dbo", "EvenBetterCustomer", "Id", 0, false, "int", isPrimaryKey: true),
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;

            CreateStoreModelBuilder()
                .CreateEntitySets(tableDetailsRowsForTables, tableDetailsRowsForViews, entityRegister);

            entitySets.Count.Should().Be(2);
            entityTypes.Count.Should().Be(2);
            entitySets.All(s => s.DefiningQuery == null).Should().BeTrue();

            entitySets[0].MetadataProperties.Any(
                p => p.Name == StoreTypeMetadataPropertyName && (string)p.Value == "Tables").Should().BeTrue();

            entitySets[1].MetadataProperties.Any(
                p => p.Name == StoreTypeMetadataPropertyName && (string)p.Value == "Views").Should().BeTrue();
        }

        [TestMethod]
        public void CreateEntitySets_creates_EntitySets_with_defining_queries_for_tables_and_views()
        {
            var tableDetailsRowsForTables =
                new[]
                    {
                        CreateRow("catalog", "dbo", "Customer", "Id", 0, true, "int", isPrimaryKey: false),
                        CreateRow("catalog", "dbo", "Customer", "SSN", 0, false, "nvarchar", isPrimaryKey: false),
                    };

            var tableDetailsRowsForViews =
                new[]
                    {
                        CreateRow("catalog", "dbo", "EvenBetterCustomer", "Id", 0, true, "int", isPrimaryKey: false),
                        CreateRow("catalog", "dbo", "EvenBetterCustomer", "SSN", 0, false, "nvarchar", isPrimaryKey: false),
                    };

            StoreModelBuilder.EntityRegister entityRegister = new StoreModelBuilder.EntityRegister();
            var entitySets = entityRegister.EntitySets;
            var entityTypes = entityRegister.EntityTypes;

            CreateStoreModelBuilder()
                .CreateEntitySets(tableDetailsRowsForTables, tableDetailsRowsForViews, entityRegister);

            entitySets.Count.Should().Be(2);
            entitySets.All(s => s.DefiningQuery != null).Should().BeTrue();
            entityTypes.Count.Should().Be(2);

            entitySets[0].MetadataProperties.Any(
                p => p.Name == StoreTypeMetadataPropertyName && (string)p.Value == "Tables").Should().BeTrue();

            entitySets[1].MetadataProperties.Any(
                p => p.Name == StoreTypeMetadataPropertyName && (string)p.Value == "Views").Should().BeTrue();
        }
    }
}
