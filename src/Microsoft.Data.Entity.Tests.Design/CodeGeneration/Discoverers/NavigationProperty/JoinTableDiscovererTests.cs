// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.NavigationProperties;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Discoverers.NavigationProperties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Discoverers.NavigationProperty
{
    [TestClass]
    public class JoinTableDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_inapplicable()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithOptional();
            modelBuilder.Entity<Entity2>().Ignore(e => e.Entity1s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            new JoinTableDiscoverer().Discover(navigationProperty, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_conventional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithMany(e => e.Entity1s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            // NOTE: This makes the model readonly. Without it, assertions fail
            model.Compile();

            JoinTableConfiguration configuration = new JoinTableDiscoverer().Discover(navigationProperty, model) as JoinTableConfiguration;

            configuration.Should().NotBeNull();
            configuration.Schema.Should().BeNull();
            configuration.Table.Should().Be("Entity1Entity2");
            configuration.LeftKeys.Should().BeEmpty();
            configuration.RightKeys.Should().BeEmpty();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_conventional_from_other_end()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithMany(e => e.Entity1s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity2");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity1s");

            // NOTE: This makes the model readonly. Without it, assertions fail
            model.Compile();

            JoinTableConfiguration configuration = new JoinTableDiscoverer().Discover(navigationProperty, model) as JoinTableConfiguration;

            configuration.Should().NotBeNull();
            configuration.Schema.Should().BeNull();
            configuration.Table.Should().Be("Entity1Entity2");
            configuration.LeftKeys.Should().BeEmpty();
            configuration.RightKeys.Should().BeEmpty();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_unconventional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithMany(e => e.Entity1s).Map(
                m => m.ToTable("Associations", "new").MapLeftKey("Entity1Id").MapRightKey("Entity2Id"));
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(t => t.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            // NOTE: This makes the model readonly. Without it, assertions fail
            model.Compile();

            JoinTableConfiguration configuration = new JoinTableDiscoverer().Discover(navigationProperty, model) as JoinTableConfiguration;

            configuration.Should().NotBeNull();
            configuration.Schema.Should().Be("new");
            configuration.Table.Should().Be("Associations");
            configuration.LeftKeys.Should().BeEquivalentTo(new[] { "Entity1Id" });
            configuration.RightKeys.Should().BeEquivalentTo(new[] { "Entity2Id" });
        }

        private class Entity1
        {
            public int Id { get; set; }
            public ICollection<Entity2> Entity2s { get; set; }
        }

        private class Entity2
        {
            public int Id { get; set; }
            public ICollection<Entity1> Entity1s { get; set; }
        }
    }
}
