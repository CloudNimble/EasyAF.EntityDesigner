// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.NavigationProperties;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Discoverers.NavigationProperties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class ForeignKeyDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_non_fk()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            new ForeignKeyDiscoverer().Discover(navigationProperty, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_required_to_optional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithOptional();
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(e => e.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            new ForeignKeyDiscoverer().Discover(navigationProperty, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_fk_equals_property_plus_pk()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithMany().HasForeignKey(e => e.TwoEntity2Id);
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            new ForeignKeyDiscoverer().Discover(navigationProperty, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_fk_equals_property_plus_pk_and_more_than_one_association()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithMany().HasForeignKey(e => e.TwoEntity2Id);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            new ForeignKeyDiscoverer().Discover(navigationProperty, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_fk_equals_entity_plus_pk()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithMany().HasForeignKey(e => e.Entity2Entity2Id);
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            new ForeignKeyDiscoverer().Discover(navigationProperty, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_fk_equals_pk()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithMany().HasForeignKey(e => e.Entity2Id);
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            new ForeignKeyDiscoverer().Discover(navigationProperty, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_fk_equals_entity_plus_pk_but_more_than_one_association()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithMany().HasForeignKey(e => e.Entity2Entity2Id);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            ForeignKeyConfiguration configuration = new ForeignKeyDiscoverer()
                .Discover(navigationProperty, model) as ForeignKeyConfiguration;

            configuration.Should().NotBeNull();
            configuration.Properties.Select(p => p.Name).Should().BeEquivalentTo(new[] { "Entity2Entity2Id" });
        }

        [TestMethod]
        public void Discover_returns_configuration_when_fk_equals_pk_but_more_than_one_association()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithMany().HasForeignKey(e => e.Entity2Id);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            ForeignKeyConfiguration configuration = new ForeignKeyDiscoverer()
                .Discover(navigationProperty, model) as ForeignKeyConfiguration;

            configuration.Should().NotBeNull();
            configuration.Properties.Select(p => p.Name).Should().BeEquivalentTo(new[] { "Entity2Id" });
        }

        [TestMethod]
        public void Discover_returns_configuration_when_composite_key()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity2>().HasKey(e => new { e.Entity2Id, e.Name });
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Two).WithMany().HasForeignKey(
                e => new { e.Entity2Id, e.Name });
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Two");

            ForeignKeyConfiguration configuration = new ForeignKeyDiscoverer()
                .Discover(navigationProperty, model) as ForeignKeyConfiguration;

            configuration.Should().NotBeNull();
            configuration.Properties.Select(p => p.Name).Should().BeEquivalentTo(new[] { "Entity2Id", "Name" });
        }

        private class Entity1
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Entity2Id { get; set; }
            public int Entity2Entity2Id { get; set; }
            public int TwoEntity2Id { get; set; }
            public Entity2 Two { get; set; }
            public ICollection<Entity2> Entity2s { get; set; }
        }

        private class Entity2
        {
            public int Entity2Id { get; set; }
            public string Name { get; set; }
        }
    }
}
