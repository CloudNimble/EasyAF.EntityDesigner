// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.EntityClient;
using System.Text;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class EntityStoreSchemaQueryGeneratorTests
    {
        private static ParameterCollectionBuilder CreateParameters(bool optimized = false)
        {
            return new ParameterCollectionBuilder(new EntityCommand().Parameters, optimized);
        }

        [TestMethod]
        public void AppendComparison_does_not_create_comparison_fragment_or_corresponding_parameter_for_non_value()
        {
            EntityStoreSchemaQueryGenerator.AppendComparison(
                new StringBuilder(),
                string.Empty,
                string.Empty,
                /* value */ null,
                CreateParameters()).ToString().Should().BeEmpty();
        }

        [TestMethod]
        public void AppendComparison_creates_comparison_fragment_and_corresponding_parameter_for_non_null_value()
        {
            var parameters = CreateParameters();

            EntityStoreSchemaQueryGenerator.AppendComparison(
                new StringBuilder(),
                "alias",
                "propertyName",
                "Value",
                parameters).ToString().Should().Be("alias.propertyName LIKE @p0");

            parameters.Count.Should().Be(1);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("Value");
        }

        [TestMethod]
        public void AppendComparison_creates_parameters_and_adds_AND_for_multiple_comparisons()
        {
            var parameters = CreateParameters();

            var filterBuilder =
                EntityStoreSchemaQueryGenerator.AppendComparison(
                    new StringBuilder(),
                    "alias1",
                    "propertyName1",
                    "Value1",
                    parameters);

            EntityStoreSchemaQueryGenerator.AppendComparison(
                filterBuilder,
                "alias2",
                "propertyName2",
                "Value2",
                parameters);

            filterBuilder.ToString().Should().Be("alias1.propertyName1 LIKE @p0 AND alias2.propertyName2 LIKE @p1");

            parameters.Count.Should().Be(2);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("Value1");
            parameters["p1"].Value.Should().Be("Value2");
        }

        [TestMethod]
        public void AppendFilterEntry_creates_filter_for_catalog_schema_and_name_if_all_specified()
        {
            var parameters = CreateParameters();

            EntityStoreSchemaQueryGenerator.AppendFilterEntry(
                new StringBuilder(),
                "alias",
                new EntityStoreSchemaFilterEntry("catalog", "schema", "name"),
                parameters).ToString().Should().Be("(alias.CatalogName LIKE @p0 AND alias.SchemaName LIKE @p1 AND alias.Name LIKE @p2)");

            parameters.Count.Should().Be(3);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("catalog");
            parameters["p1"].Value.Should().Be("schema");
            parameters["p2"].Value.Should().Be("name");
        }

        [TestMethod]
        public void AppendFilterEntry_does_not_create_comparison_for_missing_catalog_schema_or_name_if_any_specified()
        {
            EntityStoreSchemaQueryGenerator.AppendFilterEntry(
                new StringBuilder(),
                "alias",
                new EntityStoreSchemaFilterEntry(null, "schema", "name"),
                CreateParameters()).ToString().Should().Be("(alias.SchemaName LIKE @p0 AND alias.Name LIKE @p1)");

            EntityStoreSchemaQueryGenerator.AppendFilterEntry(
                new StringBuilder(),
                "alias",
                new EntityStoreSchemaFilterEntry("catalog", null, "name"),
                CreateParameters()).ToString().Should().Be("(alias.CatalogName LIKE @p0 AND alias.Name LIKE @p1)");

            EntityStoreSchemaQueryGenerator.AppendFilterEntry(
                new StringBuilder(),
                "alias",
                new EntityStoreSchemaFilterEntry("catalog", "schema", null),
                CreateParameters()).ToString().Should().Be("(alias.CatalogName LIKE @p0 AND alias.SchemaName LIKE @p1)");
        }

        [TestMethod]
        public void AppendFilterEntry_uses_wildcard_parameter_value_if_schema_catalog_and_name_are_null()
        {
            var parameters = CreateParameters();

            EntityStoreSchemaQueryGenerator.AppendFilterEntry(
                new StringBuilder(),
                "alias",
                new EntityStoreSchemaFilterEntry(null, null, null),
                parameters).ToString().Should().Be("(alias.Name LIKE @p0)");

            parameters.Count.Should().Be(1);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("%");
        }

        [TestMethod]
        public void AppendFilterEntry_uses_OR_to_connect_multiple_filters()
        {
            var parameters = CreateParameters();
            StringBuilder filterBuilder = new StringBuilder();

            EntityStoreSchemaQueryGenerator.AppendFilterEntry(
                filterBuilder,
                "alias",
                new EntityStoreSchemaFilterEntry(null, null, null),
                parameters);

            EntityStoreSchemaQueryGenerator.AppendFilterEntry(
                filterBuilder,
                "alias",
                new EntityStoreSchemaFilterEntry("catalog", "schema", null),
                parameters);

            filterBuilder.ToString().Should().Be("(alias.Name LIKE @p0) OR (alias.CatalogName LIKE @p1 AND alias.SchemaName LIKE @p2)");

            parameters.Count.Should().Be(3);
            parameters[0].ParameterName.Should().Be("p0");
            parameters[1].ParameterName.Should().Be("p1");
            parameters["p0"].Value.Should().Be("%");
            parameters["p1"].Value.Should().Be("catalog");
            parameters["p2"].Value.Should().Be("schema");
        }

        [TestMethod]
        public void Where_clause_not_created_for_empty_filter_aliases()
        {
            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null, null, null,
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Allow)
                    },
                filterAliases: new string[0])
                .CreateWhereClause(CreateParameters()).ToString().Should().Be(string.Empty);
        }

        [TestMethod]
        public void Where_clause_not_created_for_empty_filters()
        {
            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new EntityStoreSchemaFilterEntry[0],
                new[] { "alias" })
                .CreateWhereClause(CreateParameters()).ToString().Should().Be(string.Empty);
        }

        [TestMethod]
        public void Where_clause_created_for_single_Allow_filter()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            "catalog",
                            "schema",
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Allow)
                    },
                new[] { "alias" })
                .CreateWhereClause(parameters).ToString().Should().Be("((alias.CatalogName LIKE @p0 AND alias.SchemaName LIKE @p1 AND alias.Name LIKE @p2))");

            parameters.Count.Should().Be(3);
            parameters[0].ParameterName.Should().Be("p0");
            parameters[1].ParameterName.Should().Be("p1");
            parameters["p0"].Value.Should().Be("catalog");
            parameters["p1"].Value.Should().Be("schema");
            parameters["p2"].Value.Should().Be("name");
        }

        [TestMethod]
        public void Where_clause_uses_AND_to_connect_multiple_aliases_and_Allow_filter()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Allow),
                    },
                new[] { "alias1", "alias2" })
                .CreateWhereClause(parameters).ToString().Should().Be("((alias1.Name LIKE @p0))\r\nAND\r\n((alias2.Name LIKE @p1))");

            parameters.Count.Should().Be(2);
            parameters[0].ParameterName.Should().Be("p0");
            parameters[1].ParameterName.Should().Be("p1");
            parameters["p0"].Value.Should().Be("name");
            parameters["p1"].Value.Should().Be("name");
        }

        [TestMethod]
        public void Where_clause_uses_AND_to_connect_multiple_aliases_and_Allow_filter_and_optimizes_parameters()
        {
            var parameters = CreateParameters(optimized: true);

            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Allow),
                    },
                new[] { "alias1", "alias2" })
                .CreateWhereClause(parameters).ToString().Should().Be("((alias1.Name LIKE @p0))\r\nAND\r\n((alias2.Name LIKE @p0))");

            parameters.Count.Should().Be(1);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("name");
        }

        [TestMethod]
        public void Where_clause_created_for_single_Exclude_filter()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            "catalog",
                            "schema",
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Exclude)
                    },
                new[] { "alias" })
                .CreateWhereClause(parameters).ToString().Should().Be("NOT ((alias.CatalogName LIKE @p0 AND alias.SchemaName LIKE @p1 AND alias.Name LIKE @p2))");

            parameters.Count.Should().Be(3);
            parameters[0].ParameterName.Should().Be("p0");
            parameters[1].ParameterName.Should().Be("p1");
            parameters["p0"].Value.Should().Be("catalog");
            parameters["p1"].Value.Should().Be("schema");
            parameters["p2"].Value.Should().Be("name");
        }

        [TestMethod]
        public void Where_clause_uses_AND_to_connect_multiple_aliases_and_Exclude_filter()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Exclude),
                    },
                new[] { "alias1", "alias2" })
                .CreateWhereClause(parameters).ToString().Should().Be("NOT ((alias1.Name LIKE @p0))\r\nAND\r\nNOT ((alias2.Name LIKE @p1))");

            parameters.Count.Should().Be(2);
            parameters[0].ParameterName.Should().Be("p0");
            parameters[1].ParameterName.Should().Be("p1");
            parameters["p0"].Value.Should().Be("name");
            parameters["p1"].Value.Should().Be("name");
        }

        [TestMethod]
        public void Where_clause_uses_AND_to_connect_multiple_aliases_and_Exclude_filter_and_optimizes_parameters()
        {
            var parameters = CreateParameters(optimized: true);

            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Exclude),
                    },
                new[] { "alias1", "alias2" })
                .CreateWhereClause(parameters).ToString().Should().Be("NOT ((alias1.Name LIKE @p0))\r\nAND\r\nNOT ((alias2.Name LIKE @p0))");

            parameters.Count.Should().Be(1);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("name");
        }

        [TestMethod]
        public void Where_clause_created_when_Allow_and_Exclude_present()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                string.Empty,
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "nameAllowed",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Exclude),
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "nameExcluded",
                            EntityStoreSchemaFilterObjectTypes.Table,
                            EntityStoreSchemaFilterEffect.Exclude)
                    },
                new[] { "alias" })
                .CreateWhereClause(parameters).ToString().Should().Be("NOT ((alias.Name LIKE @p0) OR (alias.Name LIKE @p1))");

            parameters.Count.Should().Be(2);
            parameters[0].ParameterName.Should().Be("p0");
            parameters[1].ParameterName.Should().Be("p1");
            parameters["p0"].Value.Should().Be("nameAllowed");
            parameters["p1"].Value.Should().Be("nameExcluded");
        }

        [TestMethod]
        public void GenerateQuery_returns_base_query_if_no_orderby_clause_and_no_applicable_filters()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                "baseQuery",
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new EntityStoreSchemaFilterEntry[0],
                new string[0])
                .GenerateQuery(parameters).Should().Be("baseQuery");

            parameters.Count.Should().Be(0);
        }

        [TestMethod]
        public void GenerateQuery_filters_out_inapplicable_filters()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                "baseQuery",
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Table,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Function,
                            EntityStoreSchemaFilterEffect.Exclude),
                    },
                new string[0])
                .GenerateQuery(parameters).Should().Be("baseQuery");

            parameters.Count.Should().Be(0);
        }

        [TestMethod]
        public void GenerateQuery_appends_where_clause()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                "baseQuery",
                string.Empty,
                EntityStoreSchemaFilterObjectTypes.Function,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Function,
                            EntityStoreSchemaFilterEffect.Exclude),
                    },
                new[] { "alias" })
                .GenerateQuery(parameters).Should().Be("baseQuery\r\nWHERE\r\nNOT ((alias.Name LIKE @p0))");

            parameters.Count.Should().Be(1);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("name");
        }

        [TestMethod]
        public void GenerateQuery_appends_orderby_if_specified()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                "baseQuery",
                "orderBy",
                EntityStoreSchemaFilterObjectTypes.Table,
                new EntityStoreSchemaFilterEntry[0],
                new string[0])
                .GenerateQuery(parameters).Should().Be("baseQuery\r\norderBy");

            parameters.Count.Should().Be(0);
        }

        [TestMethod]
        public void GenerateQuery_appends_orderby_after_where_clause_if_both_are_present()
        {
            var parameters = CreateParameters();

            new EntityStoreSchemaQueryGenerator(
                "baseQuery",
                "orderby",
                EntityStoreSchemaFilterObjectTypes.Function,
                new[]
                    {
                        new EntityStoreSchemaFilterEntry(
                            null,
                            null,
                            "name",
                            EntityStoreSchemaFilterObjectTypes.Function,
                            EntityStoreSchemaFilterEffect.Exclude),
                    },
                new[] { "alias" })
                .GenerateQuery(parameters).Should().Be("baseQuery\r\nWHERE\r\nNOT ((alias.Name LIKE @p0))\r\norderby");

            parameters.Count.Should().Be(1);
            parameters[0].ParameterName.Should().Be("p0");
            parameters["p0"].Value.Should().Be("name");
        }
    }
}
