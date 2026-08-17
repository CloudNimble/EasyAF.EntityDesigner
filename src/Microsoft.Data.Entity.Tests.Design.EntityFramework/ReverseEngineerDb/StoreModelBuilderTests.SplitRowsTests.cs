// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        [TestMethod]
        public void TableDetails_SplitRows_returns_empty_list_for_empty_input_rows()
        {
            StoreModelBuilder.SplitRows(new TableDetailsRow[0]).Should().BeEmpty();
        }

        [TestMethod]
        public void TableDetails_SplitRows_returns_grouped_row_details_for_multiple_input_tables_details()
        {
            var inputTableDetailsRows =
                new[]
                    {
                        CreateRow("catalog", "dbo", "Customer", "Id", 0, false, "int", isPrimaryKey: true),
                        CreateRow("catalog", "dbo", "Order", "Id", 0, false, "int", isPrimaryKey: true),
                        CreateRow("catalog", "dbo", "Order", "Date", 1, false, "datetime", isPrimaryKey: false),
                        CreateRow("catalog", "dbo", "OrderLine", "Id", 0, false, "int", isPrimaryKey: true),
                        CreateRow("catalog", "dbo", "Customer", "Name", 1, false, "nvarchar", isPrimaryKey: false),
                    };

            var splitRows = StoreModelBuilder.SplitRows(inputTableDetailsRows);

            splitRows.Count.Should().Be(3);
            splitRows[0].All(r => r.GetMostQualifiedTableName() == "catalog.dbo.Customer").Should().BeTrue();
            splitRows[1].All(r => r.GetMostQualifiedTableName() == "catalog.dbo.Order").Should().BeTrue();
            splitRows[2].All(r => r.GetMostQualifiedTableName() == "catalog.dbo.OrderLine").Should().BeTrue();
        }

        [TestMethod]
        public void FunctionDetails_SplitRows_returns_empty_list_for_empty_input_rows()
        {
            StoreModelBuilder.SplitRows(new FunctionDetailsRowView[0]).Should().BeEmpty();
        }

        [TestMethod]
        public void FunctionDetails_SplitRows_returns_grouped_row_details_for_multiple_input_tables_details()
        {
            var functionDetailsRows =
                new[]
                    {
                        CreateFunctionDetailsRow("catalog", "dbo", "function"),
                        CreateFunctionDetailsRow("catalog", "dbo", "function"),
                        CreateFunctionDetailsRow("catalog", "dbo", "function1"),
                        CreateFunctionDetailsRow("catalog", "dbo", "function"),
                        CreateFunctionDetailsRow(schema: "sch1", functionName: "function")
                    };

            var splitRows = StoreModelBuilder.SplitRows(functionDetailsRows);

            splitRows.Count.Should().Be(3);
            splitRows[0].All(r => r.GetMostQualifiedFunctionName() == "catalog.dbo.function").Should().BeTrue();
            splitRows[1].All(r => r.GetMostQualifiedFunctionName() == "catalog.dbo.function1").Should().BeTrue();
            splitRows[2].All(r => r.GetMostQualifiedFunctionName() == "sch1.function").Should().BeTrue();
        }
    }
}
