// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Data.Entity.Infrastructure.Pluralization;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Data.Entity.Design.VersioningFacade
{
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
            System.Diagnostics.Debug.WriteLine("[EasyAF.EntityDesigner] DependencyResolver static ctor: START");

            // Initialize static fields first
            PluralizationService = new EnglishPluralizationService();
            System.Diagnostics.Debug.WriteLine("[EasyAF.EntityDesigner] DependencyResolver static ctor: PluralizationService initialized");

            ProviderServicesResolver = new DbProviderServicesResolver();
            System.Diagnostics.Debug.WriteLine("[EasyAF.EntityDesigner] DependencyResolver static ctor: ProviderServicesResolver initialized");

            // Load SqlProviderServices at runtime to support SQL Server.
            System.Diagnostics.Debug.WriteLine("[EasyAF.EntityDesigner] DependencyResolver static ctor: Pre-registering SQL Server providers");
            Type sqlProviderType = Type.GetType(
                "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer");

            if (sqlProviderType != null)
            {
                // Pre-register SQL Server provider for both invariant names
                // Prefer Microsoft.Data.SqlClient (modern) but also support System.Data.SqlClient (legacy)
                ProviderServicesResolver.Register(sqlProviderType, "Microsoft.Data.SqlClient");
                ProviderServicesResolver.Register(sqlProviderType, "System.Data.SqlClient");
                System.Diagnostics.Debug.WriteLine("[EasyAF.EntityDesigner] DependencyResolver static ctor: SQL Server provider pre-registered for Microsoft.Data.SqlClient and System.Data.SqlClient");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[EasyAF.EntityDesigner] DependencyResolver static ctor: SqlProviderServices not found - SQL Server will use fallback resolution");
            }

            // Initialize Instance LAST to ensure all dependencies are ready
            Instance = new DependencyResolver();
            System.Diagnostics.Debug.WriteLine("[EasyAF.EntityDesigner] DependencyResolver static ctor: Instance initialized - END");
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
            // Note: The resolver performs its own validation including cross-assembly type checks
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
