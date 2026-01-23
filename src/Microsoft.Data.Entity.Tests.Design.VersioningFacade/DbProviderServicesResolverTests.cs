// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using SystemDataCommon = System.Data.Common;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    using System;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.SqlServer;
    using System.Globalization;
    using Microsoft.Data.Entity.Design.VersioningFacade.LegacyProviderWrapper;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class DbProviderServicesResolverTests
    {
        private const string SqlClientInvariantName = "System.Data.SqlClient";

        [TestMethod]
        public void Legacy_provider_services_resolved_by_default()
        {
            var resolver = new DbProviderServicesResolver();
            var providerServices = resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName);

            providerServices.Should().NotBeNull();
            Assert.IsType<LegacyDbProviderServicesWrapper>(providerServices);
        }

        [TestMethod]
        public void Can_register_unregister_provider()
        {
            var resolver = new DbProviderServicesResolver();
            resolver.Register(typeof(SqlProviderServices), SqlClientInvariantName);

            Assert.Same(
                SqlProviderServices.Instance,
                resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName));

            resolver.Unregister(SqlClientInvariantName);

            Assert.IsType<LegacyDbProviderServicesWrapper>(
                resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName));
        }

        [TestMethod]
        public void Unregistering_not_registered_provider_does_not_throw()
        {
            var resolver = new DbProviderServicesResolver();
            resolver.Unregister(SqlClientInvariantName);

            Assert.IsType<LegacyDbProviderServicesWrapper>(
                resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName));
        }

        [TestMethod]
        public void Registering_registered_provider_replaces_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();

            var resolver = new DbProviderServicesResolver();
            resolver.Register(mockProviderServices.Object.GetType(), SqlClientInvariantName);
            resolver.Register(typeof(SqlProviderServices), SqlClientInvariantName);

            Assert.Same(
                SqlProviderServices.Instance,
                resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName));

            resolver.Unregister(SqlClientInvariantName);

            Assert.IsType<LegacyDbProviderServicesWrapper>(
                resolver.GetService(typeof(DbProviderServices), SqlClientInvariantName));
        }

        [TestMethod]
        public void Resolving_provider_without_static_Instance_field_or_property_throws()
        {
            var mockProviderServices = new Mock<DbProviderServices>();

            var resolver = new DbProviderServicesResolver();
            resolver.Register(mockProviderServices.Object.GetType(), "fakeProvider");

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.EF6Providers_InstanceMissing,
                    mockProviderServices.Object.GetType().AssemblyQualifiedName),
                Assert.Throws<InvalidOperationException>(
                    () => resolver.GetService(typeof(DbProviderServices), "fakeProvider")).Message);
        }

        private class ProviderFake : DbProviderServices
        {
            public static object Instance
            {
                get { return new object(); }
            }

            #region Not Implemented

            protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
            {
                throw new NotImplementedException();
            }

            protected override string GetDbProviderManifestToken(SystemDataCommon.DbConnection connection)
            {
                throw new NotImplementedException();
            }

            protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [TestMethod]
        public void Resolving_provider_whose_Instance_returns_non_DbProviderServices_throws()
        {
            var resolver = new DbProviderServicesResolver();
            resolver.Register(typeof(ProviderFake), "fakeProvider");

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.EF6Providers_NotDbProviderServices,
                    typeof(ProviderFake).AssemblyQualifiedName),
                Assert.Throws<InvalidOperationException>(
                    () => resolver.GetService(typeof(DbProviderServices), "fakeProvider")).Message);
        }

        [TestMethod]
        public void Resolving_non_DbProviderServices_type_returns_null()
        {
            new DbProviderServicesResolver(.Should().BeNull().GetService(typeof(object), "abc"));
        }

        [TestMethod]
        public void Resolving_without_invariant_name_type_returns_null()
        {
            new DbProviderServicesResolver(.Should().BeNull().GetService(typeof(DbProviderServices), null));
            new DbProviderServicesResolver(.Should().BeNull().GetService(typeof(DbProviderServices), new object()));
        }
    }
}
