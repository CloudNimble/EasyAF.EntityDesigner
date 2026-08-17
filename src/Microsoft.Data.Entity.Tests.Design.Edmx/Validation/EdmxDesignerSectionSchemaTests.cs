// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx.Validation;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.EntityFramework;

namespace Microsoft.Data.Entity.Tests.Design.Edmx.Validation
{
    /// <summary>
    ///     Verifies that the EDMX schema accepts the Designer section's children in any order.
    /// </summary>
    /// <remarks>
    ///     The designer parses these elements by name rather than by position, so ordering carries no meaning. The
    ///     schema previously modelled them as an ordered sequence, which caused a model whose Diagrams element came
    ///     first to fail validation and open blank. These tests keep the schema from being tightened back.
    /// </remarks>
    [TestClass]
    public class EdmxDesignerSectionSchemaTests
    {
        private const string EdmxNamespace = "http://schemas.microsoft.com/ado/2009/11/edmx";
        private const string ExtensionNamespace = "http://schemas.cloudnimble.com/easyaf/2025/01/edmx";

        private const string Connection =
            "<Connection><DesignerInfoPropertySet>"
            + "<DesignerProperty Name=\"MetadataArtifactProcessing\" Value=\"EmbedInOutputAssembly\" />"
            + "</DesignerInfoPropertySet></Connection>";

        private const string Options =
            "<Options><DesignerInfoPropertySet>"
            + "<DesignerProperty Name=\"ValidateOnBuild\" Value=\"true\" />"
            + "</DesignerInfoPropertySet></Options>";

        private const string Diagrams = "<Diagrams></Diagrams>";

        private const string Extensions = "<easyaf:Extensions><easyaf:OnModelCreating /></easyaf:Extensions>";

        [TestMethod]
        public void Designer_section_is_valid_when_children_are_in_schema_declaration_order()
        {
            ValidateDesignerSection(Connection + Options + Diagrams).Should().BeEmpty();
        }

        [TestMethod]
        public void Designer_section_is_valid_when_Diagrams_appears_before_Connection_and_Options()
        {
            ValidateDesignerSection(Diagrams + Connection + Options).Should().BeEmpty();
        }

        [TestMethod]
        public void Designer_section_is_valid_when_children_are_in_an_arbitrary_order()
        {
            ValidateDesignerSection(Options + Diagrams + Connection).Should().BeEmpty();
        }

        [TestMethod]
        public void Designer_section_is_valid_when_an_extension_element_follows_the_known_children()
        {
            ValidateDesignerSection(Connection + Options + Diagrams + Extensions).Should().BeEmpty();
        }

        [TestMethod]
        public void Designer_section_is_valid_when_an_extension_element_is_interleaved_with_the_known_children()
        {
            ValidateDesignerSection(Diagrams + Extensions + Connection + Options).Should().BeEmpty();
        }

        [TestMethod]
        public void Designer_section_is_valid_when_it_contains_only_an_extension_element()
        {
            ValidateDesignerSection(Extensions).Should().BeEmpty();
        }

        [TestMethod]
        public void Designer_section_is_valid_when_it_is_empty()
        {
            ValidateDesignerSection(string.Empty).Should().BeEmpty();
        }

        [TestMethod]
        public void Designer_section_is_not_valid_when_it_contains_an_unknown_element_in_the_edmx_namespace()
        {
            ValidateDesignerSection("<NotAThing />").Should().NotBeEmpty();
        }

        /// <summary>
        ///     Validates an EDMX document containing the supplied Designer section body against the EDMX schema.
        /// </summary>
        /// <param name="designerBody">The markup to place inside the Designer element.</param>
        /// <returns>The validation errors raised, which is empty when the document is valid.</returns>
        private static IList<string> ValidateDesignerSection(string designerBody)
        {
            var edmx = string.Format(
                CultureInfo.InvariantCulture,
                "<Edmx Version=\"3.0\" xmlns=\"{0}\" xmlns:easyaf=\"{1}\"><Designer>{2}</Designer></Edmx>",
                EdmxNamespace, ExtensionNamespace, designerBody);

            XmlDocument document = new XmlDocument
            {
                Schemas = EscherAttributeContentValidator.GetInstance(EntityFrameworkVersion.Version3).EdmxSchemaSet
            };
            document.LoadXml(edmx);

            SchemaValidationErrorCollector collector = new SchemaValidationErrorCollector();
            document.Validate(collector.ValidationCallBack);

            return collector.Errors;
        }
    }
}
