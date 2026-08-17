// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using SystemDataCommon = System.Data.Common;
using System;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    [TestClass]
    public class DbProviderServicesResolverTests
    {
        private const string SqlClientInvariantName = "System.Data.SqlClient";

        [TestMethod]
        public void Unregistered_provider_returns_null()
        {
            DbProviderServicesResolver resolver = new DbProviderServicesResolver();
            resolver.GetService(typeof(DbProviderServices), "UnknownProvider").Should().BeNull();
        }

        [TestMethod]
        public void Can_register_unregister_provider()
        {
            DbProviderServicesResolver resolver = new DbProviderServicesResolver();
            resolver.Register(Utils.SqlProviderServicesType, SqlClientInvariantName);

            var registeredService = resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName);
            registeredService.Should().NotBeNull();
            registeredService.GetType().Name.Should().Be("SqlProviderServices");

            resolver.Unregister(SqlClientInvariantName);

            resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName).Should().BeNull();
        }

        [TestMethod]
        public void Unregistering_not_registered_provider_does_not_throw()
        {
            DbProviderServicesResolver resolver = new DbProviderServicesResolver();
            resolver.Unregister(SqlClientInvariantName);
            resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName).Should().BeNull();
        }

        [TestMethod]
        public void Registering_registered_provider_replaces_provider()
        {
            Mock<DbProviderServices> mockProviderServices = new Mock<DbProviderServices>();

            DbProviderServicesResolver resolver = new DbProviderServicesResolver();
            resolver.Register(mockProviderServices.Object.GetType(), SqlClientInvariantName);
            resolver.Register(Utils.SqlProviderServicesType, SqlClientInvariantName);

            var service = resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName);
            service.Should().NotBeNull();
            service.GetType().Name.Should().Be("SqlProviderServices");

            resolver.Unregister(SqlClientInvariantName);
            resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName).Should().BeNull();
        }

        [TestMethod]
        public void Resolving_provider_without_static_Instance_field_or_property_returns_null()
        {
            Mock<DbProviderServices> mockProviderServices = new Mock<DbProviderServices>();

            DbProviderServicesResolver resolver = new DbProviderServicesResolver();
            resolver.Register(mockProviderServices.Object.GetType(), "fakeProvider");

            resolver.GetService(typeof(DbProviderServices), "fakeProvider").Should().BeNull();
        }

        private class ProviderFake : DbProviderServices
        {
            public static object Instance => new object();

            public override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
                => throw new NotImplementedException();

            public override string GetDbProviderManifestToken(SystemDataCommon.DbConnection connection)
                => throw new NotImplementedException();

            public override DbProviderManifest GetDbProviderManifest(string manifestToken)
                => throw new NotImplementedException();
        }

        [TestMethod]
        public void Resolving_provider_whose_Instance_returns_non_DbProviderServices_returns_null()
        {
            DbProviderServicesResolver resolver = new DbProviderServicesResolver();
            resolver.Register(typeof(ProviderFake), "fakeProvider");

            resolver.GetService(typeof(DbProviderServices), "fakeProvider").Should().BeNull();
        }

        [TestMethod]
        public void Resolving_non_DbProviderServices_type_returns_null()
        {
            new DbProviderServicesResolver().GetService(typeof(object), "abc").Should().BeNull();
        }

        [TestMethod]
        public void Resolving_without_invariant_name_type_returns_null()
        {
            new DbProviderServicesResolver().GetService(typeof(DbProviderServices), null).Should().BeNull();
            new DbProviderServicesResolver().GetService(typeof(DbProviderServices), new object()).Should().BeNull();
        }
    }
}
