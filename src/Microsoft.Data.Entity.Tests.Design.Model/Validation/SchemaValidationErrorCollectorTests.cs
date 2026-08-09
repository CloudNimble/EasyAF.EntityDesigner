// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model.Validation
{
    /// <summary>
    ///     Verifies that schema validation failures are retained with enough detail to act on.
    /// </summary>
    /// <remarks>
    ///     A document that fails schema validation opens in the XML editor rather than the designer. When this
    ///     collector kept only a count, the user was told the XML was not valid and nothing else, which is not
    ///     something anyone can correct. These tests keep the message and source location from being dropped again.
    /// </remarks>
    [TestClass]
    public class SchemaValidationErrorCollectorTests
    {
        private const string EdmxNamespace = "http://schemas.microsoft.com/ado/2009/11/edmx";

        [TestMethod]
        public void A_new_collector_reports_no_errors()
        {
            SchemaValidationErrorCollector collector = new SchemaValidationErrorCollector();

            collector.ErrorCount.Should().Be(0);
            collector.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public void ValidationCallBack_ignores_a_null_validation_event()
        {
            SchemaValidationErrorCollector collector = new SchemaValidationErrorCollector();

            collector.ValidationCallBack(null, null);

            collector.ErrorCount.Should().Be(0);
            collector.Errors.Should().BeEmpty();
        }

        [TestMethod]
        public void ValidationCallBack_records_the_validator_message_for_an_invalid_document()
        {
            // The Version attribute is required, so its absence makes this document invalid.
            SchemaValidationErrorCollector collector = ValidateEdmx("<Edmx xmlns=\"" + EdmxNamespace + "\" />");

            collector.ErrorCount.Should().BeGreaterThan(0);
            collector.Errors.Should().HaveCount(collector.ErrorCount);
            collector.Errors[0].Should().NotBeNullOrWhiteSpace();
            collector.Errors[0].Should().Contain("Version");
        }

        [TestMethod]
        public void ValidationCallBack_records_the_source_location_for_an_invalid_document()
        {
            SchemaValidationErrorCollector collector = ValidateEdmx("<Edmx xmlns=\"" + EdmxNamespace + "\" />");

            collector.Errors[0].Should().MatchRegex(@"\(line \d+, position \d+\)$");
        }

        [TestMethod]
        public void ValidationCallBack_records_every_error_rather_than_only_the_first()
        {
            // Two Designer sections and an unknown child give the validator more than one thing to complain about.
            SchemaValidationErrorCollector collector = ValidateEdmx(
                "<Edmx Version=\"3.0\" xmlns=\"" + EdmxNamespace + "\">"
                + "<Designer><NotAThing /></Designer><Designer /></Edmx>");

            collector.ErrorCount.Should().BeGreaterThan(1);
            collector.Errors.Should().HaveCount(collector.ErrorCount);
        }

        /// <summary>
        ///     Validates the supplied EDMX markup against the EDMX schema, collecting any errors raised.
        /// </summary>
        /// <param name="edmx">The EDMX markup to validate.</param>
        /// <returns>The collector holding the validation errors.</returns>
        private static SchemaValidationErrorCollector ValidateEdmx(string edmx)
        {
            XmlDocument document = new XmlDocument
            {
                Schemas = EscherAttributeContentValidator.GetInstance(EntityFrameworkVersion.Version3).EdmxSchemaSet
            };
            document.LoadXml(edmx);

            SchemaValidationErrorCollector collector = new SchemaValidationErrorCollector();
            document.Validate(collector.ValidationCallBack);

            return collector;
        }
    }
}
