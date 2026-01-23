// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    using System;
    using System.Data;
    using System.Globalization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class RelationshipDetailsRowTests
    {
        [TestMethod]
        public void Table_returns_owning_table()
        {
            var relationshipDetailsCollection = new RelationshipDetailsCollection();
            Assert.Same(relationshipDetailsCollection, relationshipDetailsCollection.NewRow().Table);
        }

        [TestMethod]
        public void PKCatalog_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkCatalog"] = "catalog";
            Assert.Equal("catalog", ((RelationshipDetailsRow)row).PKCatalog);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKCatalogNull(.Should().BeTrue());
            row["PkCatalog"] = DBNull.Value;
            row.IsPKCatalogNull(.Should().BeTrue());
        }

        [TestMethod]
        public void PKCatalog_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkCatalog",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.PKCatalog).Message);
        }

        [TestMethod]
        public void PKSchema_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkSchema"] = "schema";
            Assert.Equal("schema", ((RelationshipDetailsRow)row).PKSchema);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKSchemaNull(.Should().BeTrue());
            row["PkSchema"] = DBNull.Value;
            row.IsPKSchemaNull(.Should().BeTrue());
        }

        [TestMethod]
        public void PKSchema_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkSchema",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.PKSchema).Message);
        }

        [TestMethod]
        public void PKTable_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkTable"] = "table";
            Assert.Equal("table", ((RelationshipDetailsRow)row).PKTable);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKTableNull(.Should().BeTrue());
            row["PkTable"] = DBNull.Value;
            row.IsPKTableNull(.Should().BeTrue());
        }

        [TestMethod]
        public void PKTable_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkTable",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.PKTable).Message);
        }

        [TestMethod]
        public void PKColumn_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["PkColumn"] = "column";
            Assert.Equal("column", ((RelationshipDetailsRow)row).PKColumn);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsPKColumnNull(.Should().BeTrue());
            row["PkColumn"] = DBNull.Value;
            row.IsPKColumnNull(.Should().BeTrue());
        }

        [TestMethod]
        public void PKColumn_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "PkColumn",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.PKColumn).Message);
        }

        [TestMethod]
        public void FKCatalog_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkCatalog"] = "fk-catalog";
            Assert.Equal("fk-catalog", ((RelationshipDetailsRow)row).FKCatalog);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKCatalogNull(.Should().BeTrue());
            row["FkCatalog"] = DBNull.Value;
            row.IsFKCatalogNull(.Should().BeTrue());
        }

        [TestMethod]
        public void FKCatalog_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkCatalog",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.FKCatalog).Message);
        }

        [TestMethod]
        public void FKSchema_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkSchema"] = "fk-schema";
            Assert.Equal("fk-schema", ((RelationshipDetailsRow)row).FKSchema);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKSchemaNull(.Should().BeTrue());
            row["FkSchema"] = DBNull.Value;
            row.IsFKSchemaNull(.Should().BeTrue());
        }

        [TestMethod]
        public void FKSchema_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkSchema",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.FKSchema).Message);
        }

        [TestMethod]
        public void FKTable_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkTable"] = "fk-table";
            Assert.Equal("fk-table", ((RelationshipDetailsRow)row).FKTable);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKTableNull(.Should().BeTrue());
            row["FkTable"] = DBNull.Value;
            row.IsFKTableNull(.Should().BeTrue());
        }

        [TestMethod]
        public void FKTable_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkTable",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.FKTable).Message);
        }

        [TestMethod]
        public void FKColumn_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["FkColumn"] = "fk-column";
            Assert.Equal("fk-column", ((RelationshipDetailsRow)row).FKColumn);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsFKColumnNull(.Should().BeTrue());
            row["FkColumn"] = DBNull.Value;
            row.IsFKColumnNull(.Should().BeTrue());
        }

        [TestMethod]
        public void FKColumn_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "FkColumn",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.FKColumn).Message);
        }

        [TestMethod]
        public void Ordinal_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["Ordinal"] = 42;
            Assert.Equal(42, ((RelationshipDetailsRow)row).Ordinal);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsOrdinalNull(.Should().BeTrue());
            row["Ordinal"] = DBNull.Value;
            row.IsOrdinalNull(.Should().BeTrue());
        }

        [TestMethod]
        public void Ordinal_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "Ordinal",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.Ordinal).Message);
        }

        [TestMethod]
        public void RelationshipName_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["RelationshipName"] = "relationship";
            Assert.Equal("relationship", ((RelationshipDetailsRow)row).RelationshipName);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();
            row.IsRelationshipNameNull(.Should().BeTrue());
            row["RelationshipName"] = DBNull.Value;
            row.IsRelationshipNameNull(.Should().BeTrue());
        }

        [TestMethod]
        public void RelationshipName_throws_StrongTypingException_for_null_vale()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "RelationshipName",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.RelationshipName).Message);
        }

        [TestMethod]
        public void RelationshipId_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["RelationshipId"] = "relationship";
            Assert.Equal("relationship", ((RelationshipDetailsRow)row).RelationshipId);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "RelationshipId",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.RelationshipId).Message);
        }

        [TestMethod]
        public void RelationshipIsCascadeDelete_getter_returns_value_set_with_indexer()
        {
            var row = new RelationshipDetailsCollection().NewRow();
            row["IsCascadeDelete"] = true;
            Assert.Equal(true, ((RelationshipDetailsRow)row).RelationshipIsCascadeDelete);
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
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            Assert.Equal(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources_VersioningFacade.StronglyTypedAccessToNullValue,
                    "IsCascadeDelete",
                    "RelationshipDetails"),
                Assert.Throws<StrongTypingException>(() => row.RelationshipIsCascadeDelete).Message);
        }

        [TestMethod]
        public void GetMostQualifiedPrimaryKey_returns_expected_result()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            row["PkTable"] = "table";
            Assert.Equal("table", row.GetMostQualifiedPrimaryKey());

            row["PkSchema"] = "schema";
            Assert.Equal("schema.table", row.GetMostQualifiedPrimaryKey());

            row["PkCatalog"] = "catalog";
            Assert.Equal("catalog.schema.table", row.GetMostQualifiedPrimaryKey());
        }

        [TestMethod]
        public void GetMostQualifiedForeignKey_returns_expected_result()
        {
            var row = (RelationshipDetailsRow)new RelationshipDetailsCollection().NewRow();

            row["FkTable"] = "table";
            Assert.Equal("table", row.GetMostQualifiedForeignKey());

            row["FkSchema"] = "schema";
            Assert.Equal("schema.table", row.GetMostQualifiedForeignKey());

            row["FkCatalog"] = "catalog";
            Assert.Equal("catalog.schema.table", row.GetMostQualifiedForeignKey());
        }
    }
}
