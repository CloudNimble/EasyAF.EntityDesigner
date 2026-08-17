// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class TableDetailsCollectionTests
    {
        [TestMethod]
        public void Verify_TableDetailsCollection_columns()
        {
            TableDetailsCollection tableDetailsCollection = new TableDetailsCollection();
            tableDetailsCollection.Columns.Count.Should().Be(14);
            VerifyColumn(tableDetailsCollection.CatalogColumn, "CatalogName", typeof(string));
            VerifyColumn(tableDetailsCollection.SchemaColumn, "SchemaName", typeof(string));
            VerifyColumn(tableDetailsCollection.TableNameColumn, "TableName", typeof(string));
            VerifyColumn(tableDetailsCollection.ColumnNameColumn, "ColumnName", typeof(string));
            VerifyColumn(tableDetailsCollection.IsNullableColumn, "IsNullable", typeof(bool));
            VerifyColumn(tableDetailsCollection.DataTypeColumn, "DataType", typeof(string));
            VerifyColumn(tableDetailsCollection.MaximumLengthColumn, "MaximumLength", typeof(int));
            VerifyColumn(tableDetailsCollection.PrecisionColumn, "Precision", typeof(int));
            VerifyColumn(tableDetailsCollection.DateTimePrecisionColumn, "DateTimePrecision", typeof(int));
            VerifyColumn(tableDetailsCollection.ScaleColumn, "Scale", typeof(int));
            VerifyColumn(tableDetailsCollection.IsIdentityColumn, "IsIdentity", typeof(bool));
            VerifyColumn(tableDetailsCollection.IsServerGeneratedColumn, "IsServerGenerated", typeof(bool));
            VerifyColumn(tableDetailsCollection.IsPrimaryKeyColumn, "IsPrimaryKey", typeof(bool));
            VerifyColumn(
                tableDetailsCollection.Columns.OfType<DataColumn>().Single(c => c.ColumnName == "Ordinal"),
                "Ordinal",
                typeof(int));
        }

        private void VerifyColumn(DataColumn dataColumn, string name, Type type)
        {
            dataColumn.ColumnName.Should().Be(name);
            dataColumn.DataType.Should().BeSameAs(type);
        }
    }
}
