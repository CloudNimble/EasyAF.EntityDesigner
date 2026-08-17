// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Types;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Discoverers.Types;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class KeyDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            new KeyDiscoverer().Discover(entitySet, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().HasKey(e => e.Name);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            KeyConfiguration configuration = new KeyDiscoverer().Discover(entitySet, model) as KeyConfiguration;

            configuration.Should().NotBeNull();
            configuration.KeyProperties.Count.Should().Be(1);
            configuration.KeyProperties.First().Name.Should().Be("Name");
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
