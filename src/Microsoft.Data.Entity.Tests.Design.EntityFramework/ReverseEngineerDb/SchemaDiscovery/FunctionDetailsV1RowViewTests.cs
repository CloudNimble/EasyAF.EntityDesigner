// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class FunctionDetailsV1RowViewTests
    {
        [TestMethod]
        public void String_properties_exposed_correctly()
        {
            var row =
                new object[]
                    {
                        "schema",
                        "name",
                        "retType",
                        null,
                        null,
                        null,
                        null,
                        "paramName",
                        "paramType",
                        "paramDirection"
                    };

            FunctionDetailsV1RowView view = new FunctionDetailsV1RowView(row);

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
            const int isAggregateIndex = 3;

            var row = new object[10];
            FunctionDetailsV1RowView view = new FunctionDetailsV1RowView(row);

            row[isAggregateIndex] = false;
            view.IsIsAggregate.Should().BeFalse();
            row[isAggregateIndex] = true;
            view.IsIsAggregate.Should().BeTrue();
        }

        [TestMethod]
        public void IsComposable_property_exposed_correctly()
        {
            const int isComposableIndex = 4;
            var row = new object[10];
            FunctionDetailsV1RowView view = new FunctionDetailsV1RowView(row);

            row[isComposableIndex] = false;
            view.IsComposable.Should().BeFalse();
            row[isComposableIndex] = true;
            view.IsComposable.Should().BeTrue();
        }

        [TestMethod]
        public void IsBuiltIn_property_exposed_correctly()
        {
            const int isBuiltInIndex = 5;
            var row = new object[10];
            FunctionDetailsV1RowView view = new FunctionDetailsV1RowView(row);

            row[isBuiltInIndex] = false;
            view.IsBuiltIn.Should().BeFalse();
            row[isBuiltInIndex] = true;
            view.IsBuiltIn.Should().BeTrue();
        }

        [TestMethod]
        public void IsNiladic_property_exposed_correctly()
        {
            const int isNiladicIndex = 6;

            var row = new object[10];
            FunctionDetailsV1RowView view = new FunctionDetailsV1RowView(row);

            row[isNiladicIndex] = false;
            view.IsNiladic.Should().BeFalse();
            row[isNiladicIndex] = true;
            view.IsNiladic.Should().BeTrue();
        }

        [TestMethod]
        public void Catalog_and_IsTvf_return_default_values()
        {
            FunctionDetailsV1RowView view =
                new FunctionDetailsV1RowView(Enumerable.Repeat(new object(), 10).ToArray());

            view.Catalog.Should().BeNull();
            view.IsTvf.Should().BeFalse();
        }

        [TestMethod]
        public void DbNull_converted_to_default_values()
        {
            FunctionDetailsV1RowView view =
                new FunctionDetailsV1RowView(Enumerable.Repeat(DBNull.Value, 10).ToArray());

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
            FunctionDetailsV1RowView view =
                new FunctionDetailsV1RowView(Enumerable.Repeat(DBNull.Value, 10).ToArray());

            view.IsParameterNameNull.Should().BeTrue();
            view.IsParameterTypeNull.Should().BeTrue();
            view.IsParameterModeNull.Should().BeTrue();
        }

        [TestMethod]
        public void IsParameterXXXNull_properties_return_false_for_non_DBNull_values()
        {
            FunctionDetailsV1RowView view =
                new FunctionDetailsV1RowView(Enumerable.Repeat("test", 10).ToArray());

            view.IsParameterNameNull.Should().BeFalse();
            view.IsParameterTypeNull.Should().BeFalse();
            view.IsParameterModeNull.Should().BeFalse();
        }

        [TestMethod]
        public void TryGetParameterMode_returns_ParameterMode_corresponding_to_the_given_string_parameter_mode()
        {
            TryGetParameterMode_test_helper(DBNull.Value, false, (ParameterMode)(-1));
            TryGetParameterMode_test_helper(string.Empty, false, (ParameterMode)(-1));
            TryGetParameterMode_test_helper("foo", false, (ParameterMode)(-1));
            TryGetParameterMode_test_helper("IN", true, ParameterMode.In);
            TryGetParameterMode_test_helper("OUT", true, ParameterMode.Out);
            TryGetParameterMode_test_helper("INOUT", true, ParameterMode.InOut);
        }

        private static void TryGetParameterMode_test_helper(object paramDirection, bool successExpected, ParameterMode expectedMode)
        {
            var row =
                new[]
                    {
                        "schema",
                        "name",
                        "retType",
                        null,
                        null,
                        null,
                        null,
                        "paramName",
                        "paramType",
                        paramDirection
                    };

            var success = new FunctionDetailsV1RowView(row).TryGetParameterMode(out ParameterMode actualMode);
            success.Should().Be(successExpected);
            actualMode.Should().Be(expectedMode);
        }
    }
}
