// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Extensions
{
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class DbModelExtensionsTests
    {
        [TestMethod]
        public void GetProviderManifest_resolves_manifest()
        {
            var modelBuilder = new DbModelBuilder();
            var providerInfo = new DbProviderInfo("System.Data.SqlClient", "2012");
            var model = modelBuilder.Build(providerInfo);

            var providerServices = model.GetProviderManifest(DbConfiguration.DependencyResolver);

            providerServices.Should().NotBeNull();
            providerServices.NamespaceName.Should().Be("SqlServer");
        }

        [TestMethod]
        public void GetColumn_resolves_simple_property_mappings()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var property = model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Name");

            var column = model.GetColumn(property);

            column.Should().NotBeNull();
            column.Name.Should().Be("Name");
        }

        [TestMethod]
        public void GetColumn_resolves_rename_property_mappings()
        {
            var modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>().Property(e => e.Name).HasColumnName("Rename");
            var model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
            var property = model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Name");

            var column = model.GetColumn(property);

            column.Should().NotBeNull();
            column.Name.Should().Be("Rename");
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
