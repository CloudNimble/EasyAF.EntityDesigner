// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class TableDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            var code = new CSharpCodeHelper();
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            new TableDiscoverer(code.Should().BeNull().Discover(entitySet, model));
        }

        [TestMethod]
        public void Discover_returns_configuration_when_unconventional_name()
        {
            var code = new CSharpCodeHelper();
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().ToTable("Entity");
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            var configuration = new TableDiscoverer(code).Discover(entitySet, model) as TableConfiguration;

            configuration.Should().NotBeNull();
            configuration.Table.Should().Be("Entity");
            configuration.Schema.Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_unconventional_schema()
        {
            var code = new CSharpCodeHelper();
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().ToTable("Entities", "old");
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            var configuration = new TableDiscoverer(code).Discover(entitySet, model) as TableConfiguration;

            configuration.Should().NotBeNull();
            configuration.Table.Should().Be("Entities");
            configuration.Schema.Should().Be("old");
        }

        private class Entity
        {
            public int Id { get; set; }
        }
    }
}
