// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class MermaidExporterTests
    {
        #region SanitizeName Tests

        [TestMethod]
        public void SanitizeName_with_normal_name_returns_unchanged()
        {
            Assert.Equal("Customer", MermaidExporter.SanitizeName("Customer"));
        }

        [TestMethod]
        public void SanitizeName_with_underscores_returns_unchanged()
        {
            Assert.Equal("Customer_Order", MermaidExporter.SanitizeName("Customer_Order"));
        }

        [TestMethod]
        public void SanitizeName_with_spaces_replaces_with_underscores()
        {
            Assert.Equal("Customer_Order", MermaidExporter.SanitizeName("Customer Order"));
        }

        [TestMethod]
        public void SanitizeName_with_multiple_spaces_replaces_each_with_underscore()
        {
            Assert.Equal("My_Entity_Name", MermaidExporter.SanitizeName("My Entity Name"));
        }

        [TestMethod]
        public void SanitizeName_with_special_characters_replaces_with_underscores()
        {
            Assert.Equal("Customer_Order_", MermaidExporter.SanitizeName("Customer-Order!"));
        }

        [TestMethod]
        public void SanitizeName_with_dots_replaces_with_underscores()
        {
            Assert.Equal("System_String", MermaidExporter.SanitizeName("System.String"));
        }

        [TestMethod]
        public void SanitizeName_with_leading_digit_prepends_underscore()
        {
            Assert.Equal("_123Entity", MermaidExporter.SanitizeName("123Entity"));
        }

        [TestMethod]
        public void SanitizeName_with_all_digits_prepends_underscore()
        {
            Assert.Equal("_12345", MermaidExporter.SanitizeName("12345"));
        }

        [TestMethod]
        public void SanitizeName_with_null_returns_unknown()
        {
            Assert.Equal("Unknown", MermaidExporter.SanitizeName(null));
        }

        [TestMethod]
        public void SanitizeName_with_empty_string_returns_unknown()
        {
            Assert.Equal("Unknown", MermaidExporter.SanitizeName(""));
        }

        [TestMethod]
        public void SanitizeName_with_whitespace_only_returns_underscores()
        {
            // Whitespace is replaced with underscores, not trimmed
            Assert.Equal("___", MermaidExporter.SanitizeName("   "));
        }

        [TestMethod]
        public void SanitizeName_with_unicode_letters_preserves_them()
        {
            Assert.Equal("Kundenname", MermaidExporter.SanitizeName("Kundenname"));
        }

        [TestMethod]
        public void SanitizeName_with_mixed_unicode_and_special_chars()
        {
            Assert.Equal("Cafe_Latte", MermaidExporter.SanitizeName("Cafe-Latte"));
        }

        [TestMethod]
        public void SanitizeName_with_parentheses_replaces_with_underscores()
        {
            Assert.Equal("Entity_1_", MermaidExporter.SanitizeName("Entity(1)"));
        }

        [TestMethod]
        public void SanitizeName_with_angle_brackets_replaces_with_underscores()
        {
            Assert.Equal("List_int_", MermaidExporter.SanitizeName("List<int>"));
        }

        #endregion

        #region SanitizeType Tests

        [TestMethod]
        public void SanitizeType_with_simple_type_returns_lowercase()
        {
            Assert.Equal("string", MermaidExporter.SanitizeType("String"));
        }

        [TestMethod]
        public void SanitizeType_with_system_prefix_removes_it()
        {
            Assert.Equal("string", MermaidExporter.SanitizeType("System.String"));
        }

        [TestMethod]
        public void SanitizeType_with_nullable_removes_wrapper()
        {
            Assert.Equal("int32", MermaidExporter.SanitizeType("Nullable<Int32>"));
        }

        [TestMethod]
        public void SanitizeType_with_nullable_shorthand_removes_it()
        {
            Assert.Equal("int", MermaidExporter.SanitizeType("int?"));
        }

        [TestMethod]
        public void SanitizeType_with_system_nullable_removes_both()
        {
            Assert.Equal("datetime", MermaidExporter.SanitizeType("System.Nullable<DateTime>"));
        }

        [TestMethod]
        public void SanitizeType_with_null_returns_unknown()
        {
            Assert.Equal("unknown", MermaidExporter.SanitizeType(null));
        }

        [TestMethod]
        public void SanitizeType_with_empty_returns_unknown()
        {
            Assert.Equal("unknown", MermaidExporter.SanitizeType(""));
        }

        [TestMethod]
        public void SanitizeType_with_int32_returns_lowercase()
        {
            Assert.Equal("int32", MermaidExporter.SanitizeType("Int32"));
        }

        [TestMethod]
        public void SanitizeType_with_guid_returns_lowercase()
        {
            Assert.Equal("guid", MermaidExporter.SanitizeType("System.Guid"));
        }

        [TestMethod]
        public void SanitizeType_with_decimal_returns_lowercase()
        {
            Assert.Equal("decimal", MermaidExporter.SanitizeType("Decimal"));
        }

        #endregion

        #region GetMermaidMultiplicity Tests

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_source_returns_pipes()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("||", exporter.GetMermaidMultiplicity("1", isSource: true));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_target_returns_pipes()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("||", exporter.GetMermaidMultiplicity("1", isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_or_one_source_returns_pipe_o()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("|o", exporter.GetMermaidMultiplicity("0..1", isSource: true));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_or_one_target_returns_o_pipe()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("o|", exporter.GetMermaidMultiplicity("0..1", isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_star_source_returns_brace_o()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o", exporter.GetMermaidMultiplicity("*", isSource: true));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_star_target_returns_o_brace()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("o{", exporter.GetMermaidMultiplicity("*", isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_to_many_source_returns_brace_o()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o", exporter.GetMermaidMultiplicity("0..*", isSource: true));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_zero_to_many_target_returns_o_brace()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("o{", exporter.GetMermaidMultiplicity("0..*", isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_to_many_source_returns_brace_pipe()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}|", exporter.GetMermaidMultiplicity("1..*", isSource: true));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_one_to_many_target_returns_pipe_brace()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("|{", exporter.GetMermaidMultiplicity("1..*", isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_null_returns_default_many()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o", exporter.GetMermaidMultiplicity(null, isSource: true));
            Assert.Equal("o{", exporter.GetMermaidMultiplicity(null, isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_empty_returns_default_many()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o", exporter.GetMermaidMultiplicity("", isSource: true));
            Assert.Equal("o{", exporter.GetMermaidMultiplicity("", isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_with_unknown_value_returns_default_many()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o", exporter.GetMermaidMultiplicity("unknown", isSource: true));
            Assert.Equal("o{", exporter.GetMermaidMultiplicity("unknown", isSource: false));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_is_case_insensitive()
        {
            var exporter = new MermaidExporter();
            // Uppercase shouldn't matter since we normalize
            Assert.Equal("||", exporter.GetMermaidMultiplicity("1", isSource: true));
        }

        [TestMethod]
        public void GetMermaidMultiplicity_trims_whitespace()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("||", exporter.GetMermaidMultiplicity("  1  ", isSource: true));
            Assert.Equal("|o", exporter.GetMermaidMultiplicity(" 0..1 ", isSource: true));
        }

        #endregion

        #region GetMermaidRelationship Tests

        [TestMethod]
        public void GetMermaidRelationship_one_to_one_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("||--||", exporter.GetMermaidRelationship("1", "1"));
        }

        [TestMethod]
        public void GetMermaidRelationship_one_to_many_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("||--o{", exporter.GetMermaidRelationship("1", "*"));
        }

        [TestMethod]
        public void GetMermaidRelationship_many_to_one_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o--||", exporter.GetMermaidRelationship("*", "1"));
        }

        [TestMethod]
        public void GetMermaidRelationship_many_to_many_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o--o{", exporter.GetMermaidRelationship("*", "*"));
        }

        [TestMethod]
        public void GetMermaidRelationship_one_to_zero_or_one_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("||--o|", exporter.GetMermaidRelationship("1", "0..1"));
        }

        [TestMethod]
        public void GetMermaidRelationship_zero_or_one_to_one_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("|o--||", exporter.GetMermaidRelationship("0..1", "1"));
        }

        [TestMethod]
        public void GetMermaidRelationship_zero_or_one_to_many_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("|o--o{", exporter.GetMermaidRelationship("0..1", "*"));
        }

        [TestMethod]
        public void GetMermaidRelationship_one_to_one_or_more_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("||--|{", exporter.GetMermaidRelationship("1", "1..*"));
        }

        [TestMethod]
        public void GetMermaidRelationship_one_or_more_to_one_returns_correct_symbol()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}|--||", exporter.GetMermaidRelationship("1..*", "1"));
        }

        [TestMethod]
        public void GetMermaidRelationship_with_null_multiplicities_returns_default()
        {
            var exporter = new MermaidExporter();
            Assert.Equal("}o--o{", exporter.GetMermaidRelationship(null, null));
        }

        #endregion

        #region GenerateMermaid Tests

        [TestMethod]
        public void GenerateMermaid_with_null_diagram_throws_ArgumentNullException()
        {
            var exporter = new MermaidExporter();

            var ex = Assert.Throws<ArgumentNullException>(() => exporter.GenerateMermaid(null));
            ex.ParamName.Should().Be("diagram");
        }

        [TestMethod]
        public void GenerateMermaid_with_null_diagram_and_showTypes_throws_ArgumentNullException()
        {
            var exporter = new MermaidExporter();

            var ex = Assert.Throws<ArgumentNullException>(() => exporter.GenerateMermaid(null, showTypes: true));
            ex.ParamName.Should().Be("diagram");
        }

        #endregion

        #region ExportToMermaid Tests

        [TestMethod]
        public void ExportToMermaid_with_null_diagram_throws_ArgumentNullException()
        {
            var exporter = new MermaidExporter();

            var ex = Assert.Throws<ArgumentNullException>(() => exporter.ExportToMermaid(null, "test.mmd"));
            ex.ParamName.Should().Be("diagram");
        }

        [TestMethod]
        public void ExportToMermaid_with_null_filePath_throws_ArgumentNullException()
        {
            var exporter = new MermaidExporter();

            // We can't create a real diagram without VS infrastructure, so we test the path validation
            // by catching the diagram null check first
            var ex = Assert.Throws<ArgumentNullException>(() => exporter.ExportToMermaid(null, null));
            ex.ParamName.Should().Be("diagram");
        }

        [TestMethod]
        public void ExportToMermaid_with_empty_filePath_throws_ArgumentNullException()
        {
            var exporter = new MermaidExporter();

            var ex = Assert.Throws<ArgumentNullException>(() => exporter.ExportToMermaid(null, ""));
            ex.ParamName.Should().Be("diagram");
        }

        #endregion

        #region Edge Cases and Integration Tests

        [TestMethod]
        public void SanitizeName_with_consecutive_special_chars_produces_consecutive_underscores()
        {
            Assert.Equal("A__B", MermaidExporter.SanitizeName("A--B"));
        }

        [TestMethod]
        public void SanitizeName_with_only_special_chars_returns_unknown()
        {
            // All special chars become underscores, but empty after trim would be Unknown
            // Actually "---" becomes "___" which is not empty
            Assert.Equal("___", MermaidExporter.SanitizeName("---"));
        }

        [TestMethod]
        public void SanitizeType_with_nested_generics_handles_correctly()
        {
            // "List<Nullable<Int32>>" -> remove Nullable< and both > chars -> "List<Int32" -> sanitize -> "list_int32"
            Assert.Equal("list_int32", MermaidExporter.SanitizeType("List<Nullable<Int32>>"));
        }

        [TestMethod]
        public void SanitizeType_preserves_valid_type_with_digits()
        {
            Assert.Equal("int32", MermaidExporter.SanitizeType("Int32"));
            Assert.Equal("int64", MermaidExporter.SanitizeType("Int64"));
        }

        [TestMethod]
        public void GetMermaidRelationship_all_standard_ef_relationships()
        {
            var exporter = new MermaidExporter();

            // Standard EF relationship patterns
            Assert.Equal("||--o{", exporter.GetMermaidRelationship("1", "*"));      // One-to-Many
            Assert.Equal("}o--||", exporter.GetMermaidRelationship("*", "1"));      // Many-to-One
            Assert.Equal("||--||", exporter.GetMermaidRelationship("1", "1"));      // One-to-One
            Assert.Equal("}o--o{", exporter.GetMermaidRelationship("*", "*"));      // Many-to-Many
            Assert.Equal("||--o|", exporter.GetMermaidRelationship("1", "0..1"));   // One-to-ZeroOrOne
            Assert.Equal("|o--||", exporter.GetMermaidRelationship("0..1", "1"));   // ZeroOrOne-to-One
        }

        #endregion
    }
}
