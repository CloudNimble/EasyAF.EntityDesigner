// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Discoverers.Properties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Discoverers.Property
{
    [TestClass]
    public class PrecisionDateTimeDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_inapplicable()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new PrecisionDateTimeDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_null_when_conventional()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "DateCreated");

            new PrecisionDateTimeDiscoverer().Discover(property, model).Should().BeNull();
        }

        [TestMethod]
        public void Discover_returns_configuration()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.DateCreated).HasPrecision(2);
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "DateCreated");

            PrecisionDateTimeConfiguration configuration = new PrecisionDateTimeDiscoverer()
                .Discover(property, model) as PrecisionDateTimeConfiguration;

            configuration.Should().NotBeNull();
            configuration.Precision.Should().Be(2);
        }

        private class Entity
        {
            public int Id { get; set; }

            [Column(TypeName = "datetime2")]
            public DateTime DateCreated { get; set; }
        }
    }
}
