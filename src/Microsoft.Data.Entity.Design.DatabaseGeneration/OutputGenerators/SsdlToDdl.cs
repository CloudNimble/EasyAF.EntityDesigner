// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.DatabaseGeneration.Properties;
using Microsoft.Data.Entity.Design.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators
{
    /// <summary>
    ///     Renders Transact-SQL from a store model. This is the in-box replacement for the
    ///     <c>SSDLToSQL10.tt</c> text template the Generate Database Wizard used to run through Visual Studio's
    ///     T4 host.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The script drops the objects described by the existing store model, then creates the tables,
    ///         primary keys, and foreign keys described by the new store model. SQL Server scripts qualify
    ///         objects with a schema and guard drops with <c>OBJECT_ID</c> checks; SQL Server Compact scripts
    ///         omit schemas, <c>USE</c>, and those guards.
    ///     </para>
    ///     <para>
    ///         This lives in <c>Microsoft.Data.Entity.Design.DatabaseGeneration</c> so the Visual Studio wizard
    ///         and the command-line tool can share one implementation. Custom <c>.tt</c> files remain an
    ///         optional override in the Visual Studio host.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var generator = new SsdlToDdl();
    ///     string ddl = generator.Generate(ssdl, existingSsdl, parameters);
    ///     File.WriteAllText("Model.sql", ddl);
    ///     </code>
    /// </example>
    public sealed class SsdlToDdl : IDdlGenerator
    {
        #region Fields

        private const string SqlCeProviderPrefix = "System.Data.SqlServerCe";
        private readonly DateTime _generatedAt;

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a generator that stamps the script with the current local time.
        /// </summary>
        public SsdlToDdl()
            : this(DateTime.Now)
        {
        }

        /// <summary>
        ///     Creates a generator that stamps the script with <paramref name="generatedAt" />.
        /// </summary>
        /// <param name="generatedAt">The timestamp written into the script header.</param>
        public SsdlToDdl(DateTime generatedAt)
        {
            _generatedAt = generatedAt;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Generates the DDL that drops the objects described by <paramref name="existingSsdl" /> and creates those
        ///     described by <paramref name="ssdl" />.
        /// </summary>
        /// <param name="ssdl">The store model to create objects for.</param>
        /// <param name="existingSsdl">The store model currently recorded in the .edmx file. May be empty.</param>
        /// <param name="edmParameterBag">
        ///     Supplies the provider invariant name, database schema name, optional database name, optional .edmx path,
        ///     and target Entity Framework version.
        /// </param>
        /// <returns>The generated T-SQL script.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="edmParameterBag" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when a required parameter is missing, the target Entity Framework version is unrecognized, or
        ///     <paramref name="ssdl" /> is not a valid store model.
        /// </exception>
        public string Generate(string ssdl, string existingSsdl, EdmParameterBag edmParameterBag)
        {
            if (edmParameterBag is null)
            {
                throw new ArgumentNullException(nameof(edmParameterBag));
            }

            var targetFrameworkVersion = edmParameterBag.GetParameter<Version>(EdmParameterBag.ParameterName.TargetVersion);
            if (targetFrameworkVersion is null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        DatabaseGenerationResources.ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.TargetVersion));
            }

            if (false == EntityFrameworkVersion.IsValidVersion(targetFrameworkVersion))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, DatabaseGenerationResources.ErrorNonValidTargetVersion, targetFrameworkVersion));
            }

            var providerInvariantName = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.ProviderInvariantName);
            if (String.IsNullOrWhiteSpace(providerInvariantName))
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        DatabaseGenerationResources.ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.ProviderInvariantName));
            }

            var databaseSchemaName = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.DatabaseSchemaName);
            if (String.IsNullOrWhiteSpace(databaseSchemaName))
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        DatabaseGenerationResources.ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.DatabaseSchemaName));
            }

            var store = EdmExtension.CreateAndValidateStoreItemCollection(
                ssdl, targetFrameworkVersion, DependencyResolver.Instance, false);

            var existingStore = TryCreateExistingStore(existingSsdl, targetFrameworkVersion, out var existingStoreIsValid);

            var isSqlCe = IsSqlCe(providerInvariantName);
            var script = new StringBuilder();

            if (false == existingStoreIsValid)
            {
                script.AppendLine("-- Warning: There were errors validating the existing SSDL. Drop statements");
                script.AppendLine("-- will not be generated.");
            }

            AppendHeader(script, edmParameterBag, isSqlCe);

            if (false == isSqlCe)
            {
                script.AppendLine("SET QUOTED_IDENTIFIER OFF;");
                AppendGo(script);

                var databaseName = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.DatabaseName);
                if (false == String.IsNullOrEmpty(databaseName))
                {
                    script.AppendLine("USE [" + EscapeIdentifier(databaseName) + "];");
                    AppendGo(script);
                }

                AppendCreateSchemas(script, store);
            }

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine("-- Dropping existing FOREIGN KEY constraints");
            if (isSqlCe)
            {
                script.AppendLine("-- NOTE: if the constraint does not exist, an ignorable error will be reported.");
            }

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine();
            if (existingStoreIsValid && existingStore is not null)
            {
                AppendDropForeignKeys(script, existingStore, isSqlCe);
            }

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine("-- Dropping existing tables");
            if (isSqlCe)
            {
                script.AppendLine("-- NOTE: if the table does not exist, an ignorable error will be reported.");
            }

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine();
            if (existingStoreIsValid && existingStore is not null)
            {
                AppendDropTables(script, existingStore, isSqlCe);
            }

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine("-- Creating all tables");
            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine();
            AppendCreateTables(script, store, targetFrameworkVersion, isSqlCe);

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine("-- Creating all PRIMARY KEY constraints");
            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine();
            AppendCreatePrimaryKeys(script, store, isSqlCe);

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine("-- Creating all FOREIGN KEY constraints");
            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine();
            AppendCreateForeignKeys(script, store, isSqlCe);

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine("-- Script has ended");
            script.AppendLine("-- --------------------------------------------------");

            return script.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Writes <c>CREATE INDEX</c> and <c>ALTER TABLE ... ADD CONSTRAINT</c> statements for each association.
        /// </summary>
        private static void AppendCreateForeignKeys(StringBuilder script, StoreItemCollection store, bool isSqlCe)
        {
            foreach (var associationSet in store.GetAllAssociationSets())
            {
                var constraint = associationSet.ElementType.ReferentialConstraints.Single();
                var dependentSetEnd = associationSet.AssociationSetEnds.Single(
                    ase => ase.CorrespondingAssociationEndMember == constraint.ToRole);
                var principalSetEnd = associationSet.AssociationSetEnds.Single(
                    ase => ase.CorrespondingAssociationEndMember == constraint.FromRole);
                var schemaName = EscapeIdentifier(dependentSetEnd.EntitySet.GetSchemaName());
                var dependentTableName = EscapeIdentifier(dependentSetEnd.EntitySet.GetTableName());
                var principalTableName = EscapeIdentifier(principalSetEnd.EntitySet.GetTableName());
                var constraintName = WriteFkConstraintName(constraint);

                script.AppendLine(
                    "-- Creating foreign key on " + WriteColumns(constraint.ToProperties, ',') + " in table '" + dependentTableName + "'");
                script.AppendLine("ALTER TABLE " + Qualify(schemaName, dependentTableName, isSqlCe));
                script.AppendLine("ADD CONSTRAINT [" + constraintName + "]");
                script.AppendLine("    FOREIGN KEY (" + WriteColumns(constraint.ToProperties, ',') + ")");
                script.AppendLine(
                    "    REFERENCES " + Qualify(schemaName, principalTableName, isSqlCe));
                script.AppendLine("        (" + WriteColumns(constraint.FromProperties, ',') + ")");
                script.AppendLine("    ON DELETE " + GetDeleteAction(constraint) + " ON UPDATE NO ACTION;");
                AppendGo(script);

                var keyProperties = dependentSetEnd.EntitySet.ElementType.GetKeyProperties()
                    .Take(constraint.ToProperties.Count())
                    .OrderBy(p => p.Name);
                var fkProperties = constraint.ToProperties.OrderBy(p => p.Name);
                if (false == keyProperties.SequenceEqual(fkProperties))
                {
                    script.AppendLine("-- Creating non-clustered index for FOREIGN KEY '" + constraintName + "'");
                    script.AppendLine("CREATE INDEX [IX_" + constraintName + "]");
                    script.AppendLine("ON " + Qualify(schemaName, dependentTableName, isSqlCe));
                    script.AppendLine("    (" + WriteColumns(constraint.ToProperties, ',') + ");");
                    AppendGo(script);
                }
            }
        }

        /// <summary>
        ///     Writes a clustered (SQL Server) or unclustered (SQL Compact) primary key for each entity set.
        /// </summary>
        private static void AppendCreatePrimaryKeys(StringBuilder script, StoreItemCollection store, bool isSqlCe)
        {
            foreach (var entitySet in store.GetAllEntitySets())
            {
                var schemaName = EscapeIdentifier(entitySet.GetSchemaName());
                var tableName = EscapeIdentifier(entitySet.GetTableName());
                var keyColumns = WriteColumns(entitySet.ElementType.GetKeyProperties(), ',');

                script.AppendLine("-- Creating primary key on " + keyColumns + " in table '" + tableName + "'");
                script.AppendLine("ALTER TABLE " + Qualify(schemaName, tableName, isSqlCe));
                script.AppendLine("ADD CONSTRAINT [PK_" + tableName + "]");
                if (isSqlCe)
                {
                    script.AppendLine("    PRIMARY KEY (" + keyColumns + " );");
                }
                else
                {
                    script.AppendLine("    PRIMARY KEY CLUSTERED (" + keyColumns + " ASC);");
                }

                AppendGo(script);
            }
        }

        /// <summary>
        ///     Writes <c>CREATE SCHEMA</c> statements for every distinct schema in <paramref name="store" />.
        /// </summary>
        private static void AppendCreateSchemas(StringBuilder script, StoreItemCollection store)
        {
            foreach (var unescapedSchemaName in store.GetAllEntitySets().Select(es => es.GetSchemaName()).Distinct())
            {
                script.AppendLine(
                    "IF SCHEMA_ID(N'" + EscapeLiteral(unescapedSchemaName) + "') IS NULL EXECUTE(N'CREATE SCHEMA ["
                    + EscapeIdentifier(unescapedSchemaName) + "]');");
            }

            AppendGo(script);
        }

        /// <summary>
        ///     Writes a <c>CREATE TABLE</c> statement for each entity set.
        /// </summary>
        private static void AppendCreateTables(
            StringBuilder script, StoreItemCollection store, Version targetFrameworkVersion, bool isSqlCe)
        {
            foreach (var entitySet in store.GetAllEntitySets())
            {
                var schemaName = EscapeIdentifier(entitySet.GetSchemaName());
                var tableName = EscapeIdentifier(entitySet.GetTableName());

                script.AppendLine("-- Creating table '" + tableName + "'");
                script.AppendLine("CREATE TABLE " + Qualify(schemaName, tableName, isSqlCe) + " (");

                var properties = entitySet.ElementType.Properties;
                for (var p = 0; p < properties.Count; p++)
                {
                    var prop = properties[p];
                    var comma = p < properties.Count - 1 ? "," : String.Empty;
                    script.AppendLine(
                        "    [" + EscapeIdentifier(prop.Name) + "] " + prop.ToStoreType() + " "
                        + WriteIdentity(prop, targetFrameworkVersion) + " " + WriteNullable(prop.Nullable) + comma);
                }

                script.AppendLine(");");
                AppendGo(script);
            }
        }

        /// <summary>
        ///     Writes drop statements for every foreign key in <paramref name="existingStore" />.
        /// </summary>
        private static void AppendDropForeignKeys(StringBuilder script, StoreItemCollection existingStore, bool isSqlCe)
        {
            foreach (var associationSet in existingStore.GetAllAssociationSets())
            {
                var constraint = associationSet.ElementType.ReferentialConstraints.Single();
                var constraintName = EscapeIdentifier(WriteFkConstraintName(constraint));
                var dependentSetEnd = associationSet.AssociationSetEnds.Single(
                    ase => ase.CorrespondingAssociationEndMember == constraint.ToRole);
                var schemaName = EscapeIdentifier(dependentSetEnd.EntitySet.GetSchemaName());
                var dependentTableName = EscapeIdentifier(dependentSetEnd.EntitySet.GetTableName());

                if (false == isSqlCe)
                {
                    script.AppendLine(
                        "IF OBJECT_ID(N'[" + EscapeLiteral(schemaName) + "].[" + EscapeLiteral(constraintName) + "]', 'F') IS NOT NULL");
                }

                script.AppendLine(
                    "    ALTER TABLE " + Qualify(schemaName, dependentTableName, isSqlCe) + " DROP CONSTRAINT [" + constraintName + "];");
                AppendGo(script);
            }
        }

        /// <summary>
        ///     Writes drop statements for every table in <paramref name="existingStore" />.
        /// </summary>
        private static void AppendDropTables(StringBuilder script, StoreItemCollection existingStore, bool isSqlCe)
        {
            foreach (var entitySet in existingStore.GetAllEntitySets())
            {
                var schemaName = EscapeIdentifier(entitySet.GetSchemaName());
                var tableName = EscapeIdentifier(entitySet.GetTableName());

                if (false == isSqlCe)
                {
                    script.AppendLine(
                        "IF OBJECT_ID(N'[" + EscapeLiteral(schemaName) + "].[" + EscapeLiteral(tableName) + "]', 'U') IS NOT NULL");
                }

                script.AppendLine("    DROP TABLE " + Qualify(schemaName, tableName, isSqlCe) + ";");
                AppendGo(script);
            }
        }

        /// <summary>
        ///     Writes a <c>GO</c> batch separator followed by a blank line.
        /// </summary>
        private static void AppendGo(StringBuilder script)
        {
            script.AppendLine("GO");
            script.AppendLine();
        }

        /// <summary>
        ///     Writes the script banner, including the optional originating .edmx path.
        /// </summary>
        private void AppendHeader(StringBuilder script, EdmParameterBag edmParameterBag, bool isSqlCe)
        {
            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine(
                isSqlCe
                    ? "-- Entity Designer DDL Script for SQL Server Compact Edition"
                    : "-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure");
            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine("-- Date Created: " + _generatedAt);
            var edmxPath = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.EdmxPath);
            if (false == String.IsNullOrEmpty(edmxPath))
            {
                script.AppendLine("-- Generated from EDMX file: " + EscapeIdentifier(edmxPath));
            }

            script.AppendLine("-- --------------------------------------------------");
            script.AppendLine();
        }

        /// <summary>
        ///     Doubles closing brackets so an identifier can sit inside <c>[...]</c>.
        /// </summary>
        private static string EscapeIdentifier(string userIdentifier)
        {
            return userIdentifier.Replace("]", "]]");
        }

        /// <summary>
        ///     Doubles single quotes so a value can sit inside an N'...' literal.
        /// </summary>
        private static string EscapeLiteral(string userLiteral)
        {
            return userLiteral.Replace("'", "''");
        }

        /// <summary>
        ///     Returns CASCADE when the principal end deletes dependents, otherwise NO ACTION.
        /// </summary>
        private static string GetDeleteAction(ReferentialConstraint refConstraint)
        {
            return refConstraint.FromRole.DeleteBehavior == OperationAction.Cascade
                ? "CASCADE"
                : "NO ACTION";
        }

        /// <summary>
        ///     Returns whether <paramref name="property" /> is a SQL integer or decimal type that can take IDENTITY.
        /// </summary>
        private static bool IsIntegerOrDecimalType(EdmProperty property)
        {
            HashSet<string> sqlIntegerOrDecimalTypes =
            [
                "int",
                "bigint",
                "smallint",
                "double",
                "decimal",
                "float",
                "real",
                "tinyint"
            ];

            var edmType = property.TypeUsage?.EdmType;
            return edmType is not null && sqlIntegerOrDecimalTypes.Contains(edmType.Name);
        }

        /// <summary>
        ///     Returns whether <paramref name="providerInvariantName" /> is a SQL Server Compact provider.
        /// </summary>
        private static bool IsSqlCe(string providerInvariantName)
        {
            return providerInvariantName.StartsWith(SqlCeProviderPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Qualifies a table with its schema for SQL Server, or returns the table name alone for SQL Compact.
        /// </summary>
        private static string Qualify(string schemaName, string tableName, bool isSqlCe)
        {
            return isSqlCe
                ? "[" + tableName + "]"
                : "[" + schemaName + "].[" + tableName + "]";
        }

        /// <summary>
        ///     Attempts to load the existing store model. Empty input is treated as "no existing objects".
        /// </summary>
        private static StoreItemCollection TryCreateExistingStore(
            string existingSsdl, Version targetFrameworkVersion, out bool existingStoreIsValid)
        {
            if (String.IsNullOrWhiteSpace(existingSsdl))
            {
                existingStoreIsValid = true;
                return null;
            }

            var existingStore = EdmExtension.CreateStoreItemCollection(
                existingSsdl, targetFrameworkVersion, DependencyResolver.Instance, out IList<EdmSchemaError> existingSsdlErrors);
            existingStoreIsValid = existingSsdlErrors is null || existingSsdlErrors.Count == 0;
            return existingStoreIsValid ? existingStore : null;
        }

        /// <summary>
        ///     Writes bracketed, identifier-escaped column names separated by <paramref name="delimiter" />.
        /// </summary>
        private static string WriteColumns(IEnumerable<EdmProperty> properties, char delimiter)
        {
            var serializedProperties = new StringBuilder();
            foreach (var property in properties)
            {
                serializedProperties.Append("[" + EscapeIdentifier(property.Name) + "]");
                serializedProperties.Append(delimiter + " ");
            }

            return serializedProperties.ToString().Trim().TrimEnd(delimiter);
        }

        /// <summary>
        ///     Builds the foreign-key constraint name from the principal role, prefixing <c>FK_</c> when needed.
        /// </summary>
        private static string WriteFkConstraintName(ReferentialConstraint constraint)
        {
            var name = constraint.FromRole.DeclaringType.Name;
            if (false == name.StartsWith("FK_", StringComparison.InvariantCultureIgnoreCase))
            {
                return "FK_" + name;
            }

            return name;
        }

        /// <summary>
        ///     Returns <c>IDENTITY(1,1)</c> for integer or decimal identity columns, otherwise an empty string.
        /// </summary>
        private static string WriteIdentity(EdmProperty property, Version targetVersion)
        {
            if (property.GetStoreGeneratedPatternValue(targetVersion, DataSpace.SSpace) == StoreGeneratedPattern.Identity
                && IsIntegerOrDecimalType(property))
            {
                return "IDENTITY(1,1)";
            }

            return String.Empty;
        }

        /// <summary>
        ///     Returns <c>NULL</c> or <c>NOT NULL</c> for a column definition.
        /// </summary>
        private static string WriteNullable(bool isNull)
        {
            return isNull ? "NULL" : "NOT NULL";
        }

        #endregion
    }
}
