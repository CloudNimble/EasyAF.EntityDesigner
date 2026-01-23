// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class DatabaseGeneratedDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_identity_int_key()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new DatabaseGeneratedDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_null_when_timestamp()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Timestamp");

            new DatabaseGeneratedDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_null_when_none_nonkey()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            new DatabaseGeneratedDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_configuration_when_nonidentity_int_key()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            var configuration = new DatabaseGeneratedDiscoverer()
                .Discover(property, model) as DatabaseGeneratedConfiguration;

            configuration.Should().NotBeNull();
            configuration.StoreGeneratedPattern.Should().Be(StoreGeneratedPattern.None);
        }

        [TestMethod]
        public void Discover_returns_configuration_when_computed()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Name).HasDatabaseGeneratedOption(
                DatabaseGeneratedOption.Computed);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            var configuration = new DatabaseGeneratedDiscoverer()
                .Discover(property, model) as DatabaseGeneratedConfiguration;

            configuration.Should().NotBeNull();
            configuration.StoreGeneratedPattern.Should().Be(StoreGeneratedPattern.Computed);
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }

            [Timestamp]
            public byte[] Timestamp { get; set; }
        }
    }
}
