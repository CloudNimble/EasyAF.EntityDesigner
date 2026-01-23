// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    using System;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.Pluralization;
    using System.Data.Entity.SqlServer;
    using Microsoft.Data.Entity.Design.VersioningFacade.LegacyProviderWrapper;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class DependencyResolverTests
    {
        [TestMethod]
        public void DependencyResolver_resolves_IDbProviderServicesFactory()
        {
            DependencyResolver.GetService<DbProviderServices>("System.Data.SqlClient".Should().NotBeNull());
        }

        [TestMethod]
        public void DependencyResolver_resolves_IPluralizationService()
        {
            DependencyResolver.GetService<IPluralizationService>(.Should().NotBeNull());
        }

        [TestMethod]
        public void DependencyResolver_does_not_resolve_Object()
        {
            DependencyResolver.GetService<object>(.Should().BeNull());
        }

        [TestMethod]
        public void DependencyResolver_can_register_unregister_provider()
        {
            // Use a unique provider name to avoid interfering with pre-registered providers
            const string testProviderName = "Test.Provider.ForUnregisterTest";

            DependencyResolver.RegisterProvider(typeof(SqlProviderServices), testProviderName);
            try
            {
                Assert.Same(
                    SqlProviderServices.Instance,
                    DependencyResolver.GetService<DbProviderServices>(testProviderName));
            }
            finally
            {
                DependencyResolver.UnregisterProvider(testProviderName);
            }

            // After unregistering a fake provider, the legacy resolver throws because
            // the provider isn't registered with ADO.NET DbProviderFactories
            Assert.Throws<InvalidOperationException>(
                () => DependencyResolver.GetService<DbProviderServices>(testProviderName));
        }

        [TestMethod]
        public void EnsureProvider_registers_provider()
        {
            // Use a unique provider name to avoid interfering with pre-registered providers
            const string testProviderName = "Test.Provider.ForEnsureProviderTest";

            DependencyResolver.EnsureProvider(testProviderName, typeof(SqlProviderServices));
            try
            {
                Assert.Same(
                    SqlProviderServices.Instance,
                    DependencyResolver.GetService<DbProviderServices>(testProviderName));
            }
            finally
            {
                DependencyResolver.UnregisterProvider(testProviderName);
            }
        }

        [TestMethod]
        public void EnsureProvider_unregisters_provider_when_null()
        {
            // Use a unique provider name to avoid interfering with pre-registered providers
            const string testProviderName = "Test.Provider.ForEnsureProviderNullTest";

            DependencyResolver.RegisterProvider(typeof(SqlProviderServices), testProviderName);

            DependencyResolver.EnsureProvider(testProviderName, null);

            // After unregistering a fake provider with null, the legacy resolver throws because
            // the provider isn't registered with ADO.NET DbProviderFactories
            Assert.Throws<InvalidOperationException>(
                () => DependencyResolver.GetService<DbProviderServices>(testProviderName));
        }

        [TestMethod]
        public void DependencyResolver_preregisters_MicrosoftDataSqlClient()
        {
            // Microsoft.Data.SqlClient should be pre-registered to use SqlProviderServices
            // This happens in the static constructor of DependencyResolver
            var providerServices = DependencyResolver.GetService<DbProviderServices>("Microsoft.Data.SqlClient");

            providerServices.Should().NotBeNull();
            providerServices.Should().BeSameAs(SqlProviderServices.Instance);
        }

        [TestMethod]
        public void DependencyResolver_preregisters_SystemDataSqlClient()
        {
            // System.Data.SqlClient should also be pre-registered to use SqlProviderServices
            var providerServices = DependencyResolver.GetService<DbProviderServices>("System.Data.SqlClient");

            providerServices.Should().NotBeNull();
            providerServices.Should().BeSameAs(SqlProviderServices.Instance);
        }

        [TestMethod]
        public void DependencyResolver_MicrosoftDataSqlClient_does_not_use_legacy_wrapper()
        {
            // Verify Microsoft.Data.SqlClient returns SqlProviderServices, NOT LegacyDbProviderServicesWrapper
            var providerServices = DependencyResolver.GetService<DbProviderServices>("Microsoft.Data.SqlClient");

            providerServices.Should().NotBeNull();
            Assert.IsNotType<LegacyDbProviderServicesWrapper>(providerServices);
            Assert.IsType<SqlProviderServices>(providerServices);
        }

        [TestMethod]
        public void GetServices_returns_single_item_for_registered_provider()
        {
            var services = DependencyResolver.Instance.GetServices(typeof(DbProviderServices), "Microsoft.Data.SqlClient");

            services.Should().NotBeNull();
            var serviceList = new System.Collections.Generic.List<object>(services);
            Assert.Single(serviceList);
            serviceList[0].Should().BeSameAs(SqlProviderServices.Instance);
        }

        [TestMethod]
        public void GetServices_returns_empty_for_unknown_type()
        {
            var services = DependencyResolver.Instance.GetServices(typeof(object), null);

            services.Should().NotBeNull();
            Assert.Empty(services);
        }

        [TestMethod]
        public void GetServices_returns_single_item_for_pluralization_service()
        {
            var services = DependencyResolver.Instance.GetServices(typeof(IPluralizationService), null);

            services.Should().NotBeNull();
            var serviceList = new System.Collections.Generic.List<object>(services);
            Assert.Single(serviceList);
            Assert.IsAssignableFrom<IPluralizationService>(serviceList[0]);
        }

        [TestMethod]
        public void GetService_returns_null_for_DbProviderServices_with_null_key()
        {
            // When key is null, should return null (not throw)
            var result = DependencyResolver.GetService<DbProviderServices>(null);
            result.Should().BeNull();
        }
    }
}
