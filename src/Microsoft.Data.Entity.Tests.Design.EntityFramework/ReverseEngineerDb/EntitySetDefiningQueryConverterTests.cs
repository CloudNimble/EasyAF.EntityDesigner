// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.DependencyResolution;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using System.Linq;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    [TestClass]
    public class EntitySetDefiningQueryConverterTests
    {
        private static DbProviderManifest ProviderManifest =>
            Utils.SqlProviderServicesInstance.GetProviderManifest("2008");

        private const string StoreSchemaAttributeNamespace = "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator";

        [TestMethod]
        public void CreateTransientMetadataWorkspace_creates_workspace_with_provided_store_entity_sets()
        {
            EdmProperty property =
                EdmProperty.CreatePrimitive(
                    "Id",
                    ProviderManifest.GetStoreTypes().Single(t => t.PrimitiveTypeKind == PrimitiveTypeKind.Int32));
            property.Nullable = false;

            EntityType entityType =
                EntityType.Create("EntityType", "MyModel", DataSpace.SSpace, new[] { "Id" }, new[] { property }, null);

            EntitySet entitySet = EntitySet.Create("EntityTypeSet", "dbo", "EntityTypes", null, entityType, null);

            var workspace =
                EntitySetDefiningQueryConverter.CreateTransientMetadataWorkspace(
                    [entitySet],
                    EntityFrameworkVersion.Version3,
                    "System.Data.SqlClient", "2008", ProviderManifest);

            workspace.Should().NotBeNull();
            StoreItemCollection storeItemCollection = (StoreItemCollection)workspace.GetItemCollection(DataSpace.SSpace);
            storeItemCollection.Should().NotBeNull();
            storeItemCollection.GetEntityContainer("StoreModelContainer").EntitySets.Count.Should().Be(1);
            storeItemCollection.GetEntityContainer("StoreModelContainer").EntitySets.Single().Name.Should().Be("EntityTypeSet");
            storeItemCollection.GetItems<EntityType>().Count.Should().Be(1);
            storeItemCollection.GetItems<EntityType>().Single().Name.Should().Be("EntityType");
            workspace.GetItemCollection(DataSpace.CSpace).Should().NotBeNull();
            workspace.GetItemCollection(DataSpace.CSSpace).Should().NotBeNull();
        }

        [TestMethod]
        public void CreateDefiningQuery_creates_query_for_entity_set()
        {
            EdmProperty property =
                EdmProperty.CreatePrimitive(
                    "Id",
                    ProviderManifest.GetStoreTypes().Single(t => t.PrimitiveTypeKind == PrimitiveTypeKind.Int32));
            property.Nullable = false;

            EntityType entityType =
                EntityType.Create("EntityType", "MyModel", DataSpace.SSpace, new[] { "Id" }, new[] { property }, null);

            EntitySet entitySet = EntitySet.Create("EntityTypeSet", "dbo", "EntityTypes", null, entityType, null);

            var workspace =
                EntitySetDefiningQueryConverter.CreateTransientMetadataWorkspace(
                    [entitySet],
                    EntityFrameworkVersion.Version3,
                    "System.Data.SqlClient", "2008", ProviderManifest);

            var definingQuery = EntitySetDefiningQueryConverter.CreateDefiningQuery(entitySet, workspace, Utils.SqlProviderServicesInstance);
            definingQuery.Should().NotBeNull();
        }

        [TestMethod]
        public void CloneWithDefiningQuery_creates_new_equivalent_entity_set_but_with_defining_query()
        {
            EdmProperty property =
                EdmProperty.CreatePrimitive(
                    "Id",
                    ProviderManifest.GetStoreTypes().Single(t => t.PrimitiveTypeKind == PrimitiveTypeKind.Int32));

            MetadataProperty customMetadataProperty =
                MetadataProperty.Create(
                    "http://tempUri:myProperty",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                    "value");

            EntityType entityType =
                EntityType.Create("EntityType", "MyModel", DataSpace.SSpace, new[] { "Id" }, new[] { property }, null);

            EntitySet entitySet = EntitySet.Create("EntityTypeSet", "dbo", "EntityTypes", null, entityType, new[] { customMetadataProperty });

            var clonedEntitySet = EntitySetDefiningQueryConverter.CloneWithDefiningQuery(entitySet, "definingQuery");

            clonedEntitySet.Should().NotBeNull();
            entitySet.Should().NotBeSameAs(clonedEntitySet);
            clonedEntitySet.Name.Should().BeSameAs(entitySet.Name);
            clonedEntitySet.ElementType.Should().BeSameAs(entitySet.ElementType);
            clonedEntitySet.Schema.Should().BeNull();
            clonedEntitySet.Table.Should().BeNull();
            clonedEntitySet.MetadataProperties.Single(p => p.Name == StoreSchemaAttributeNamespace + ":Schema")
                .Value.Should().Be(entitySet.Schema);
            clonedEntitySet.MetadataProperties.Single(p => p.Name == StoreSchemaAttributeNamespace + ":Name")
                .Value.Should().Be(entitySet.Table);

            clonedEntitySet.MetadataProperties.Single(p => p.Name == "http://tempUri:myProperty").Value
                .Should().Be(entitySet.MetadataProperties.Single(p => p.Name == "http://tempUri:myProperty").Value);
        }

        [TestMethod]
        public void CloneWithDefiningQuery_does_not_creat_schema_and_table_extended_attributes_if_they_are_null()
        {
            EdmProperty property =
                EdmProperty.CreatePrimitive(
                    "Id",
                    ProviderManifest.GetStoreTypes().Single(t => t.PrimitiveTypeKind == PrimitiveTypeKind.Int32));

            MetadataProperty customMetadataProperty =
                MetadataProperty.Create(
                    "http://tempUri:myProperty",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                    "value");

            EntityType entityType =
                EntityType.Create("EntityType", "MyModel", DataSpace.SSpace, new[] { "Id" }, new[] { property }, null);

            EntitySet entitySet = EntitySet.Create("EntityTypeSet", null, null, null, entityType, new[] { customMetadataProperty });

            var clonedEntitySet = EntitySetDefiningQueryConverter.CloneWithDefiningQuery(entitySet, "definingQuery");

            clonedEntitySet.Schema.Should().BeNull();
            clonedEntitySet.Table.Should().BeNull();
            entitySet.MetadataProperties.Any(p => p.Name.EndsWith(StoreSchemaAttributeNamespace + ":Schema")).Should().BeFalse();
            entitySet.MetadataProperties.Any(p => p.Name.EndsWith(StoreSchemaAttributeNamespace + ":Name")).Should().BeFalse();
        }

        [TestMethod]
        public void Convert_can_convert_entitysets_without_defining_queries_to_entitysets_with_defining_queries()
        {
            EdmProperty property1 =
                EdmProperty.CreatePrimitive(
                    "Id",
                    ProviderManifest.GetStoreTypes().Single(t => t.PrimitiveTypeKind == PrimitiveTypeKind.Int32));
            property1.Nullable = false;

            EntityType entityType1 =
                EntityType.Create("EntityType1", "MyModel", DataSpace.SSpace, new[] { "Id" }, new[] { property1 }, null);

            EdmProperty property2 =
                EdmProperty.CreatePrimitive(
                    "Id",
                    ProviderManifest.GetStoreTypes().Single(t => t.PrimitiveTypeKind == PrimitiveTypeKind.Int32));
            property2.Nullable = false;

            EntityType entityType2 =
                EntityType.Create("EntityType2", "MyModel", DataSpace.SSpace, new[] { "Id" }, new[] { property2 }, null);

            List<EntitySet> entitySets =
                new List<EntitySet>
                    {
                        EntitySet.Create("EntityType1Set", "dbo", "EntityTypes1", null, entityType1, null),
                        EntitySet.Create("EntityType2Set", "dbo", "EntityTypes2", null, entityType2, null)
                    };

            Mock<IDbDependencyResolver> mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver.Setup(
                r => r.GetService(
                    It.Is<Type>(t => t == typeof(DbProviderServices)),
                    It.IsAny<string>())).Returns(Utils.SqlProviderServicesInstance);

            List<EntitySet> convertedEntitySets =
                EntitySetDefiningQueryConverter.Convert(
                    entitySets,
                    EntityFrameworkVersion.Version3,
                    "System.Data.SqlClient",
                    "2008",
                    mockResolver.Object).ToList();

            convertedEntitySets.Should().NotBeNull();
            convertedEntitySets.Select(e => e.Name).Should().Equal(entitySets.Select(e => e.Name));
            convertedEntitySets.All(e => e.DefiningQuery != null).Should().BeTrue();
        }
    }
}
