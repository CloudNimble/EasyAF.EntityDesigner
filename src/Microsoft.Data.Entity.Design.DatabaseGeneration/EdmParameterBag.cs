// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Data.Entity.Design.DatabaseGeneration
{
    /// <summary>
    ///     Provides values for the parameters that are defined in the EdmParameterBag.ParameterName enumeration.
    /// </summary>
    public class EdmParameterBag
    {
        /// <summary>
        ///     An enumeration of parameter names that are used by classes in the Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators namespace when generating a database from a conceptual model.
        /// </summary>
        public enum ParameterName
        {
            /// <summary>
            ///     The SynchronizationContext that represents the state of the user interface thread in Visual Studio.
            /// </summary>
            SynchronizationContext,

            /// <summary>
            ///     The name of the IAssemblyLoader parameter object used to resolve and load an assembly given its name.
            /// </summary>
            AssemblyLoader,

            /// <summary>
            ///     The targeted version of the Entity Framework.
            /// </summary>
            TargetVersion,

            /// <summary>
            ///     The invariant name of the provider.
            /// </summary>
            ProviderInvariantName,

            /// <summary>
            ///     The provider's manifest token.
            /// </summary>
            ProviderManifestToken,

            /// <summary>
            ///     The provider connection string.
            /// </summary>
            ProviderConnectionString,

            /// <summary>
            ///     The name of the schema of the generated database.
            /// </summary>
            DatabaseSchemaName,

            /// <summary>
            ///     The name of the generated database.
            /// </summary>
            DatabaseName,

            /// <summary>
            ///     The path to the text template used to generate data definition language (DDL).
            /// </summary>
            DDLTemplatePath,

            /// <summary>
            ///     The path to the .edmx file from which the Generate Database Wizard was launched.
            /// </summary>
            EdmxPath
        }

        private readonly Dictionary<ParameterName, object> _parameterBag;

        /// <summary>
        ///     Constructor for EdmParameterBag
        /// </summary>
        /// <param name="syncContext">An optional SynchronizationContext that represents the state of the user interface thread in Visual Studio.</param>
        /// <param name="assemblyLoader">An optional IAssemblyLoader used to resolve and load an assembly given its name.</param>
        /// <param name="targetVersion">The targeted version of the Entity Framework.</param>
        /// <param name="providerInvariantName">The invariant name of the provider.</param>
        /// <param name="providerManifestToken">The provider's manifest token.</param>
        /// <param name="providerConnectionString">An optional provider connection string.</param>
        /// <param name="databaseSchemaName">The name of the schema of the generated database.</param>
        /// <param name="databaseName">The name of the generated database.</param>
        /// <param name="ddlTemplatePath">The path to the text template used to generate data definition language (DDL).</param>
        /// <param name="edmxPath">An optional path to the .edmx file from which the Generate Database Wizard was launched.</param>
        public EdmParameterBag(
            SynchronizationContext syncContext,
            IAssemblyLoader assemblyLoader,
            Version targetVersion,
            string providerInvariantName,
            string providerManifestToken,
            string providerConnectionString,
            string databaseSchemaName,
            string databaseName,
            string ddlTemplatePath,
            string edmxPath)
        {
            _parameterBag = new Dictionary<ParameterName, object>
            {
                { ParameterName.SynchronizationContext, syncContext },
                { ParameterName.AssemblyLoader, assemblyLoader },
                { ParameterName.TargetVersion, targetVersion },
                { ParameterName.ProviderInvariantName, providerInvariantName },
                { ParameterName.ProviderManifestToken, providerManifestToken },
                { ParameterName.ProviderConnectionString, providerConnectionString },
                { ParameterName.DatabaseSchemaName, databaseSchemaName },
                { ParameterName.DatabaseName, databaseName },
                { ParameterName.DDLTemplatePath, ddlTemplatePath },
                { ParameterName.EdmxPath, edmxPath }
            };
        }

        /// <summary>
        ///     Returns the value of the parameter for the specified <see cref="EdmParameterBag.ParameterName" />.
        /// </summary>
        /// <typeparam name="T">The type of the parameter with the name parameterName.</typeparam>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>
        ///     The value of the parameter for the specified <see cref="EdmParameterBag.ParameterName" />.
        /// </returns>
        public T GetParameter<T>(ParameterName parameterName) where T : class
        {
            _parameterBag.TryGetValue(parameterName, out object paramValue);
            return paramValue as T;
        }
    }
}
