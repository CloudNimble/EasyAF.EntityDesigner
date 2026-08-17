// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using SystemDataCommon = System.Data.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Threading;

namespace Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb
{
    internal class StoreSchemaConnectionFactory
    {
#if NET
        private const string SqlClientInvariantName = "System.Data.SqlClient";
        private static bool _sqlClientRegistrationChecked;
#endif

        /// <summary>
        ///     Creates an EntityConnection loaded with the providers metadata for the latest available store schema.
        ///     Note that the targetEntityFrameworkVersion parameter uses internal EntityFramework version numbers as
        ///     described in the <see cref="EntityFrameworkVersion" /> class.
        /// </summary>
        /// <param name="resolver">Resolver used to resolve provider services.</param>
        /// <param name="providerInvariantName">The provider invariant name.</param>
        /// <param name="connectionString">The connection for the providers connection.</param>
        /// <param name="maxAllowedSchemaVersion">The maximum allowed Entity Framework schema version that is being targeted.</param>
        /// <param name="storeSchemaModelVersion">
        ///     The version of the store schema model supported by the provider. Can be either v1 or v3 (store schema model in v2 did not change
        ///     from v1, in v3 TVFs are supported and the provider needs to know how to handle Esql queries that ask for TVFs).
        ///     Note that schema view artifacts themselves are v1 since there is nothing that is needed to ask for v3 concepts that
        ///     cannot be expressed in v1 terms.
        ///     **This value MUST NOT be used as the version of the model that will be created for the user (which can be
        ///     any version regardless of this value), nor as the version of schema view artifacts (which is always v1)
        ///     but rather it is a version of concepts we are asking the provider to return details for (to put it simply if this is v3
        ///     we will ask the provider about TVFs, otherwise this is v1 and we don't ask about TVFs)**
        /// </param>
        /// <returns>An EntityConnection that can query the ConceptualSchemaDefinition for the provider.</returns>
        /// <remarks>virtual for testing</remarks>
        public virtual EntityConnection Create(
            IDbDependencyResolver resolver, string providerInvariantName, string connectionString,
            Version maxAllowedSchemaVersion, out Version storeSchemaModelVersion)
        {
            Debug.Assert(resolver != null, "resolver != null");
            Debug.Assert(!string.IsNullOrWhiteSpace(providerInvariantName), "providerInvarianName cannot be null or empty");
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionString), "connectionString cannot be null or empty");
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(maxAllowedSchemaVersion), "invalid maxAllowedSchemaVersion");

            // Only Version3 (EF6) is supported
            storeSchemaModelVersion = EntityFrameworkVersion.Version3;
            return Create(resolver, providerInvariantName, connectionString, storeSchemaModelVersion);
        }

        /// <summary>
        ///     Creates an EntityConnection loaded with the providers metadata for the store schema.
        /// </summary>
        /// <param name="resolver">Resolver used to resolve provider services.</param>
        /// <param name="providerInvariantName">The provider invariant name.</param>
        /// <param name="connectionString">The connection for the providers connection.</param>
        /// <param name="targetSchemaVersion">The target Entity Framework schema version that is being targeted.</param>
        /// <returns>An EntityConnection that can query the ConceptualSchemaDefinition for the provider.</returns>
        /// <remarks>virtual for testing</remarks>
        public virtual EntityConnection Create(
            IDbDependencyResolver resolver, string providerInvariantName, string connectionString, Version targetSchemaVersion)
        {
            Debug.Assert(resolver != null, "resolver != null");
            Debug.Assert(!string.IsNullOrWhiteSpace(providerInvariantName), "providerInvarianName cannot be null or empty");
            Debug.Assert(!string.IsNullOrWhiteSpace(connectionString), "connectionString cannot be null or empty");
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(targetSchemaVersion), "invalid targetSchemaVersion");

            EnsureSqlClientRegistered();

            SystemDataCommon.DbProviderFactory factory;
            try
            {
                factory = SystemDataCommon.DbProviderFactories.GetFactory(providerInvariantName);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources_VersioningFacade.EntityClient_InvalidStoreProvider,
                        providerInvariantName),
                    e);
            }

            var providerConnection = factory.CreateConnection();
            if (providerConnection == null)
            {
                throw new ProviderIncompatibleException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        Resources_VersioningFacade.ProviderFactoryReturnedNullFactory,
                        providerInvariantName));
            }
            
            providerConnection.ConnectionString = ReplaceMdsKeywords(connectionString);
            
            return new EntityConnection(
                GetProviderSchemaMetadataWorkspace(
                    resolver,
                    providerInvariantName,
                    providerConnection,
                    targetSchemaVersion),
                providerConnection);
        }
        
        /// <summary>
        ///     Registers the SQL Server ADO.NET provider factory when the runtime has no ambient registration for it.
        /// </summary>
        /// <remarks>
        ///     .NET Framework discovers provider factories through machine.config, so nothing is needed there. .NET
        ///     Core and later have no such file: a factory is only resolvable once something calls RegisterFactory,
        ///     and without it <see cref="SystemDataCommon.DbProviderFactories.GetFactory(string)" /> throws for a name
        ///     as ordinary as "System.Data.SqlClient". Registering here keeps headless callers - the CLI, tests -
        ///     working without every host having to know to do it. Registration is skipped when the name already
        ///     resolves, so a host that registered its own factory keeps it.
        /// </remarks>
        private static void EnsureSqlClientRegistered()
        {
#if NET
            if (_sqlClientRegistrationChecked)
            {
                return;
            }

            _sqlClientRegistrationChecked = true;

            try
            {
                SystemDataCommon.DbProviderFactories.GetFactory(SqlClientInvariantName);
            }
            catch (ArgumentException)
            {
                SystemDataCommon.DbProviderFactories.RegisterFactory(
                    SqlClientInvariantName, System.Data.SqlClient.SqlClientFactory.Instance);
            }
#endif
        }

        private static string ReplaceMdsKeywords(string connectionString)
        {
            connectionString = connectionString.Replace("Multiple Active Result Sets=", "MultipleActiveResultSets=")
                .Replace("Trust Server Certificate=", "TrustServerCertificate=")
                .Replace("Authentication=ActiveDirectoryIntegrated", "Authentication=Active Directory Integrated")
                .Replace("Authentication=ActiveDirectoryPassword", "Authentication=Active Directory Password")                          
                .Replace("Authentication=ActiveDirectoryInteractive", "Authentication=Active Directory Interactive");
            return connectionString;
        }

        private static MetadataWorkspace GetProviderSchemaMetadataWorkspace(
            IDbDependencyResolver resolver, string providerInvariantName, SystemDataCommon.DbConnection providerConnection,
            Version targetSchemaVersion)
        {
            Debug.Assert(resolver != null, "resolver != null");
            Debug.Assert(!string.IsNullOrWhiteSpace(providerInvariantName), "providerInvarianName cannot be null or empty");
            Debug.Assert(providerConnection != null, "providerConnection != null");
            Debug.Assert(EntityFrameworkVersion.IsValidVersion(targetSchemaVersion), "invalid targetSchemaVersion");

            string csdlName;
            string ssdlName;
            string mslName;
            if (targetSchemaVersion >= EntityFrameworkVersion.Version3)
            {
                csdlName = DbProviderManifest.ConceptualSchemaDefinitionVersion3;
                ssdlName = DbProviderManifest.StoreSchemaDefinitionVersion3;
                mslName = DbProviderManifest.StoreSchemaMappingVersion3;
            }
            else
            {
                csdlName = DbProviderManifest.ConceptualSchemaDefinition;
                ssdlName = DbProviderManifest.StoreSchemaDefinition;
                mslName = DbProviderManifest.StoreSchemaMapping;
            }

            var providerServices = resolver.GetService<DbProviderServices>(providerInvariantName);
            Debug.Assert(providerServices != null, "Trying to get unregistered provider.");

            var providerManifest =
                providerServices.GetProviderManifest(
                    providerServices.GetProviderManifestToken(providerConnection));

            var edmItemCollection = LoadEdmItemCollection(csdlName);
            var storeItemCollection = LoadStoreItemCollection(providerManifest, ssdlName);
            var mappingItemCollection = LoadMappingItemCollection(providerManifest, mslName, edmItemCollection, storeItemCollection);
            MetadataWorkspace workspace = new MetadataWorkspace(
                () => edmItemCollection,
                () => storeItemCollection,
                () => mappingItemCollection);

            // TODO: there is currently no public surface to do this (workitem 606 on codeplex)
            //// make the views generate here so we can wrap the provider schema problems
            //// in a ProviderIncompatibleException
            //ForceViewGeneration(workspace);

            return workspace;
        }

        private static EdmItemCollection LoadEdmItemCollection(string csdlName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(csdlName), "csdlName cannot be null or empty");

            using (var csdlReader = DbProviderServices.GetConceptualSchemaDefinition(csdlName))
            {
                EdmItemCollection edmItemCollection =
                    EdmItemCollection.Create(
                        new[] { csdlReader },
                        new ReadOnlyCollection<string>(
                            [
                                    GetProviderServicesInformationLocationPath(
                                        typeof(DbProviderServices).Assembly.FullName,
                                        csdlName)
                                ]),
                        out IList<EdmSchemaError> errors);

                Debug.Assert(
                    errors == null || errors.Count == 0, "Unexpected errors conceptual schema definition csdl");

                return edmItemCollection;
            }
        }

        private static StoreItemCollection LoadStoreItemCollection(DbProviderManifest providerManifest, string ssdlName)
        {
            Debug.Assert(providerManifest != null, "providerManifest != null");
            Debug.Assert(!string.IsNullOrWhiteSpace(ssdlName), "ssdlName cannot be null or empty");

            using (var ssdlReader = providerManifest.GetInformation(ssdlName))
            {
                StoreItemCollection storeItemCollection =
                    StoreItemCollection.Create(
                        new[] { ssdlReader },
                        new ReadOnlyCollection<string>(
                            [
                                    GetProviderServicesInformationLocationPath(
                                        providerManifest.GetType().Assembly.FullName,
                                        ssdlName)
                                ]),
                        DependencyResolver.Instance,
                        out IList<EdmSchemaError> errors);

                ThrowOnError(errors);

                return storeItemCollection;
            }
        }

        private static StorageMappingItemCollection LoadMappingItemCollection(
            DbProviderManifest providerManifest, string mslName, EdmItemCollection edmItemCollection,
            StoreItemCollection storeItemCollection)
        {
            using (var mslReader = providerManifest.GetInformation(mslName))
            {
                StorageMappingItemCollection mappingItemCollection =
                    StorageMappingItemCollection.Create(
                        edmItemCollection,
                        storeItemCollection,
                        new[] { mslReader },
                        new ReadOnlyCollection<string>(
                            [
                                    GetProviderServicesInformationLocationPath(
                                        providerManifest.GetType().Assembly.FullName,
                                        mslName)
                                ]),
                        out IList<EdmSchemaError> errors);

                ThrowOnError(errors);

                return mappingItemCollection;
            }
        }

        private static string GetProviderServicesInformationLocationPath(string assemblyName, string artifactName)
        {
            const string providerServicesInformationLocationPathTemplate = "DbProviderServices://{0}/{1}";

            return
                string.Format(
                    CultureInfo.InvariantCulture,
                    providerServicesInformationLocationPathTemplate,
                    assemblyName,
                    artifactName);
        }

        private static void ThrowOnError(ICollection<EdmSchemaError> errors)
        {
            Debug.Assert(errors != null, "errors != null");

            if (errors.Any(e => e.Severity == EdmSchemaErrorSeverity.Error))
            {
                throw new ProviderIncompatibleException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources_VersioningFacade.InvalidSchemaEncountered,
                        string.Join(Environment.NewLine, errors)));
            }
        }

        internal static bool IsCatchableExceptionType(Exception e)
        {
            // a 'catchable' exception is defined by what it is not.
            Debug.Assert(e != null, "Unexpected null exception!");

            var type = e.GetType();

            return
                type != typeof(StackOverflowException) &&
                type != typeof(OutOfMemoryException) &&
                type != typeof(ThreadAbortException) &&
                type != typeof(NullReferenceException) &&
                type != typeof(AccessViolationException) &&
                !typeof(SecurityException).IsAssignableFrom(type);
        }
    }
}
