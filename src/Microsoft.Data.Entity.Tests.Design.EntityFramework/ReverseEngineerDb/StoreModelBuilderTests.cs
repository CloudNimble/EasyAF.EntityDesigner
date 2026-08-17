// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure.DependencyResolution;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    [TestClass]
    public partial class StoreModelBuilderTests
    {
        private static readonly IDbDependencyResolver DependencyResolver;

        static StoreModelBuilderTests()
        {
            Mock<IDbDependencyResolver> mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver.Setup(
                r => r.GetService(
                    It.Is<Type>(t => t == typeof(DbProviderServices)),
                    It.IsAny<string>())).Returns(Utils.SqlProviderServicesInstance);

            DependencyResolver = mockResolver.Object;
        }

        internal static TableDetailsRow CreateRow(
            string catalog = null, string schema = null, string table = null,
            string columnName = null, int? ordinal = null, bool isNullable = true,
            string dataType = null,
            int? maximumLength = null, int? precision = null,
            int? dateTimePrecision = null,
            int? scale = null, bool? isIdentity = null,
            bool? isServerGenerated = null, bool isPrimaryKey = false)
        {
            TableDetailsRow tableDetailsRow = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action<TableDetailsRow, string, object> setColumnValue =
                (row, column, value) =>
                    {
                        if (value != null)
                        {
                            row[column] = value;
                        }
                    };

            setColumnValue(tableDetailsRow, "CatalogName", catalog);
            setColumnValue(tableDetailsRow, "SchemaName", schema);
            setColumnValue(tableDetailsRow, "TableName", table);
            setColumnValue(tableDetailsRow, "ColumnName", columnName);
            setColumnValue(tableDetailsRow, "Ordinal", ordinal);
            setColumnValue(tableDetailsRow, "IsNullable", isNullable);
            setColumnValue(tableDetailsRow, "DataType", dataType);
            setColumnValue(tableDetailsRow, "MaximumLength", maximumLength);
            setColumnValue(tableDetailsRow, "Precision", precision);
            setColumnValue(tableDetailsRow, "DateTimePrecision", dateTimePrecision);
            setColumnValue(tableDetailsRow, "Scale", scale);
            setColumnValue(tableDetailsRow, "IsIdentity", isIdentity);
            setColumnValue(tableDetailsRow, "IsServerGenerated", isServerGenerated);
            setColumnValue(tableDetailsRow, "IsPrimaryKey", isPrimaryKey);

            return tableDetailsRow;
        }

        internal static RelationshipDetailsRow CreateRelationshipDetailsRow(
            string id, string name, int ordinal, bool isCascadeDelete,
            string pkCatalog, string pkSchema, string pkTable, string pkColumn,
            string fkCatalog, string fkSchema, string fkTable, string fkColumn)
        {
            RelationshipDetailsRow relationshipDetailsRow = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action<RelationshipDetailsRow, string, object> setColumnValue =
                (row, column, value) =>
                    {
                        if (value != null)
                        {
                            row[column] = value;
                        }
                    };

            setColumnValue(relationshipDetailsRow, "RelationshipId", id);
            setColumnValue(relationshipDetailsRow, "RelationshipName", name);
            setColumnValue(relationshipDetailsRow, "Ordinal", ordinal);
            setColumnValue(relationshipDetailsRow, "IsCascadeDelete", isCascadeDelete);
            setColumnValue(relationshipDetailsRow, "PkCatalog", pkCatalog);
            setColumnValue(relationshipDetailsRow, "PkSchema", pkSchema);
            setColumnValue(relationshipDetailsRow, "PkTable", pkTable);
            setColumnValue(relationshipDetailsRow, "PkColumn", pkColumn);
            setColumnValue(relationshipDetailsRow, "FkCatalog", fkCatalog);
            setColumnValue(relationshipDetailsRow, "FkSchema", fkSchema);
            setColumnValue(relationshipDetailsRow, "FkTable", fkTable);
            setColumnValue(relationshipDetailsRow, "FkColumn", fkColumn);

            return relationshipDetailsRow;
        }

        private static FunctionDetailsRowView CreateFunctionDetailsRow(
            string catalog = null,
            string schema = null,
            string functionName = null, string returnTypeName = null, bool isAggregate = false,
            bool isComposable = false, bool isBuiltIn = false, bool isNiladic = false, bool isTvf = false,
            string paramName = null, string paramTypeName = null, string parameterDirection = null)
        {
            return new FunctionDetailsV3RowView(
                new[]
                    {
                        (object)catalog ?? DBNull.Value,
                        (object)schema ?? DBNull.Value,
                        (object)functionName ?? DBNull.Value,
                        (object)returnTypeName ?? DBNull.Value,
                        isAggregate,
                        isComposable,
                        isBuiltIn,
                        isNiladic,
                        isTvf,
                        (object)paramName ?? DBNull.Value,
                        (object)paramTypeName ?? DBNull.Value,
                        (object)parameterDirection ?? DBNull.Value
                    });
        }

        internal static StoreModelBuilder CreateStoreModelBuilder(
            string providerInvariantName = "System.Data.SqlClient",
            string providerManifestToken = "2008",
            Version targetEntityFrameworkVersion = null,
            string namespaceName = "myModel",
            bool generateForeignKeyProperties = false)
        {
            return new StoreModelBuilder(
                providerInvariantName,
                providerManifestToken,
                targetEntityFrameworkVersion ?? EntityFrameworkVersion.Version3,
                namespaceName,
                DependencyResolver,
                generateForeignKeyProperties);
        }
    }
}
