// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Legacy = System.Data.Common;
using LegacyCommandTrees = System.Data.Common.CommandTrees;
using LegacyMetadata = System.Data.Metadata.Edm;
using SystemData = System.Data;

namespace Microsoft.Data.Entity.Design.VersioningFacade.LegacyProviderWrapper
{
    using System;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Microsoft.Data.Entity.Design.VersioningFacade.LegacyProviderWrapper.LegacyMetadataExtensions;

    internal class LegacyDbProviderServicesWrapper : DbProviderServices
    {
        private readonly Legacy.DbProviderServices _wrappedProviderServices;

        private static readonly ConstructorInfo LegacyDbQueryCommandTreeCtor =
            typeof(LegacyCommandTrees.DbQueryCommandTree)
                .GetConstructor(
                    BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[]
                        {
                            typeof(LegacyMetadata.MetadataWorkspace),
                            typeof(LegacyMetadata.DataSpace),
                            typeof(LegacyCommandTrees.DbExpression)
                        },
                    null);

        public LegacyDbProviderServicesWrapper(Legacy.DbProviderServices wrappedProviderServices)
        {
            if (wrappedProviderServices == null)
            {
                throw new ArgumentNullException(nameof(wrappedProviderServices),
                    "The wrapped provider services cannot be null. " +
                    "This typically indicates the ADO.NET provider does not support the legacy Entity Framework 6 provider model.");
            }

            _wrappedProviderServices = wrappedProviderServices;
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            Debug.Assert(providerManifest != null, "providerManifest != null");
            Debug.Assert(
                providerManifest is LegacyDbProviderManifestWrapper, "providerManifest expected to be LegacyDbProviderManifestWrapper");
            Debug.Assert(commandTree != null, "commandTree != null");
            Debug.Assert(commandTree is DbQueryCommandTree, "Only query trees are supported");
            Debug.Assert(commandTree.DataSpace == DataSpace.SSpace, "SSpace tree expected");

            try
            {
                var legacyMetadata = commandTree.MetadataWorkspace.ToLegacyMetadataWorkspace();

                var legacyQuery =
                    ((DbQueryCommandTree)commandTree).Query.Accept(
                        new LegacyDbExpressionConverter(
                            (LegacyMetadata.StoreItemCollection)
                            legacyMetadata.GetItemCollection(LegacyMetadata.DataSpace.SSpace)));

                var legacyCommandTree =
                    (LegacyCommandTrees.DbCommandTree)LegacyDbQueryCommandTreeCtor.Invoke(
                        new object[]
                            {
                                legacyMetadata,
                                LegacyMetadata.DataSpace.SSpace,
                                legacyQuery
                            });

                return new LegacyDbCommandDefinitionWrapper(
                    _wrappedProviderServices.CreateCommandDefinition(
                        ((LegacyDbProviderManifestWrapper)providerManifest).WrappedManifest,
                        legacyCommandTree));
            }
            catch (SystemData.ProviderIncompatibleException exception)
            {
                throw new ProviderIncompatibleException(exception.Message, exception.InnerException);
            }
        }

        protected override string GetDbProviderManifestToken(Legacy.DbConnection connection)
        {
            // EF Designer does not unwrap ProviderIncompatibleExceptions and this method is protected and
            // is called by GetProviderManifest method which will wrap any exception in ProviderIncompatibleException.
            // Therefore even if the wrapped provider services throws a legacy ProviderIncompatibleException
            // it will be wrapped in non-legacy ProviderCompatibleException. Unwrapping the legacy here
            // ProviderIncompatibleException would be tricky and should not be done unless really necessary.
            return _wrappedProviderServices.GetProviderManifestToken(connection);
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            var legacyManifest = _wrappedProviderServices.GetProviderManifest(manifestToken);

            if (legacyManifest == null)
            {
                throw new ProviderIncompatibleException(
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        "The ADO.NET provider returned a null provider manifest for token '{0}'. " +
                        "This typically indicates the provider does not recognize the manifest token, " +
                        "or the provider does not fully support the Entity Framework 6 legacy provider model. " +
                        "If you are using Microsoft.Data.SqlClient, consider using System.Data.SqlClient instead for EF6 projects.",
                        manifestToken ?? "(null)"));
            }

            return new LegacyDbProviderManifestWrapper(legacyManifest);
        }
    }
}
