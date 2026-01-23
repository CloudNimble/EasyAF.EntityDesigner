// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    using System;
    using System.Data;
    using System.Globalization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class TableDetailsRowTests
    {
        [TestMethod]
        public void Table_returns_owning_table()
        {
            var tableDetailsCollection = new TableDetailsCollection();
            Assert.Same(tableDetailsCollection, tableDetailsCollection.NewRow().Table);
        }

        [TestMethod]
        public void CatalogName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["CatalogName"] = "catalog";
            Assert.Equal("catalog", ((TableDetailsRow)row).Catalog);
        }

        [TestMethod]
        public void CatalogName_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).Catalog = "catalog";
            row["CatalogName"].Should().Be("catalog");
        }

        [TestMethod]
        public void CatalogName_IsDbNull_returns_true_for_null_CatalogName_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsCatalogNull(.Should().BeTrue());
            row["CatalogName"] = DBNull.Value;
            row.IsCatalogNull(.Should().BeTrue());
        }

        [TestMethod]
        public void CatalogName_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "CatalogName",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.Catalog).Message);
        }

        [TestMethod]
        public void SchemaName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["SchemaName"] = "schema";
            Assert.Equal("schema", ((TableDetailsRow)row).Schema);
        }

        [TestMethod]
        public void SchemaName_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).Schema = "schema";
            row["SchemaName"].Should().Be("schema");
        }

        [TestMethod]
        public void SchemaName_IsDbNull_returns_true_for_null_SchemaName_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsSchemaNull(.Should().BeTrue());
            row["SchemaName"] = DBNull.Value;
            row.IsSchemaNull(.Should().BeTrue());
        }

        [TestMethod]
        public void SchemaName_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "SchemaName",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.Schema).Message);
        }

        [TestMethod]
        public void TableName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["TableName"] = "table";
            Assert.Equal("table", ((TableDetailsRow)row).TableName);
        }

        [TestMethod]
        public void TableName_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).TableName = "table";
            row["TableName"].Should().Be("table");
        }

        [TestMethod]
        public void TableName_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "TableName",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.TableName).Message);
        }

        [TestMethod]
        public void ColumnName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["ColumnName"] = "column";
            Assert.Equal("column", ((TableDetailsRow)row).ColumnName);
        }

        [TestMethod]
        public void ColumnName_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).ColumnName = "column";
            row["ColumnName"].Should().Be("column");
        }

        [TestMethod]
        public void ColumnName_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "ColumnName",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.ColumnName).Message);
        }

        [TestMethod]
        public void IsNullable_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsNullable"] = true;
            ((TableDetailsRow.Should().BeTrue()row).IsNullable);
        }

        [TestMethod]
        public void IsNullable_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).IsNullable = true;
            (bool.Should().BeTrue()row["IsNullable"]);
        }

        [TestMethod]
        public void IsNullable_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsNullable",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.IsNullable).Message);
        }

        [TestMethod]
        public void DataType_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["DataType"] = "myType";
            Assert.Equal("myType", ((TableDetailsRow)row).DataType);
        }

        [TestMethod]
        public void DataType_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).DataType = "myType";
            row["DataType"].Should().Be("myType");
        }

        [TestMethod]
        public void DataType_IsDbNull_returns_true_for_null_DataType_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsDataTypeNull(.Should().BeTrue());
            row["DataType"] = DBNull.Value;
            row.IsDataTypeNull(.Should().BeTrue());
        }

        [TestMethod]
        public void DataType_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "DataType",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.DataType).Message);
        }

        [TestMethod]
        public void MaximumLength_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["MaximumLength"] = 42;
            Assert.Equal(42, ((TableDetailsRow)row).MaximumLength);
        }

        [TestMethod]
        public void MaximumLength_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).MaximumLength = 42;
            row["MaximumLength"].Should().Be(42);
        }

        [TestMethod]
        public void MaximumLength_IsDbNull_returns_true_for_null_MaximumLength_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsMaximumLengthNull(.Should().BeTrue());
            row["MaximumLength"] = DBNull.Value;
            row.IsMaximumLengthNull(.Should().BeTrue());
        }

        [TestMethod]
        public void MaximumLength_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "MaximumLength",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.MaximumLength).Message);
        }

        [TestMethod]
        public void DateTimePrecision_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["DateTimePrecision"] = 18;
            Assert.Equal(18, ((TableDetailsRow)row).DateTimePrecision);
        }

        [TestMethod]
        public void DateTimePrecision_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).DateTimePrecision = 18;
            row["DateTimePrecision"].Should().Be(18);
        }

        [TestMethod]
        public void DateTimePrecision_IsDbNull_returns_true_for_null_DateTimePrecision_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsDateTimePrecisionNull(.Should().BeTrue());
            row["DateTimePrecision"] = DBNull.Value;
            row.IsDateTimePrecisionNull(.Should().BeTrue());
        }

        [TestMethod]
        public void DateTimePrecision_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "DateTimePrecision",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.DateTimePrecision).Message);
        }

        [TestMethod]
        public void Precision_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["Precision"] = 18;
            Assert.Equal(18, ((TableDetailsRow)row).Precision);
        }

        [TestMethod]
        public void Precision_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).Precision = 18;
            row["Precision"].Should().Be(18);
        }

        [TestMethod]
        public void Precision_IsDbNull_returns_true_for_null_Precision_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsPrecisionNull(.Should().BeTrue());
            row["Precision"] = DBNull.Value;
            row.IsPrecisionNull(.Should().BeTrue());
        }

        [TestMethod]
        public void Precision_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "Precision",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.Precision).Message);
        }

        [TestMethod]
        public void Scale_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["Scale"] = 3;
            Assert.Equal(3, ((TableDetailsRow)row).Scale);
        }

        [TestMethod]
        public void Scale_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).Scale = 3;
            row["Scale"].Should().Be(3);
        }

        [TestMethod]
        public void Scale_IsDbNull_returns_true_for_null_Scale_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsScaleNull(.Should().BeTrue());
            row["Scale"] = DBNull.Value;
            row.IsScaleNull(.Should().BeTrue());
        }

        [TestMethod]
        public void Scale_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "Scale",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.Scale).Message);
        }

        [TestMethod]
        public void IsIdentity_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsIdentity"] = true;
            Assert.Equal(true, ((TableDetailsRow)row).IsIdentity);
        }

        [TestMethod]
        public void IsIdentity_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).IsIdentity = true;
            row["IsIdentity"].Should().Be(true);
        }

        [TestMethod]
        public void IsIdentity_IsDbNull_returns_true_for_null_IsIdentity_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsIsIdentityNull(.Should().BeTrue());
            row["IsIdentity"] = DBNull.Value;
            row.IsIsIdentityNull(.Should().BeTrue());
        }

        [TestMethod]
        public void IsIdentity_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsIdentity",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.IsIdentity).Message);
        }

        [TestMethod]
        public void IsServerGenerated_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsServerGenerated"] = true;
            Assert.Equal(true, ((TableDetailsRow)row).IsServerGenerated);
        }

        [TestMethod]
        public void IsServerGenerated_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).IsServerGenerated = true;
            row["IsServerGenerated"].Should().Be(true);
        }

        [TestMethod]
        public void IsServerGenerated_IsDbNull_returns_true_for_null_IsServerGenerated_value()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsIsServerGeneratedNull(.Should().BeTrue());
            row["IsServerGenerated"] = DBNull.Value;
            row.IsIsServerGeneratedNull(.Should().BeTrue());
        }

        [TestMethod]
        public void IsServerGenerated_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsServerGenerated",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.IsServerGenerated).Message);
        }

        [TestMethod]
        public void IsPrimaryKey_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsPrimaryKey"] = true;
            Assert.Equal(true, ((TableDetailsRow)row).IsPrimaryKey);
        }

        [TestMethod]
        public void IsPrimaryKey_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).IsPrimaryKey = true;
            row["IsPrimaryKey"].Should().Be(true);
        }

        [TestMethod]
        public void IsPrimaryKey_throws_StrongTypingException_for_null_vale()
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsPrimaryKey",
                    "TableDetails"),
                Assert.Throws<StrongTypingException>(() => row.IsPrimaryKey).Message);
        }

        [TestMethod]
        public void GetMostQualifiedTableName_uses_available_catalog_schema_table()
        {
            Assert.Equal(
                "catalog.schema.table",
                CreateTableDetailsRow("catalog", "schema", "table").GetMostQualifiedTableName());
            Assert.Equal("schema.table", CreateTableDetailsRow(null, "schema", "table").GetMostQualifiedTableName());
            Assert.Equal("catalog.table", CreateTableDetailsRow("catalog", null, "table").GetMostQualifiedTableName());
            Assert.Equal("table", CreateTableDetailsRow(null, null, "table").GetMostQualifiedTableName());
        }

        private TableDetailsRow CreateTableDetailsRow(string catalog, string schema, string table)
        {
            var row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.Catalog = catalog;
            row.Schema = schema;
            row.TableName = table;

            return row;
        }
    }
}
