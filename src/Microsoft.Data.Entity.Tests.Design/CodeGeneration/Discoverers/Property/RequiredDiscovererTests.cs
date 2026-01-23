// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class RequiredDiscovererTests
    {
        [TestMethod]
        public void Discover_returns_null_when_nullable_reference_type()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            new RequiredDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_null_when_nonnullable_value_type()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "DateCreated");

            new RequiredDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_null_when_key()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Id");

            new RequiredDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_null_when_timestamp()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Timestamp");

            new RequiredDiscoverer(.Should().BeNull().Discover(property, model));
        }

        [TestMethod]
        public void Discover_returns_configuration()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Name).IsRequired();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var entityType = model.ConceptualModel.EntityTypes.First();
            var property = entityType.Properties.First(p => p.Name == "Name");

            var configuration = new RequiredDiscoverer().Discover(property, model) as RequiredConfiguration;

            configuration.Should().NotBeNull();
        }

        private class Entity
        {
            public string Id { get; set; }
            public DateTime DateCreated { get; set; }
            public string Name { get; set; }

            [Timestamp]
            public byte[] Timestamp { get; set; }
        }
    }
}
