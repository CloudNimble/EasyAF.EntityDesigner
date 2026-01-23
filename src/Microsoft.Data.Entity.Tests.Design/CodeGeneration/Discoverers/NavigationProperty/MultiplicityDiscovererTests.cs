// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class MultiplicityDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_configuration_when_many_to_many()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithMany(e => e.Entity1s);
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2);
            modelBuilder.Entity<Entity2>().Ignore(e => e.Entity1);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(e => e.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            bool isDefault;
            var configuration = MultiplicityDiscoverer.Discover(navigationProperty, out isDefault);

            isDefault.Should().BeTrue();
            configuration.Should().NotBeNull();
            configuration.LeftEntityType.Should().BeSameAs(entityType);
            configuration.LeftNavigationProperty.Should().BeSameAs(navigationProperty);
            Assert.Equal(
                RelationshipMultiplicity.Many,
                configuration.LeftNavigationProperty.FromEndMember.RelationshipMultiplicity);
            Assert.Equal(
                RelationshipMultiplicity.Many,
                configuration.RightNavigationProperty.FromEndMember.RelationshipMultiplicity);
        }

        [TestMethod]
        public void Discover_returns_configuration_when_many_to_many_and_more_than_one_association()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasMany(e => e.Entity2s).WithMany(e => e.Entity1s);
            modelBuilder.Entity<Entity2>().Ignore(e => e.Entity1);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(e => e.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2s");

            bool isDefault;
            var configuration = MultiplicityDiscoverer.Discover(navigationProperty, out isDefault);

            isDefault.Should().BeFalse();
            configuration.Should().NotBeNull();
            configuration.LeftEntityType.Should().BeSameAs(entityType);
            configuration.LeftNavigationProperty.Should().BeSameAs(navigationProperty);
            Assert.Equal(
                RelationshipMultiplicity.Many,
                configuration.LeftNavigationProperty.FromEndMember.RelationshipMultiplicity);
            Assert.Equal(
                RelationshipMultiplicity.Many,
                configuration.RightNavigationProperty.FromEndMember.RelationshipMultiplicity);
        }

        [TestMethod]
        public void Discover_returns_configuration_when_required_to_optional()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity1>().HasRequired(e => e.Entity2).WithOptional(e => e.Entity1);
            modelBuilder.Entity<Entity1>().Ignore(e => e.Entity2s);
            modelBuilder.Entity<Entity2>().Ignore(e => e.Entity1s);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First(e => e.Name == "Entity1");
            var navigationProperty = entityType.NavigationProperties.First(p => p.Name == "Entity2");

            bool isDefault;
            var configuration = MultiplicityDiscoverer.Discover(navigationProperty, out isDefault);

            isDefault.Should().BeFalse();
            configuration.Should().NotBeNull();
            configuration.LeftEntityType.Should().BeSameAs(entityType);
            configuration.LeftNavigationProperty.Should().BeSameAs(navigationProperty);
            Assert.Equal(
                RelationshipMultiplicity.ZeroOrOne,
                configuration.LeftNavigationProperty.FromEndMember.RelationshipMultiplicity);
            Assert.Equal(
                RelationshipMultiplicity.One,
                configuration.RightNavigationProperty.FromEndMember.RelationshipMultiplicity);
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
