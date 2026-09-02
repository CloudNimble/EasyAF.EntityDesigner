// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Discoverers.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Discoverers.Property
{
    [TestClass]
    public class NonUnicodeDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_inapplicable()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new NonUnicodeDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            new NonUnicodeDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Name).IsUnicode(false);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            NonUnicodeConfiguration configuration = new NonUnicodeDiscoverer().Discover(property, model) as NonUnicodeConfiguration;

            configuration.Should().NotBeNull();
        }

        private class Entity
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
