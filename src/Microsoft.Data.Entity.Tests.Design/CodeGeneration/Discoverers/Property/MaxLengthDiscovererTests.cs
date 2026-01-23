// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class MaxLengthDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_inapplicable()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new MaxLengthDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_null_when_conventional_nonkey()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            new MaxLengthDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_null_when_conventional_key()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().HasKey(e => e.Name);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            new MaxLengthDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_configuration_when_binary()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Data).HasMaxLength(256);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Data");

            var configuration = new MaxLengthDiscoverer().Discover(property, model) as MaxLengthConfiguration;

            configuration.Should().NotBeNull();
            configuration.MaxLength.Should().Be(256);
        }

        [TestMethod]
        public void Discover_returns_configuration_when_string()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Name).HasMaxLength(30);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            var configuration = new MaxLengthDiscoverer().Discover(property, model) as MaxLengthStringConfiguration;

            configuration.Should().NotBeNull();
            configuration.MaxLength.Should().Be(30);
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public byte[] Data { get; set; }
        }
    }
}
