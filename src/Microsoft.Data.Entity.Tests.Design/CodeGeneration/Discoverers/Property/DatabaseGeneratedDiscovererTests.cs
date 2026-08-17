// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Discoverers.Properties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class DatabaseGeneratedDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_identity_int_key()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new DatabaseGeneratedDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_timestamp()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Timestamp");

            new DatabaseGeneratedDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_none_nonkey()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            new DatabaseGeneratedDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_nonidentity_int_key()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            DatabaseGeneratedConfiguration configuration = new DatabaseGeneratedDiscoverer()
                .Discover(property, model) as DatabaseGeneratedConfiguration;

            configuration.Should().NotBeNull();
            configuration.StoreGeneratedPattern.Should().Be(StoreGeneratedPattern.None);
        }

        [TestMethod]
        public void Discover_returns_configuration_when_computed()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Name).HasDatabaseGeneratedOption(
                DatabaseGeneratedOption.Computed);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            DatabaseGeneratedConfiguration configuration = new DatabaseGeneratedDiscoverer()
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
