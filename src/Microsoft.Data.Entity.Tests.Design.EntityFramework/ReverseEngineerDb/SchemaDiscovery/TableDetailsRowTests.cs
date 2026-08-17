// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class TableDetailsRowTests
    {
        [TestMethod]
        public void Table_returns_owning_table()
        {
            TableDetailsCollection tableDetailsCollection = new TableDetailsCollection();
            tableDetailsCollection.NewRow().Table.Should().BeSameAs(tableDetailsCollection);
        }

        [TestMethod]
        public void CatalogName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["CatalogName"] = "catalog";
            ((TableDetailsRow)row).Catalog.Should().Be("catalog");
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsCatalogNull().Should().BeTrue();
            row["CatalogName"] = DBNull.Value;
            row.IsCatalogNull().Should().BeTrue();
        }

        [TestMethod]
        public void CatalogName_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.Catalog; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "CatalogName",
                    "TableDetails"));
        }

        [TestMethod]
        public void SchemaName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["SchemaName"] = "schema";
            ((TableDetailsRow)row).Schema.Should().Be("schema");
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsSchemaNull().Should().BeTrue();
            row["SchemaName"] = DBNull.Value;
            row.IsSchemaNull().Should().BeTrue();
        }

        [TestMethod]
        public void SchemaName_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.Schema; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "SchemaName",
                    "TableDetails"));
        }

        [TestMethod]
        public void TableName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["TableName"] = "table";
            ((TableDetailsRow)row).TableName.Should().Be("table");
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.TableName; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "TableName",
                    "TableDetails"));
        }

        [TestMethod]
        public void ColumnName_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["ColumnName"] = "column";
            ((TableDetailsRow)row).ColumnName.Should().Be("column");
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.ColumnName; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "ColumnName",
                    "TableDetails"));
        }

        [TestMethod]
        public void IsNullable_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsNullable"] = true;
            ((TableDetailsRow)row).IsNullable.Should().BeTrue();
        }

        [TestMethod]
        public void IsNullable_setter_sets_value_in_uderlying_row()
        {
            var row = new TableDetailsCollection().NewRow();
            ((TableDetailsRow)row).IsNullable = true;
            ((bool)row["IsNullable"]).Should().BeTrue();
        }

        [TestMethod]
        public void IsNullable_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.IsNullable; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsNullable",
                    "TableDetails"));
        }

        [TestMethod]
        public void DataType_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["DataType"] = "myType";
            ((TableDetailsRow)row).DataType.Should().Be("myType");
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsDataTypeNull().Should().BeTrue();
            row["DataType"] = DBNull.Value;
            row.IsDataTypeNull().Should().BeTrue();
        }

        [TestMethod]
        public void DataType_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.DataType; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "DataType",
                    "TableDetails"));
        }

        [TestMethod]
        public void MaximumLength_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["MaximumLength"] = 42;
            ((TableDetailsRow)row).MaximumLength.Should().Be(42);
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsMaximumLengthNull().Should().BeTrue();
            row["MaximumLength"] = DBNull.Value;
            row.IsMaximumLengthNull().Should().BeTrue();
        }

        [TestMethod]
        public void MaximumLength_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.MaximumLength; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "MaximumLength",
                    "TableDetails"));
        }

        [TestMethod]
        public void DateTimePrecision_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["DateTimePrecision"] = 18;
            ((TableDetailsRow)row).DateTimePrecision.Should().Be(18);
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsDateTimePrecisionNull().Should().BeTrue();
            row["DateTimePrecision"] = DBNull.Value;
            row.IsDateTimePrecisionNull().Should().BeTrue();
        }

        [TestMethod]
        public void DateTimePrecision_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.DateTimePrecision; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "DateTimePrecision",
                    "TableDetails"));
        }

        [TestMethod]
        public void Precision_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["Precision"] = 18;
            ((TableDetailsRow)row).Precision.Should().Be(18);
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsPrecisionNull().Should().BeTrue();
            row["Precision"] = DBNull.Value;
            row.IsPrecisionNull().Should().BeTrue();
        }

        [TestMethod]
        public void Precision_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.Precision; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "Precision",
                    "TableDetails"));
        }

        [TestMethod]
        public void Scale_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["Scale"] = 3;
            ((TableDetailsRow)row).Scale.Should().Be(3);
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsScaleNull().Should().BeTrue();
            row["Scale"] = DBNull.Value;
            row.IsScaleNull().Should().BeTrue();
        }

        [TestMethod]
        public void Scale_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.Scale; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "Scale",
                    "TableDetails"));
        }

        [TestMethod]
        public void IsIdentity_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsIdentity"] = true;
            ((TableDetailsRow)row).IsIdentity.Should().Be(true);
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsIsIdentityNull().Should().BeTrue();
            row["IsIdentity"] = DBNull.Value;
            row.IsIsIdentityNull().Should().BeTrue();
        }

        [TestMethod]
        public void IsIdentity_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.IsIdentity; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsIdentity",
                    "TableDetails"));
        }

        [TestMethod]
        public void IsServerGenerated_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsServerGenerated"] = true;
            ((TableDetailsRow)row).IsServerGenerated.Should().Be(true);
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.IsIsServerGeneratedNull().Should().BeTrue();
            row["IsServerGenerated"] = DBNull.Value;
            row.IsIsServerGeneratedNull().Should().BeTrue();
        }

        [TestMethod]
        public void IsServerGenerated_throws_StrongTypingException_for_null_vale()
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.IsServerGenerated; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsServerGenerated",
                    "TableDetails"));
        }

        [TestMethod]
        public void IsPrimaryKey_getter_returns_value_set_with_indexer()
        {
            var row = new TableDetailsCollection().NewRow();
            row["IsPrimaryKey"] = true;
            ((TableDetailsRow)row).IsPrimaryKey.Should().Be(true);
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
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();

            Action act = () => { var _ = row.IsPrimaryKey; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsPrimaryKey",
                    "TableDetails"));
        }

        [TestMethod]
        public void GetMostQualifiedTableName_uses_available_catalog_schema_table()
        {
            CreateTableDetailsRow("catalog", "schema", "table").GetMostQualifiedTableName()
                .Should().Be("catalog.schema.table");
            CreateTableDetailsRow(null, "schema", "table").GetMostQualifiedTableName()
                .Should().Be("schema.table");
            CreateTableDetailsRow("catalog", null, "table").GetMostQualifiedTableName()
                .Should().Be("catalog.table");
            CreateTableDetailsRow(null, null, "table").GetMostQualifiedTableName()
                .Should().Be("table");
        }

        private TableDetailsRow CreateTableDetailsRow(string catalog, string schema, string table)
        {
            TableDetailsRow row = (TableDetailsRow)new TableDetailsCollection().NewRow();
            row.Catalog = catalog;
            row.Schema = schema;
            row.TableName = table;

            return row;
        }
    }
}
