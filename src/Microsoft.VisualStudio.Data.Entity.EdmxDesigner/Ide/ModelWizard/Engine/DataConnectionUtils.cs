// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.VisualStudio.Data.Core;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Data.Services.SupportEntities;
using System;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine
{
    internal static class DataConnectionUtils
    {
        internal static IVsDataConnection GetDataConnection(
            IVsDataConnectionManager dataConnectionManager,
            IVsDataProviderManager dataProviderManager,
            string providerInvariantName,
            string providerConnectionString)
        {
            IVsDataConnection connection = null;
            var dataProviderGuid = Guid.Empty;

            foreach (var dataProvider in dataProviderManager.Providers.Values)
            {
                var invariantName = dataProvider.GetProperty("InvariantName") as string;
                if (!string.IsNullOrEmpty(invariantName)
                    && invariantName == providerInvariantName)
                {
                    dataProviderGuid = dataProvider.Guid;
                    break;
                }
            }
            Debug.Assert(dataProviderGuid != Guid.Empty, "The IVsDataProvider could not be found among the list of providers");
            if (dataProviderGuid != Guid.Empty)
            {
                connection = dataConnectionManager.GetConnection(dataProviderGuid, providerConnectionString, false);
            }

            return connection;
        }

        private static DbProviderFactory GetProviderFactoryForProviderGuid(IVsDataProviderManager dataProviderManager, Guid provider)
        {
            var invariantName = GetProviderInvariantName(dataProviderManager, provider);

            DbProviderFactory pf = null;

            if (!String.IsNullOrEmpty(invariantName))
            {
                try
                {
                    pf = DbProviderFactories.GetFactory(invariantName);
                }
                catch (ConfigurationException)
                {
                    // do nothing. we were tracing here before
                }
                catch (ArgumentException)
                {
                    // do nothing. we were tracing here before
                }
            }
            return pf;
        }

        internal static bool HasEntityFrameworkProvider(
            IVsDataProviderManager dataProviderManager,
            Guid provider,
            Project project,
            IServiceProvider serviceProvider)
        {
            Debug.Assert(dataProviderManager != null, "dataProviderManager is null.");
            Debug.Assert(project != null, "project is null.");
            Debug.Assert(serviceProvider != null, "serviceProvider is null.");

            return HasLegacyEntityFrameworkProvider(dataProviderManager, provider)
                   || HasModernEntityFrameworkProvider(dataProviderManager, provider, project, serviceProvider);
        }

        private static bool HasLegacyEntityFrameworkProvider(IVsDataProviderManager dataProviderManager, Guid provider)
        {
            Debug.Assert(dataProviderManager != null, "dataProviderManager is null.");

            var providerFactory = GetProviderFactoryForProviderGuid(dataProviderManager, provider);


            return providerFactory is IServiceProvider serviceProvider && LegacyDbProviderServicesUtils.CanGetDbProviderServices(serviceProvider);
        }

        private static bool HasModernEntityFrameworkProvider(
            IVsDataProviderManager dataProviderManager,
            Guid provider,
            Project project,
            IServiceProvider serviceProvider)
        {
            Debug.Assert(dataProviderManager != null, "dataProviderManager is null.");
            Debug.Assert(project != null, "project is null.");
            Debug.Assert(serviceProvider != null, "serviceProvider is null.");

            var invariantName = GetProviderInvariantName(dataProviderManager, provider);
            if (string.IsNullOrWhiteSpace(invariantName))
            {
                return false;
            }

            return VsUtils.IsModernProviderAvailable(invariantName, project, serviceProvider);
        }

        internal static string GetInitialCatalog(IVsDataProviderManager dataProviderManager, IVsDataConnection dataConnection)
        {
            string initialCatalog = null;

            var invariantName = GetProviderInvariantName(dataProviderManager, dataConnection.Provider);
            var props = GetConnectionProperties(dataProviderManager, dataConnection);
            if (props != null)
            {
                if (props.ContainsKey(LocalDataUtil.CONNECTION_PROPERTY_ATTACH_DB_FILENAME)
                    &&
                    !string.IsNullOrEmpty(props[LocalDataUtil.CONNECTION_PROPERTY_ATTACH_DB_FILENAME] as string))
                {
                    //sql client with "AttachDbFileName" parameter in the connection string.
                    props.TryGetValue(LocalDataUtil.CONNECTION_PROPERTY_ATTACH_DB_FILENAME, out object o);
                    initialCatalog = o as String;
                    if (initialCatalog != null)
                    {
                        initialCatalog = Path.GetFileNameWithoutExtension(initialCatalog);
                    }
                }
                else if (LocalDataUtil.IsSqlMobileConnectionString(invariantName))
                {
                    // sql CE
                    props.TryGetValue(LocalDataUtil.CONNECTION_PROPERTY_DATA_SOURCE, out object o);
                    initialCatalog = o as String;
                    if (initialCatalog != null)
                    {
                        initialCatalog = Path.GetFileNameWithoutExtension(initialCatalog);
                    }
                }
                else
                {
                    props.TryGetValue("Database", out object o);
                    initialCatalog = o as String;
                }
            }

            // save the default catalog
            if (string.IsNullOrEmpty(initialCatalog))
            {
                IVsDataSourceInformation sourceInformation = dataConnection.GetService(typeof(IVsDataSourceInformation)) as IVsDataSourceInformation;
                Debug.Assert(
                    sourceInformation != null,
                    "Could not find the IVsDataSourceInformation for this IVsDataConnection to determine the default catalog");
                if (sourceInformation != null)
                {
                    initialCatalog = sourceInformation["DefaultCatalog"] as string;
                    // Note: it is valid for initialCatalog to be null for certain providers which do not support that concept
                }
            }

            return initialCatalog;
        }

        internal static string GetInitialCatalog(string providerName, string providerConnectionString)
        {
            DbConnectionStringBuilder dbsb = new DbConnectionStringBuilder();
            dbsb.ConnectionString = providerConnectionString;
            var initialCatalog = String.Empty;

            if (dbsb.ContainsKey(LocalDataUtil.CONNECTION_PROPERTY_ATTACH_DB_FILENAME)
                &&
                !string.IsNullOrEmpty(dbsb[LocalDataUtil.CONNECTION_PROPERTY_ATTACH_DB_FILENAME] as string))
            {
                //sql client with "AttachDbFileName" parameter in the connection string.
                dbsb.TryGetValue(LocalDataUtil.CONNECTION_PROPERTY_ATTACH_DB_FILENAME, out object o);
                initialCatalog = o as String;
                if (initialCatalog != null)
                {
                    initialCatalog = Path.GetFileNameWithoutExtension(initialCatalog);
                }
            }
            else if (LocalDataUtil.IsSqlMobileConnectionString(providerName))
            {
                // sql CE
                dbsb.TryGetValue(LocalDataUtil.CONNECTION_PROPERTY_DATA_SOURCE, out object o);
                initialCatalog = o as String;
                if (initialCatalog != null)
                {
                    initialCatalog = Path.GetFileNameWithoutExtension(initialCatalog);
                }
            }
            else
            {
                dbsb.TryGetValue("Initial Catalog", out object o);
                initialCatalog = o as String;
            }

            return initialCatalog;
        }

        internal static string GetProviderInvariantName(IVsDataProviderManager dataProviderManager, Guid provider)
        {
            var invariantName = String.Empty;
            IVsDataProvider dataProvider = null;

            Debug.Assert(dataProviderManager != null, "_dataProviderManager is not initialized!");
            if (dataProviderManager != null)
            {
                dataProviderManager.Providers.TryGetValue(provider, out dataProvider);
                Debug.Assert(dataProvider != null, "Invalid provider Guid");
                if (dataProvider != null)
                {
                    invariantName = (string)dataProvider.GetProperty("InvariantName");
                }
            }

            Debug.Assert(
                !String.IsNullOrEmpty(invariantName),
                "provider " + dataProvider != null ? dataProvider.DisplayName : "(null)" + " has a null InvariantName");

            return invariantName;
        }

        internal static IVsDataConnectionProperties GetConnectionProperties(
            IVsDataProviderManager dataProviderManager, IVsDataConnection dataConnection)
        {
            if (dataConnection == null)
            {
                throw new ArgumentNullException("dataConnection");
            }

            IVsDataConnectionProperties properties = null;
            var provider = GetVsProvider(dataProviderManager, dataConnection);
            Debug.Assert(provider != null, "null provider value for dataConnection");
            if (provider != null)
            {
                var dataSource = GetVsDataSource(dataProviderManager, dataConnection);
                properties = provider.TryCreateObject<IVsDataConnectionProperties>(dataSource);
                Debug.Assert(properties != null, "Could not get connection properties service");

                if (properties != null)
                {
                    var connectionString = DecryptConnectionString(dataConnection)
                                           ?? string.Empty;
                    properties.Parse(connectionString);
                }
            }
            return properties;
        }

        private static Guid GetVsDataSource(IVsDataProviderManager dataProviderManager, IVsDataConnection dataConnection)
        {
            if (dataConnection == null)
            {
                throw new ArgumentNullException("dataConnection");
            }

            var guid = Guid.Empty;
            var provider = GetVsProvider(dataProviderManager, dataConnection);
            guid = provider.DeriveSource(DataProtection.DecryptString(dataConnection.EncryptedConnectionString));

            return guid;
        }

        internal static IVsDataProvider GetVsProvider(IVsDataProviderManager dataProviderManager, IVsDataConnection dataConnection)
        {
            if (dataConnection == null)
            {
                throw new ArgumentNullException("dataConnection");
            }

            dataProviderManager.Providers.TryGetValue(dataConnection.Provider, out IVsDataProvider vsDataProvider);

            Debug.Assert(
                vsDataProvider != null, "Data provider identified by guid '{0}' could not be loaded" + dataConnection.Provider.ToString());
            return vsDataProvider;
        }

        internal static string DecryptConnectionString(IVsDataConnection dataConnection)
        {
            return dataConnection == null
                       ? string.Empty
                       : DataProtection.DecryptString(dataConnection.EncryptedConnectionString);
        }
    }
}
