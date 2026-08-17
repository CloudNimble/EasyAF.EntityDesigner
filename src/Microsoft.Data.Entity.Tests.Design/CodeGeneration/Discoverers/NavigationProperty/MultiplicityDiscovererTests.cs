// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Discoverers.NavigationProperties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Discoverers.NavigationProperty
{
    [TestClass]
    public class MultiplicityDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_configuration_when_many_to_many()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithMany(e => e.Entity1s);
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2);
            modelBuilder.Entity<Entity2>().Ignore(e => e.Entity1);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(e => e.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            var configuration = MultiplicityDiscoverer.Discover(navigationProperty, out bool isDefault);

            isDefault.Should().BeTrue();
            configuration.Should().NotBeNull();
            configuration.LeftEntityType.Should().BeSameAs(entityType);
            configuration.LeftNavigationProperty.Should().BeSameAs(navigationProperty);
            configuration.LeftNavigationProperty.FromEndMember.RelationshipMultiplicity.Should().Be(
                RelationshipMultiplicity.Many);
            configuration.RightNavigationProperty.FromEndMember.RelationshipMultiplicity.Should().Be(
                RelationshipMultiplicity.Many);
        }

        [TestMethod]
        public void Discover_returns_configuration_when_many_to_many_and_more_than_one_association()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithMany(e => e.Entity1s);
            modelBuilder.Entity<Entity2>().Ignore(e => e.Entity1);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(e => e.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            var configuration = MultiplicityDiscoverer.Discover(navigationProperty, out bool isDefault);

            isDefault.Should().BeFalse();
            configuration.Should().NotBeNull();
            configuration.LeftEntityType.Should().BeSameAs(entityType);
            configuration.LeftNavigationProperty.Should().BeSameAs(navigationProperty);
            configuration.LeftNavigationProperty.FromEndMember.RelationshipMultiplicity.Should().Be(
                RelationshipMultiplicity.Many);
            configuration.RightNavigationProperty.FromEndMember.RelationshipMultiplicity.Should().Be(
                RelationshipMultiplicity.Many);
        }

        [TestMethod]
        public void Discover_returns_configuration_when_required_to_optional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Entity2).WithOptional(e => e.Entity1);
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2s);
            modelBuilder.Entity<Entity2>().Ignore(e => e.Entity1s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(e => e.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2");

            var configuration = MultiplicityDiscoverer.Discover(navigationProperty, out bool isDefault);

            isDefault.Should().BeFalse();
            configuration.Should().NotBeNull();
            configuration.LeftEntityType.Should().BeSameAs(entityType);
            configuration.LeftNavigationProperty.Should().BeSameAs(navigationProperty);
            configuration.LeftNavigationProperty.FromEndMember.RelationshipMultiplicity.Should().Be(
                RelationshipMultiplicity.ZeroOrOne);
            configuration.RightNavigationProperty.FromEndMember.RelationshipMultiplicity.Should().Be(
                RelationshipMultiplicity.One);
        }

        private class Entity1
        {
            public int Id { get; set; }
            public Entity2 Entity2 { get; set; }
            public ICollection<Entity2> Entity2s { get; set; }
        }

        private class Entity2
        {
            public int Id { get; set; }
            public Entity1 Entity1 { get; set; }
            public ICollection<Entity1> Entity1s { get; set; }
        }
    }
}
