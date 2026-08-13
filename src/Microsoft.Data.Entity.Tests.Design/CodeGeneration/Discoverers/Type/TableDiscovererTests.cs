// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Types;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Discoverers.Types;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class TableDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            var configuration = new TableDiscoverer(code).Discover(entitySet, model);

            configuration.Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_unconventional_name()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().ToTable("Entity");
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            TableConfiguration configuration = new TableDiscoverer(code).Discover(entitySet, model) as TableConfiguration;

            configuration.Should().NotBeNull();
            configuration.Table.Should().Be("Entity");
            configuration.Schema.Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration_when_unconventional_schema()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().ToTable("Entities", "old");
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            TableConfiguration configuration = new TableDiscoverer(code).Discover(entitySet, model) as TableConfiguration;

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
