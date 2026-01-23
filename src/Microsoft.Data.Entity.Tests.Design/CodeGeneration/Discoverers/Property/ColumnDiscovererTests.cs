// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class ColumnDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            var code = new CSharpCodeHelper();
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new ColumnDiscoverer(code.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_configuration_when_name()
        {
            var code = new CSharpCodeHelper();
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Id).HasColumnName("EntityId");
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            var configuration = new ColumnDiscoverer(code).Discover(property, model) as ColumnConfiguration;

            configuration.Should().NotBeNull();
            configuration.Name.Should().Be("EntityId");
            configuration.TypeName.Should().BeNull();
            configuration.Order.Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_type()
        {
            var code = new CSharpCodeHelper();
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Name).HasColumnType("xml");
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            var configuration = new ColumnDiscoverer(code).Discover(property, model) as ColumnConfiguration;

            configuration.Should().NotBeNull();
            configuration.Name.Should().BeNull();
            configuration.TypeName.Should().Be("xml");
            configuration.Order.Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_order()
        {
            var code = new CSharpCodeHelper();
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().HasKey(e => new { e.Id, e.Name });
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            var configuration = new ColumnDiscoverer(code).Discover(property, model) as ColumnConfiguration;

            configuration.Should().NotBeNull();
            configuration.Name.Should().BeNull();
            configuration.TypeName.Should().BeNull();
            configuration.Order.Should().Be(0);
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
