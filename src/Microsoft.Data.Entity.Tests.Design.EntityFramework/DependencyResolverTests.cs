// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure.Pluralization;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    [TestClass]
    public class DependencyResolverTests
    {
        [TestMethod]
        public void DependencyResolver_resolves_IPluralizationService()
        {
            DependencyResolver.GetService<IPluralizationService>().Should().NotBeNull();
        }

        [TestMethod]
        public void DependencyResolver_does_not_resolve_Object()
        {
            DependencyResolver.GetService<object>().Should().BeNull();
        }

        [TestMethod]
        public void DependencyResolver_can_register_unregister_provider()
        {
            const string testProviderName = "Test.Provider.ForUnregisterTest";

            DependencyResolver.RegisterProvider(Utils.SqlProviderServicesType, testProviderName);
            try
            {
                var service = DependencyResolver.GetService<DbProviderServices>(testProviderName);
                service.Should().NotBeNull();
                service.GetType().Name.Should().Be("SqlProviderServices");
            }
            finally
            {
                DependencyResolver.UnregisterProvider(testProviderName);
            }

            DependencyResolver.GetService<DbProviderServices>(testProviderName).Should().BeNull();
        }

        [TestMethod]
        public void EnsureProvider_registers_provider()
        {
            const string testProviderName = "Test.Provider.ForEnsureProviderTest";

            DependencyResolver.EnsureProvider(testProviderName, Utils.SqlProviderServicesType);
            try
            {
                var service = DependencyResolver.GetService<DbProviderServices>(testProviderName);
                service.Should().NotBeNull();
                service.GetType().Name.Should().Be("SqlProviderServices");
            }
            finally
            {
                DependencyResolver.UnregisterProvider(testProviderName);
            }
        }

        [TestMethod]
        public void EnsureProvider_unregisters_provider_when_null()
        {
            const string testProviderName = "Test.Provider.ForEnsureProviderNullTest";

            DependencyResolver.RegisterProvider(Utils.SqlProviderServicesType, testProviderName);
            DependencyResolver.EnsureProvider(testProviderName, null);

            DependencyResolver.GetService<DbProviderServices>(testProviderName).Should().BeNull();
        }

        [TestMethod]
        public void DependencyResolver_preregisters_MicrosoftDataSqlClient()
        {
            var providerServices = DependencyResolver.GetService<DbProviderServices>("Microsoft.Data.SqlClient");
            providerServices.Should().NotBeNull("Microsoft.Data.SqlClient should be resolvable");
        }

        [TestMethod]
        public void DependencyResolver_preregisters_SystemDataSqlClient()
        {
            var providerServices = DependencyResolver.GetService<DbProviderServices>("System.Data.SqlClient");
            providerServices.Should().NotBeNull("System.Data.SqlClient should be resolvable");
        }

        [TestMethod]
        public void DependencyResolver_MicrosoftDataSqlClient_resolves_to_sql_provider()
        {
            var providerServices = DependencyResolver.GetService<DbProviderServices>("Microsoft.Data.SqlClient");
            providerServices.Should().NotBeNull();
            providerServices.GetType().Name.Should().Be("SqlProviderServices");
        }

        [TestMethod]
        public void GetServices_returns_single_item_for_registered_provider()
        {
            var services = DependencyResolver.Instance.GetServices(typeof(DbProviderServices), "Microsoft.Data.SqlClient");
            services.Should().NotBeNull();
            List<object> serviceList = new System.Collections.Generic.List<object>(services);
            serviceList.Should().ContainSingle();
            serviceList[0].Should().NotBeNull();
        }

        [TestMethod]
        public void GetServices_returns_empty_for_unknown_type()
        {
            var services = DependencyResolver.Instance.GetServices(typeof(object), null);
            services.Should().NotBeNull();
            services.Should().BeEmpty();
        }

        [TestMethod]
        public void GetServices_returns_single_item_for_pluralization_service()
        {
            var services = DependencyResolver.Instance.GetServices(typeof(IPluralizationService), null);
            services.Should().NotBeNull();
            List<object> serviceList = new System.Collections.Generic.List<object>(services);
            serviceList.Should().ContainSingle();
            serviceList[0].Should().BeAssignableTo<IPluralizationService>();
        }

        [TestMethod]
        public void GetService_returns_null_for_DbProviderServices_with_null_key()
        {
            var result = DependencyResolver.GetService<DbProviderServices>(null);
            result.Should().BeNull();
        }
    }
}
