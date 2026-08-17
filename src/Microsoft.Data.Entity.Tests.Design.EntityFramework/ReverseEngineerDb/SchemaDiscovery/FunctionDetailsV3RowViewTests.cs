// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class FunctionDetailsV3RowViewTests
    {
        [TestMethod]
        public void String_properties_exposed_correctly()
        {
            var row =
                new object[]
                    {
                        "catalog",
                        "schema",
                        "name",
                        "retType",
                        null,
                        null,
                        null,
                        null,
                        null,
                        "paramName",
                        "paramType",
                        "paramDirection"
                    };

            FunctionDetailsV3RowView view = new FunctionDetailsV3RowView(row);

            view.Catalog.Should().Be("catalog");
            view.Schema.Should().Be("schema");
            view.ProcedureName.Should().Be("name");
            view.ReturnType.Should().Be("retType");
            view.ParameterName.Should().Be("paramName");
            view.ParameterType.Should().Be("paramType");
            view.ProcParameterMode.Should().Be("paramDirection");
        }

        [TestMethod]
        public void IsAggregate_property_exposed_correctly()
        {
            const int isAggregateIndex = 4;

            var row = new object[12];
            FunctionDetailsV3RowView view = new FunctionDetailsV3RowView(row);

            row[isAggregateIndex] = false;
            view.IsIsAggregate.Should().BeFalse();
            row[isAggregateIndex] = true;
            view.IsIsAggregate.Should().BeTrue();
        }

        [TestMethod]
        public void IsComposable_property_exposed_correctly()
        {
            const int isComposableIndex = 5;
            var row = new object[12];
            FunctionDetailsV3RowView view = new FunctionDetailsV3RowView(row);

            row[isComposableIndex] = false;
            view.IsComposable.Should().BeFalse();
            row[isComposableIndex] = true;
            view.IsComposable.Should().BeTrue();
        }

        [TestMethod]
        public void IsBuiltIn_property_exposed_correctly()
        {
            const int isBuiltInIndex = 6;
            var row = new object[12];
            FunctionDetailsV3RowView view = new FunctionDetailsV3RowView(row);

            row[isBuiltInIndex] = false;
            view.IsBuiltIn.Should().BeFalse();
            row[isBuiltInIndex] = true;
            view.IsBuiltIn.Should().BeTrue();
        }

        [TestMethod]
        public void IsNiladic_property_exposed_correctly()
        {
            const int isNiladicIndex = 7;

            var row = new object[12];
            FunctionDetailsV3RowView view = new FunctionDetailsV3RowView(row);

            row[isNiladicIndex] = false;
            view.IsNiladic.Should().BeFalse();
            row[isNiladicIndex] = true;
            view.IsNiladic.Should().BeTrue();
        }

        [TestMethod]
        public void IsTvf_property_exposed_correctly()
        {
            const int isTvfIndex = 8;

            var row = new object[12];
            FunctionDetailsV3RowView view = new FunctionDetailsV3RowView(row);

            row[isTvfIndex] = false;
            view.IsTvf.Should().BeFalse();
            row[isTvfIndex] = true;
            view.IsTvf.Should().BeTrue();
        }

        [TestMethod]
        public void DbNull_converted_to_default_values()
        {
            FunctionDetailsV3RowView view =
                new FunctionDetailsV3RowView(Enumerable.Repeat(DBNull.Value, 12).ToArray());

            view.Catalog.Should().BeNull();
            view.Schema.Should().BeNull();
            view.ProcedureName.Should().BeNull();
            view.ReturnType.Should().BeNull();
            view.IsIsAggregate.Should().BeFalse();
            view.IsBuiltIn.Should().BeFalse();
            view.IsComposable.Should().BeFalse();
            view.IsNiladic.Should().BeFalse();
            view.IsTvf.Should().BeFalse();
            view.ParameterName.Should().BeNull();
            view.ParameterType.Should().BeNull();
            view.ProcParameterMode.Should().BeNull();
        }

        [TestMethod]
        public void IsParameterXXXNull_properties_return_true_for_DBNull_values()
        {
            FunctionDetailsV3RowView view =
                new FunctionDetailsV3RowView(Enumerable.Repeat(DBNull.Value, 12).ToArray());

            view.IsParameterNameNull.Should().BeTrue();
            view.IsParameterTypeNull.Should().BeTrue();
            view.IsParameterModeNull.Should().BeTrue();
        }

        [TestMethod]
        public void IsParameterXXXNull_properties_return_false_for_non_DBNull_values()
        {
            FunctionDetailsV3RowView view =
                new FunctionDetailsV3RowView(Enumerable.Repeat("test", 12).ToArray());

            view.IsParameterNameNull.Should().BeFalse();
            view.IsParameterTypeNull.Should().BeFalse();
            view.IsParameterModeNull.Should().BeFalse();
        }

        [TestMethod]
        public void GetMostQualifiedFunctionName_returns_correct_function_name()
        {
            GetMostQualifiedFunctionName(catalog: DBNull.Value, schema: DBNull.Value, procedureName: "function").Should().Be("function");
            GetMostQualifiedFunctionName(catalog: DBNull.Value, schema: "dbo", procedureName: "function").Should().Be("dbo.function");
            GetMostQualifiedFunctionName(catalog: "catalog", schema: DBNull.Value, procedureName: "function").Should().Be("catalog.function");
            GetMostQualifiedFunctionName(catalog: "catalog", schema: "dbo", procedureName: "function").Should().Be("catalog.dbo.function");
        }

        private static string GetMostQualifiedFunctionName(object catalog, object schema, object procedureName)
        {
            var row =
                new[]
                    {
                        catalog, schema, procedureName, null, null, null, null, null, null, null, null, null
                    };

            return new FunctionDetailsV3RowView(row).GetMostQualifiedFunctionName();
        }
    }
}
