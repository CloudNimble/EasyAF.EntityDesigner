// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    [TestClass]
    public class SvgStylesheetManagerTests
    {
        private readonly SvgStylesheetManager _manager;

        public SvgStylesheetManagerTests()
        {
            _manager = new SvgStylesheetManager();
        }

        #region GetStyleDefinitions Tests

        [TestMethod]
        public void GetStyleDefinitions_returns_style_element()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain("<style>");
            result.Should().Contain("</style>");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_icon_size_classes()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".icon {");
            result.Should().Contain("width: 16px");
            result.Should().Contain("height: 16px");
            result.Should().Contain(".icon-sm {");
            result.Should().Contain("width: 14px");
            result.Should().Contain("height: 14px");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_icon_fill_classes()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".icon-shadow {");
            result.Should().Contain(".icon-fill {");
            result.Should().Contain(".icon-accent-shadow {");
            result.Should().Contain(".icon-accent {");
            result.Should().Contain(".icon-blue {");
            result.Should().Contain(".icon-muted {");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_text_base_class()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".text-base {");
            result.Should().Contain("font-family: Segoe UI");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_text_header_class()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".text-header {");
            result.Should().Contain("font-size: 11px");
            result.Should().Contain("fill: #FFFFFF");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_text_entity_class()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".text-entity {");
            result.Should().Contain("font-size: 12px");
            result.Should().Contain("font-weight: bold");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_text_property_class()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".text-property {");
            result.Should().Contain("font-size: 11px");
            result.Should().Contain("fill: #000000");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_text_mult_class()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".text-mult {");
            result.Should().Contain("font-size: 11px");
            result.Should().Contain("font-weight: bold");
        }

        [TestMethod]
        public void GetStyleDefinitions_includes_compartment_header_class()
        {
            var result = _manager.GetStyleDefinitions();

            result.Should().Contain(".header-compartment {");
            result.Should().Contain("fill: #E0E0E0");
            result.Should().Contain("height: 24px");
        }

        [TestMethod]
        public void GetStyleDefinitions_uses_proper_indentation()
        {
            var result = _manager.GetStyleDefinitions();

            // Should start with proper indentation for embedding in SVG defs
            result.Should().StartWith("    <style>");
        }

        #endregion
    }
}
