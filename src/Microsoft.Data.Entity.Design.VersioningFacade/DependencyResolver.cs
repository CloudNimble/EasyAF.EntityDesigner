// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VersioningFacade
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Infrastructure.Pluralization;
    using System.Data.Entity.SqlServer;
    using System.Diagnostics;
    using System.Linq;

    internal sealed class DependencyResolver : IDbDependencyResolver
    {
        public static readonly DependencyResolver Instance;

        private static readonly EnglishPluralizationService PluralizationService;
        private static readonly DbProviderServicesResolver ProviderServicesResolver;

        /// <summary>
        /// Static constructor to pre-register known SQL Server providers.
        /// This ensures Microsoft.Data.SqlClient works without requiring explicit EnsureProvider calls.
        /// </summary>
        static DependencyResolver()
        {
            System.Diagnostics.Debug.WriteLine("[EF6Tools] DependencyResolver static ctor: START");

            // Initialize static fields first
            PluralizationService = new EnglishPluralizationService();
            System.Diagnostics.Debug.WriteLine("[EF6Tools] DependencyResolver static ctor: PluralizationService initialized");

            ProviderServicesResolver = new DbProviderServicesResolver();
            System.Diagnostics.Debug.WriteLine("[EF6Tools] DependencyResolver static ctor: ProviderServicesResolver initialized");

            // Pre-register both SQL Server providers to use SqlProviderServices.
            // This allows Microsoft.Data.SqlClient to work with the EF6 designer
            // without going through the legacy provider wrapping path.
            System.Diagnostics.Debug.WriteLine("[EF6Tools] DependencyResolver static ctor: Pre-registering SQL Server providers");
            ProviderServicesResolver.Register(typeof(SqlProviderServices), "System.Data.SqlClient");
            ProviderServicesResolver.Register(typeof(SqlProviderServices), "Microsoft.Data.SqlClient");
            System.Diagnostics.Debug.WriteLine("[EF6Tools] DependencyResolver static ctor: Providers registered");

            // Initialize Instance LAST to ensure all dependencies are ready
            Instance = new DependencyResolver();
            System.Diagnostics.Debug.WriteLine("[EF6Tools] DependencyResolver static ctor: Instance initialized - END");
        }

        private DependencyResolver()
        {
        }

        public static T GetService<T>(object key = null) where T : class
        {
            return (T)Instance.GetService(typeof(T), key);
        }

        public object GetService(Type type, object key)
        {
            if (type == typeof(IPluralizationService))
            {
                return PluralizationService;
            }

            return ProviderServicesResolver.GetService(type, key);
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            var service = GetService(type, key);
            return service != null ? new[] { service } : Enumerable.Empty<object>();
        }

        public static void RegisterProvider(Type type, string invariantName)
        {
            Debug.Assert(type != null, "type != null");
            Debug.Assert(
                typeof(DbProviderServices).IsAssignableFrom(type),
                "expected type derived from DbProviderServices");
            Debug.Assert(!string.IsNullOrWhiteSpace(invariantName), "invariantName cannot be null or empty string");

            ProviderServicesResolver.Register(type, invariantName);
        }

        public static void UnregisterProvider(string invariantName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(invariantName), "invariantName cannot be null or empty string");

            ProviderServicesResolver.Unregister(invariantName);
        }

        public static void EnsureProvider(string invariantName, Type type)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(invariantName), "invariantName is null or empty.");

            if (type == null)
            {
                UnregisterProvider(invariantName);
            }
            else
            {
                RegisterProvider(type, invariantName);
            }
        }
    }
}
