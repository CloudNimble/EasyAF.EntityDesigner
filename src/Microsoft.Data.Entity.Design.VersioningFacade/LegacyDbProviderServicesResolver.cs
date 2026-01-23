// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Legacy = System.Data.Common;

namespace Microsoft.Data.Entity.Design.VersioningFacade
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Data.Entity.Design.VersioningFacade.LegacyProviderWrapper;

    internal class LegacyDbProviderServicesResolver : IDbDependencyResolver
    {
        private const string MicrosoftDataSqlClient = "Microsoft.Data.SqlClient";

        public object GetService(Type type, object key)
        {
            var providerInvariantName = key as string;

            Debug.WriteLine($"[EF6Tools] LegacyDbProviderServicesResolver.GetService: type={type?.Name}, key='{key}'");

            if (type == typeof(DbProviderServices)
                && providerInvariantName != null)
            {
                Debug.WriteLine($"[EF6Tools] LegacyDbProviderServicesResolver: Checking '{providerInvariantName}' against MicrosoftDataSqlClient='{MicrosoftDataSqlClient}'");
                Debug.WriteLine($"[EF6Tools] LegacyDbProviderServicesResolver: StringEquals result = {string.Equals(providerInvariantName, MicrosoftDataSqlClient, StringComparison.OrdinalIgnoreCase)}");

                // Microsoft.Data.SqlClient does not implement the legacy System.Data.Common.DbProviderServices
                // interface, so it cannot be wrapped by LegacyDbProviderServicesWrapper. Instead, it should
                // be handled by the pre-registered SqlProviderServices in DbProviderServicesResolver.
                // Return null here to indicate this resolver cannot handle this provider.
                if (string.Equals(providerInvariantName, MicrosoftDataSqlClient, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[EF6Tools] LegacyDbProviderServicesResolver: RETURNING NULL for Microsoft.Data.SqlClient (should be handled by registered SqlProviderServices)");
                    return null;
                }

                Legacy.DbProviderFactory factory;
                try
                {
                    factory = Legacy.DbProviderFactories.GetFactory(providerInvariantName);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "The ADO.NET provider '{0}' is not registered on this machine. " +
                            "Please ensure the provider is installed and registered in the machine.config or app.config file.",
                            providerInvariantName),
                        ex);
                }

                if (factory == null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "DbProviderFactories.GetFactory returned null for provider '{0}'.",
                            providerInvariantName));
                }

                var serviceProvider = factory as IServiceProvider;
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "The ADO.NET provider '{0}' does not implement IServiceProvider and cannot be used with the EF6 designer. " +
                            "This typically occurs when using Microsoft.Data.SqlClient, which is designed for EF Core. " +
                            "Consider using System.Data.SqlClient instead for EF6 projects.",
                            providerInvariantName));
                }

                var legacyProviderServices = serviceProvider.GetService(typeof(Legacy.DbProviderServices)) as Legacy.DbProviderServices;

                if (legacyProviderServices == null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "The ADO.NET provider '{0}' does not support the legacy Entity Framework 6 provider model. " +
                            "The provider factory does not return DbProviderServices when queried. " +
                            "This typically occurs when using Microsoft.Data.SqlClient, which is designed for EF Core. " +
                            "Consider using System.Data.SqlClient instead for EF6 projects, or ensure the EF6 provider is properly installed.",
                            providerInvariantName));
                }

                Debug.WriteLine($"[EF6Tools] LegacyDbProviderServicesResolver: Creating LegacyDbProviderServicesWrapper for provider '{providerInvariantName}'");
                return new LegacyDbProviderServicesWrapper(legacyProviderServices);
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            var service = GetService(type, key);
            return service != null ? new[] { service } : Enumerable.Empty<object>();
        }
    }
}
