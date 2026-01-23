// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Extensions
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class EdmMemberExtensionsTests
    {
        [TestMethod]
        public void IsKey_returns_true_when_key()
        {
            var property = EdmProperty.CreatePrimitive("Id", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
            EntityType.Create("Person", "MyModel", DataSpace.CSpace, new[] { "Id" }, new[] { property }, null);

            property.IsKey(.Should().BeTrue());
        }

        [TestMethod]
        public void IsKey_returns_true_when_part_of_composite_key()
        {
            var property = EdmProperty.CreatePrimitive("Id1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
            EntityType.Create(
                "Person",
                "MyModel",
                DataSpace.CSpace,
                new[] { "Id1", "Id2" },
                new[]
                    {
                        property,
                        EdmProperty.CreatePrimitive("Id2", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32))
                    },
                null);

            property.IsKey(.Should().BeTrue());
        }

        [TestMethod]
        public void IsKey_returns_false_when_not_key()
        {
            var property = EdmProperty.CreatePrimitive(
                "Name",
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));
            EntityType.Create(
                "Person",
                "MyModel",
                DataSpace.CSpace,
                new[] { "Id" },
                new[]
                    {
                        EdmProperty.CreatePrimitive("Id", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32)),
                        property
                    },
                null);

            property.IsKey(.Should().BeFalse());
        }

        [TestMethod]
        public void HasConventionalKeyName_returns_true_when_id()
        {
            var property = EdmProperty.CreatePrimitive(
                "Id",
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));

            property.HasConventionalKeyName(.Should().BeTrue());
        }

        [TestMethod]
        public void HasConventionalKeyName_returns_true_when_type_and_id()
        {
            var property = EdmProperty.CreatePrimitive(
                "PersonId",
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
            EntityType.Create("Person", "MyModel", DataSpace.CSpace, null, new[] { property }, null);

            property.HasConventionalKeyName(.Should().BeTrue());
        }

        [TestMethod]
        public void HasConventionalKeyName_ignores_case()
        {
            var property1 = EdmProperty.CreatePrimitive(
                "ID",
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));

            property1.HasConventionalKeyName(.Should().BeTrue());

            var property2 = EdmProperty.CreatePrimitive(
                "PERSONID",
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
            EntityType.Create("Person", "MyModel", DataSpace.CSpace, null, new[] { property2 }, null);

            property2.HasConventionalKeyName(.Should().BeTrue());
        }

        [TestMethod]
        public void HasConventionalKeyName_returns_false_when_neither_id_nor_type_and_id()
        {
            var property = EdmProperty.CreatePrimitive(
                "Name",
                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
            EntityType.Create("Person", "MyModel", DataSpace.CSpace, null, new[] { property }, null);

            property.HasConventionalKeyName(.Should().BeFalse());
        }

        [TestMethod]
        public void IsTimestamp_returns_true_when_timestamp()
        {
            var builder = new DbModelBuilder();
            builder.Entity<EntityWithBinaryProperty>().Property(e => e.BinaryProperty)
                .IsRowVersion();
            var model = builder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var property = model.StoreModel.Container.EntitySets.First().ElementType.Properties.First(
                p => p.Name == "BinaryProperty");

            property.IsTimestamp(.Should().BeTrue());
        }

        [TestMethod]
        public void IsTimestamp_returns_false_when_not_timestamp()
        {
            var builder = new DbModelBuilder();
            builder.Entity<EntityWithBinaryProperty>().Property(e => e.BinaryProperty)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)
                .IsRequired()
                .HasMaxLength(8);
            var model = builder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var property = model.StoreModel.Container.EntitySets.First().ElementType.Properties.First(
                p => p.Name == "BinaryProperty");

            property.IsTimestamp(.Should().BeFalse());
        }

        [TestMethod]
        public void IsTimestamp_returns_false_when_not_computed()
        {
            var builder = new DbModelBuilder();
            builder.Entity<EntityWithBinaryProperty>().Property(e => e.BinaryProperty)
                .HasColumnType("timestamp")
                .IsRequired()
                .HasMaxLength(8);
            var model = builder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var property = model.StoreModel.Container.EntitySets.First().ElementType.Properties.First(
                p => p.Name == "BinaryProperty");

            property.IsTimestamp(.Should().BeFalse());
        }

        [TestMethod]
        public void IsTimestamp_returns_false_when_nullable()
        {
            var builder = new DbModelBuilder();
            builder.Entity<EntityWithBinaryProperty>().Property(e => e.BinaryProperty)
                .HasColumnType("timestamp")
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)
                .HasMaxLength(8);
            var model = builder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));

            var property = model.StoreModel.Container.EntitySets.First().ElementType.Properties.First(
                p => p.Name == "BinaryProperty");

            property.IsTimestamp(.Should().BeFalse());
        }

        private class EntityWithBinaryProperty
        {
            public int Id { get; set; }
            public byte[] BinaryProperty { get; set; }
        }
    }
}
