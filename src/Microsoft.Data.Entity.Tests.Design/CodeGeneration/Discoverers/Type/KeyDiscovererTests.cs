// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class KeyDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            new KeyDiscoverer(.Should().BeNull().Discover(entitySet, model));
        }

        [TestMethod]
        public void Discover_returns_configuration()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().HasKey(e => e.Name);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entitySet = model.ConceptualModel.Container.EntitySets.First();

            var configuration = new KeyDiscoverer().Discover(entitySet, model) as KeyConfiguration;

            configuration.Should().NotBeNull();
            configuration.KeyProperties.Count.Should().Be(1);
            Assert.Equal("Name", configuration.KeyProperties.First().Name);
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
