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
    public class RelationshipDetailsRowTests
    {
        [TestMethod]
        public void Table_returns_owning_table()
        {
            RelationshipDetailsCollection relationshipDetailsCollection = new RelationshipDetailsCollection();
            relationshipDetailsCollection.NewRow().Table.Should().BeSameAs(relationshipDetailsCollection);
        }

        [TestMethod]
        public void PKCatalog_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkCatalog"] = "catalog";
            ((RelationshipDetailsRow)row).PKCatalog.Should().Be("catalog");
        }

        [TestMethod]
        public void PKCatalog_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).PKCatalog = "catalog";
            row["PkCatalog"].Should().Be("catalog");
        }

        [TestMethod]
        public void PKCatalog_IsDbNull_returns_true_for_null_PKCatalog_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKCatalogNull().Should().BeTrue();
            row["PkCatalog"] = DBNull.Value;
            row.IsPKCatalogNull().Should().BeTrue();
        }

        [TestMethod]
        public void PKCatalog_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.PKCatalog; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkCatalog",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void PKSchema_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkSchema"] = "schema";
            ((RelationshipDetailsRow)row).PKSchema.Should().Be("schema");
        }

        [TestMethod]
        public void PKSchema_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).PKSchema = "schema";
            row["PkSchema"].Should().Be("schema");
        }

        [TestMethod]
        public void PKSchema_IsDbNull_returns_true_for_null_PkSchema_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKSchemaNull().Should().BeTrue();
            row["PkSchema"] = DBNull.Value;
            row.IsPKSchemaNull().Should().BeTrue();
        }

        [TestMethod]
        public void PKSchema_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.PKSchema; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkSchema",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void PKTable_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkTable"] = "table";
            ((RelationshipDetailsRow)row).PKTable.Should().Be("table");
        }

        [TestMethod]
        public void PKTable_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).PKTable = "table";
            row["PkTable"].Should().Be("table");
        }

        [TestMethod]
        public void PKTable_IsDbNull_returns_true_for_null_PkTable_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKTableNull().Should().BeTrue();
            row["PkTable"] = DBNull.Value;
            row.IsPKTableNull().Should().BeTrue();
        }

        [TestMethod]
        public void PKTable_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.PKTable; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkTable",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void PKColumn_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkColumn"] = "column";
            ((RelationshipDetailsRow)row).PKColumn.Should().Be("column");
        }

        [TestMethod]
        public void PKColumn_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).PKColumn = "column";
            row["PkColumn"].Should().Be("column");
        }

        [TestMethod]
        public void PKColumn_IsDbNull_returns_true_for_null_PkColumn_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKColumnNull().Should().BeTrue();
            row["PkColumn"] = DBNull.Value;
            row.IsPKColumnNull().Should().BeTrue();
        }

        [TestMethod]
        public void PKColumn_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.PKColumn; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkColumn",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void FKCatalog_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkCatalog"] = "fk-catalog";
            ((RelationshipDetailsRow)row).FKCatalog.Should().Be("fk-catalog");
        }

        [TestMethod]
        public void FKCatalog_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).FKCatalog = "fk-catalog";
            row["FkCatalog"].Should().Be("fk-catalog");
        }

        [TestMethod]
        public void FKCatalog_IsDbNull_returns_true_for_null_FkCatalog_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKCatalogNull().Should().BeTrue();
            row["FkCatalog"] = DBNull.Value;
            row.IsFKCatalogNull().Should().BeTrue();
        }

        [TestMethod]
        public void FKCatalog_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.FKCatalog; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkCatalog",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void FKSchema_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkSchema"] = "fk-schema";
            ((RelationshipDetailsRow)row).FKSchema.Should().Be("fk-schema");
        }

        [TestMethod]
        public void FKSchema_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).FKSchema = "fk-schema";
            row["FkSchema"].Should().Be("fk-schema");
        }

        [TestMethod]
        public void FKSchema_IsDbNull_returns_true_for_null_FkSchema_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKSchemaNull().Should().BeTrue();
            row["FkSchema"] = DBNull.Value;
            row.IsFKSchemaNull().Should().BeTrue();
        }

        [TestMethod]
        public void FKSchema_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.FKSchema; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkSchema",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void FKTable_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkTable"] = "fk-table";
            ((RelationshipDetailsRow)row).FKTable.Should().Be("fk-table");
        }

        [TestMethod]
        public void FKTable_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).FKTable = "fk-table";
            row["FkTable"].Should().Be("fk-table");
        }

        [TestMethod]
        public void FKTable_IsDbNull_returns_true_for_null_FkTable_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKTableNull().Should().BeTrue();
            row["FkTable"] = DBNull.Value;
            row.IsFKTableNull().Should().BeTrue();
        }

        [TestMethod]
        public void FKTable_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.FKTable; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkTable",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void FKColumn_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkColumn"] = "fk-column";
            ((RelationshipDetailsRow)row).FKColumn.Should().Be("fk-column");
        }

        [TestMethod]
        public void FKColumn_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).FKColumn = "fk-column";
            row["FkColumn"].Should().Be("fk-column");
        }

        [TestMethod]
        public void FKColumn_IsDbNull_returns_true_for_null_FkColumn_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKColumnNull().Should().BeTrue();
            row["FkColumn"] = DBNull.Value;
            row.IsFKColumnNull().Should().BeTrue();
        }

        [TestMethod]
        public void FKColumn_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.FKColumn; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkColumn",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void Ordinal_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["Ordinal"] = 42;
            ((RelationshipDetailsRow)row).Ordinal.Should().Be(42);
        }

        [TestMethod]
        public void Ordinal_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).Ordinal = 42;
            row["Ordinal"].Should().Be(42);
        }

        [TestMethod]
        public void Ordinal_IsDbNull_returns_true_for_null_Ordinal_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsOrdinalNull().Should().BeTrue();
            row["Ordinal"] = DBNull.Value;
            row.IsOrdinalNull().Should().BeTrue();
        }

        [TestMethod]
        public void Ordinal_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.Ordinal; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "Ordinal",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void RelationshipName_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["RelationshipName"] = "relationship";
            ((RelationshipDetailsRow)row).RelationshipName.Should().Be("relationship");
        }

        [TestMethod]
        public void RelationshipName_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).RelationshipName = "relationship";
            row["RelationshipName"].Should().Be("relationship");
        }

        [TestMethod]
        public void RelationshipName_IsDbNull_returns_true_for_null_RelationshipName_value()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsRelationshipNameNull().Should().BeTrue();
            row["RelationshipName"] = DBNull.Value;
            row.IsRelationshipNameNull().Should().BeTrue();
        }

        [TestMethod]
        public void RelationshipName_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.RelationshipName; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "RelationshipName",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void RelationshipId_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["RelationshipId"] = "relationship";
            ((RelationshipDetailsRow)row).RelationshipId.Should().Be("relationship");
        }

        [TestMethod]
        public void RelationshipId_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).RelationshipId = "relationship";
            row["RelationshipId"].Should().Be("relationship");
        }

        [TestMethod]
        public void RelationshipId_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.RelationshipId; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "RelationshipId",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void RelationshipIsCascadeDelete_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["IsCascadeDelete"] = true;
            ((RelationshipDetailsRow)row).RelationshipIsCascadeDelete.Should().Be(true);
        }

        [TestMethod]
        public void RelationshipIsCascadeDelete_setter_sets_value_in_uderlying_row()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            ((RelationshipDetailsRow)row).RelationshipIsCascadeDelete = true;
            row["IsCascadeDelete"].Should().Be(true);
        }

        [TestMethod]
        public void RelationshipIsCascadeDelete_throws_StrongTypingException_for_null_vale()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Action act = () => { var _ = row.RelationshipIsCascadeDelete; };
            act.Should().Throw<StrongTypingException>()
                .WithMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsCascadeDelete",
                    "RelationshipDetails"));
        }

        [TestMethod]
        public void GetMostQualifiedPrimaryKey_returns_expected_result()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            row["PkTable"] = "table";
            row.GetMostQualifiedPrimaryKey().Should().Be("table");

            row["PkSchema"] = "schema";
            row.GetMostQualifiedPrimaryKey().Should().Be("schema.table");

            row["PkCatalog"] = "catalog";
            row.GetMostQualifiedPrimaryKey().Should().Be("catalog.schema.table");
        }

        [TestMethod]
        public void GetMostQualifiedForeignKey_returns_expected_result()
        {
            RelationshipDetailsRow row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            row["FkTable"] = "table";
            row.GetMostQualifiedForeignKey().Should().Be("table");

            row["FkSchema"] = "schema";
            row.GetMostQualifiedForeignKey().Should().Be("schema.table");

            row["FkCatalog"] = "catalog";
            row.GetMostQualifiedForeignKey().Should().Be("catalog.schema.table");
        }
    }
}
