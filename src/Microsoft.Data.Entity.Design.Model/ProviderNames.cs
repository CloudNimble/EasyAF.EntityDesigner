// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model
{
    using System;

    /// <summary>
    /// Helper class for SQL Server provider invariant names.
    /// Consolidates provider name constants and comparison methods to support both
    /// System.Data.SqlClient and Microsoft.Data.SqlClient providers.
    /// </summary>
    internal static class ProviderNames
    {
        /// <summary>
        /// The legacy System.Data.SqlClient provider invariant name.
        /// </summary>
        internal const string SystemDataSqlClient = "System.Data.SqlClient";

        /// <summary>
        /// The modern Microsoft.Data.SqlClient provider invariant name.
        /// </summary>
        internal const string MicrosoftDataSqlClient = "Microsoft.Data.SqlClient";

        /// <summary>
        /// The SQL Server CE provider name prefix.
        /// </summary>
        internal const string SqlServerCePrefix = "System.Data.SqlServerCe";

        /// <summary>
        /// Checks if the given provider invariant name is a SQL Server provider
        /// (either System.Data.SqlClient or Microsoft.Data.SqlClient).
        /// </summary>
        /// <param name="providerInvariantName">The provider invariant name to check.</param>
        /// <returns>True if the provider is SQL Server, false otherwise.</returns>
        internal static bool IsSqlServerProvider(string providerInvariantName)
        {
            if (string.IsNullOrEmpty(providerInvariantName))
            {
                return false;
            }

            return providerInvariantName.Equals(SystemDataSqlClient, StringComparison.OrdinalIgnoreCase) ||
                   providerInvariantName.Equals(MicrosoftDataSqlClient, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the given provider invariant name is in the SQL Server family
        /// (SQL Server or SQL Server CE).
        /// </summary>
        /// <param name="providerInvariantName">The provider invariant name to check.</param>
        /// <returns>True if the provider is in the SQL Server family, false otherwise.</returns>
        internal static bool IsSqlServerFamilyProvider(string providerInvariantName)
        {
            if (string.IsNullOrEmpty(providerInvariantName))
            {
                return false;
            }

            return IsSqlServerProvider(providerInvariantName) ||
                   providerInvariantName.StartsWith(SqlServerCePrefix, StringComparison.Ordinal);
        }
    }
}
