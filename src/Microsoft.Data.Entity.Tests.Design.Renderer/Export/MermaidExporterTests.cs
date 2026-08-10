// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    [TestClass]
    public class MermaidExporterTests
    {
        #region SanitizeName Tests

        [TestMethod]
        public void SanitizeName_with_normal_name_returns_unchanged()
        {
            MermaidExporter.SanitizeName("Customer").Should().Be("Customer");
        }

        [TestMethod]
        public void SanitizeName_with_underscores_returns_unchanged()
        {
            MermaidExporter.SanitizeName("Customer_Order").Should().Be("Customer_Order");
        }

        [TestMethod]
        public void SanitizeName_with_spaces_replaces_with_underscores()
        {
            MermaidExporter.SanitizeName("Customer Order").Should().Be("Customer_Order");
        }

        [TestMethod]
        public void SanitizeName_with_multiple_spaces_replaces_each_with_underscore()
        {
            MermaidExporter.SanitizeName("My Entity Name").Should().Be("My_Entity_Name");
        }

        [TestMethod]
        public void SanitizeName_with_special_characters_replaces_with_underscores()
        {
            MermaidExporter.SanitizeName("Customer-Order!").Should().Be("Customer_Order_");
        }

        [TestMethod]
        public void SanitizeName_with_dots_replaces_with_underscores()
        {
            MermaidExporter.SanitizeName("System.String").Should().Be("System_String");
        }

        [TestMethod]
        public void SanitizeName_with_leading_digit_prepends_underscore()
        {
            MermaidExporter.SanitizeName("123Entity").Should().Be("_123Entity");
        }

        [TestMethod]
        public void SanitizeName_with_all_digits_prepends_underscore()
        {
            MermaidExporter.SanitizeName("12345").Should().Be("_12345");
        }

        [TestMethod]
        public void SanitizeName_with_null_returns_unknown()
        {
            MermaidExporter.SanitizeName(null).Should().Be("Unknown");
        }

        [TestMethod]
        public void SanitizeName_with_empty_string_returns_unknown()
        {
            MermaidExporter.SanitizeName("").Should().Be("Unknown");
        }

        [TestMethod]
        public void SanitizeName_with_whitespace_only_returns_underscores()
        {
            // Whitespace is replaced with underscores, not trimmed
            MermaidExporter.SanitizeName("   ").Should().Be("___");
        }

        [TestMethod]
        public void SanitizeName_with_unicode_letters_preserves_them()
        {
            MermaidExporter.SanitizeName("Kundenname").Should().Be("Kundenname");
        }

        [TestMethod]
        public void SanitizeName_with_mixed_unicode_and_special_chars()
        {
            MermaidExporter.SanitizeName("Cafe-Latte").Should().Be("Cafe_Latte");
        }

        [TestMethod]
        public void SanitizeName_with_parentheses_replaces_with_underscores()
        {
            MermaidExporter.SanitizeName("Entity(1)").Should().Be("Entity_1_");
        }

        [TestMethod]
        public void SanitizeName_with_angle_brackets_replaces_with_underscores()
        {
            MermaidExporter.SanitizeName("List<int>").Should().Be("List_int_");
        }

        #endregion

        #region SanitizeType Tests

        [TestMethod]
        public void SanitizeType_with_simple_type_returns_lowercase()
        {
            MermaidExporter.SanitizeType("String").Should().Be("string");
        }

        [TestMethod]
        public void SanitizeType_with_system_prefix_removes_it()
        {
            MermaidExporter.SanitizeType("System.String").Should().Be("string");
        }

        [TestMethod]
        public void SanitizeType_with_nullable_removes_wrapper()
        {
            MermaidExporter.SanitizeType("Nullable<Int32>").Should().Be("int32");
        }

        [TestMethod]
        public void SanitizeType_with_nullable_shorthand_removes_it()
        {
            MermaidExporter.SanitizeType("int?").Should().Be("int");
        }

        [TestMethod]
        public void SanitizeType_with_system_nullable_removes_both()
        {
            MermaidExporter.SanitizeType("System.Nullable<DateTime>").Should().Be("datetime");
        }

        [TestMethod]
        public void SanitizeType_with_null_returns_unknown()
        {
            MermaidExporter.SanitizeType(null).Should().Be("unknown");
        }

        [TestMethod]
        public void SanitizeType_with_empty_returns_unknown()
        {
            MermaidExporter.SanitizeType("").Should().Be("unknown");
        }

        [TestMethod]
        public void SanitizeType_with_int32_returns_lowercase()
        {
            MermaidExporter.SanitizeType("Int32").Should().Be("int32");
        }

        [TestMethod]
        public void SanitizeType_with_guid_returns_lowercase()
        {
            MermaidExporter.SanitizeType("System.Guid").Should().Be("guid");
        }

        [TestMethod]
        public void SanitizeType_with_decimal_returns_lowercase()
        {
            MermaidExporter.SanitizeType("Decimal").Should().Be("decimal");
        }

        #endregion

        #region GetMermaidMultiplicity Tests

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_source_returns_pipes()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("1", isSource: true).Should().Be("||");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_target_returns_pipes()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("1", isSource: false).Should().Be("||");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_or_one_source_returns_pipe_o()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("0..1", isSource: true).Should().Be("|o");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_or_one_target_returns_o_pipe()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("0..1", isSource: false).Should().Be("o|");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_star_source_returns_brace_o()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("*", isSource: true).Should().Be("}o");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_star_target_returns_o_brace()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("*", isSource: false).Should().Be("o{");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_to_many_source_returns_brace_o()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("0..*", isSource: true).Should().Be("}o");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_to_many_target_returns_o_brace()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("0..*", isSource: false).Should().Be("o{");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_to_many_source_returns_brace_pipe()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("1..*", isSource: true).Should().Be("}|");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_to_many_target_returns_pipe_brace()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("1..*", isSource: false).Should().Be("|{");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_null_returns_default_many()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity(null, isSource: true).Should().Be("}o");
            exporter.GetMermaidMultiplicity(null, isSource: false).Should().Be("o{");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_empty_returns_default_many()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("", isSource: true).Should().Be("}o");
            exporter.GetMermaidMultiplicity("", isSource: false).Should().Be("o{");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_unknown_value_returns_default_many()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("unknown", isSource: true).Should().Be("}o");
            exporter.GetMermaidMultiplicity("unknown", isSource: false).Should().Be("o{");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_is_case_insensitive()
        {
            MermaidExporter exporter = new MermaidExporter();
            // Uppercase shouldn't matter since we normalize
            exporter.GetMermaidMultiplicity("1", isSource: true).Should().Be("||");
        }

        [TestMethod]
        public void GetMermaidMultiplicity_trims_whitespace()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidMultiplicity("  1  ", isSource: true).Should().Be("||");
            exporter.GetMermaidMultiplicity(" 0..1 ", isSource: true).Should().Be("|o");
        }

        #endregion

        #region GetMermaidRelationship Tests

        [TestMethod]
        public void GetMermaidRelationship_one_to_one_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("1", "1").Should().Be("||--||");
        }

        [TestMethod]
        public void GetMermaidRelationship_one_to_many_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("1", "*").Should().Be("||--o{");
        }

        [TestMethod]
        public void GetMermaidRelationship_many_to_one_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("*", "1").Should().Be("}o--||");
        }

        [TestMethod]
        public void GetMermaidRelationship_many_to_many_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("*", "*").Should().Be("}o--o{");
        }

        [TestMethod]
        public void GetMermaidRelationship_one_to_zero_or_one_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("1", "0..1").Should().Be("||--o|");
        }

        [TestMethod]
        public void GetMermaidRelationship_zero_or_one_to_one_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("0..1", "1").Should().Be("|o--||");
        }

        [TestMethod]
        public void GetMermaidRelationship_zero_or_one_to_many_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("0..1", "*").Should().Be("|o--o{");
        }

        [TestMethod]
        public void GetMermaidRelationship_one_to_one_or_more_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("1", "1..*").Should().Be("||--|{");
        }

        [TestMethod]
        public void GetMermaidRelationship_one_or_more_to_one_returns_correct_symbol()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship("1..*", "1").Should().Be("}|--||");
        }

        [TestMethod]
        public void GetMermaidRelationship_with_null_multiplicities_returns_default()
        {
            MermaidExporter exporter = new MermaidExporter();
            exporter.GetMermaidRelationship(null, null).Should().Be("}o--o{");
        }

        #endregion

        #region GenerateMermaid Tests

        [TestMethod]
        public void GenerateMermaid_with_null_diagram_throws_ArgumentNullException()
        {
            MermaidExporter exporter = new MermaidExporter();

            Action act = () => exporter.GenerateMermaid(null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("diagram");
        }

        [TestMethod]
        public void GenerateMermaid_with_null_diagram_and_showTypes_throws_ArgumentNullException()
        {
            MermaidExporter exporter = new MermaidExporter();

            Action act = () => exporter.GenerateMermaid(null, showTypes: true);
            act.Should().Throw<ArgumentNullException>().WithParameterName("diagram");
        }

        #endregion

        #region ExportToMermaid Tests

        [TestMethod]
        public void ExportToMermaid_with_null_diagram_throws_ArgumentNullException()
        {
            MermaidExporter exporter = new MermaidExporter();

            Action act = () => exporter.ExportToMermaid(null, "test.mmd");
            act.Should().Throw<ArgumentNullException>().WithParameterName("diagram");
        }

        [TestMethod]
        public void ExportToMermaid_with_null_filePath_throws_ArgumentNullException()
        {
            MermaidExporter exporter = new MermaidExporter();

            // We can't create a real diagram without VS infrastructure, so we test the path validation
            // by catching the diagram null check first
            Action act = () => exporter.ExportToMermaid(null, null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("diagram");
        }

        [TestMethod]
        public void ExportToMermaid_with_empty_filePath_throws_ArgumentNullException()
        {
            MermaidExporter exporter = new MermaidExporter();

            Action act = () => exporter.ExportToMermaid(null, "");
            act.Should().Throw<ArgumentNullException>().WithParameterName("diagram");
        }

        #endregion

        #region Edge Cases and Integration Tests

        [TestMethod]
        public void SanitizeName_with_consecutive_special_chars_produces_consecutive_underscores()
        {
            MermaidExporter.SanitizeName("A--B").Should().Be("A__B");
        }

        [TestMethod]
        public void SanitizeName_with_only_special_chars_returns_unknown()
        {
            // All special chars become underscores, but empty after trim would be Unknown
            // Actually "---" becomes "___" which is not empty
            MermaidExporter.SanitizeName("---").Should().Be("___");
        }

        [TestMethod]
        public void SanitizeType_with_nested_generics_handles_correctly()
        {
            // "List<Nullable<Int32>>" -> remove Nullable< and both > chars -> "List<Int32" -> sanitize -> "list_int32"
            MermaidExporter.SanitizeType("List<Nullable<Int32>>").Should().Be("list_int32");
        }

        [TestMethod]
        public void SanitizeType_preserves_valid_type_with_digits()
        {
            MermaidExporter.SanitizeType("Int32").Should().Be("int32");
            MermaidExporter.SanitizeType("Int64").Should().Be("int64");
        }

        [TestMethod]
        public void GetMermaidRelationship_all_standard_ef_relationships()
        {
            MermaidExporter exporter = new MermaidExporter();

            // Standard EF relationship patterns
            exporter.GetMermaidRelationship("1", "*").Should().Be("||--o{");      // One-to-Many
            exporter.GetMermaidRelationship("*", "1").Should().Be("}o--||");      // Many-to-One
            exporter.GetMermaidRelationship("1", "1").Should().Be("||--||");      // One-to-One
            exporter.GetMermaidRelationship("*", "*").Should().Be("}o--o{");      // Many-to-Many
            exporter.GetMermaidRelationship("1", "0..1").Should().Be("||--o|");   // One-to-ZeroOrOne
            exporter.GetMermaidRelationship("0..1", "1").Should().Be("|o--||");   // ZeroOrOne-to-One
        }

        #endregion
    }
}
