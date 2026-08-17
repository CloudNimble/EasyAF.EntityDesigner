// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Discoverers.Properties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class PrecisionDecimalDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_inapplicable()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new PrecisionDecimalDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Decimal");

            new PrecisionDecimalDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Decimal).HasPrecision(9, 0);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Decimal");

            PrecisionDecimalConfiguration configuration = new PrecisionDecimalDiscoverer()
                .Discover(property, model) as PrecisionDecimalConfiguration;

            configuration.Should().NotBeNull();
            configuration.Precision.Should().Be(9);
            configuration.Scale.Should().Be(0);
        }

        private class Entity
        {
            public int Id { get; set; }
            public decimal Decimal { get; set; }
        }
    }
}
