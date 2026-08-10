// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    [TestClass]
    public class SvgConnectorRendererTests
    {
        private readonly SvgConnectorRenderer _renderer;

        public SvgConnectorRendererTests()
        {
            _renderer = new SvgConnectorRenderer();
        }

        #region GetMarkerDefinitions Tests

        [TestMethod]
        public void GetMarkerDefinitions_returns_defs_element()
        {
            var result = _renderer.GetMarkerDefinitions();

            result.Should().Contain("<defs>");
            result.Should().Contain("</defs>");
        }

        [TestMethod]
        public void GetMarkerDefinitions_includes_diamond_marker()
        {
            var result = _renderer.GetMarkerDefinitions();

            result.Should().Contain("id=\"diamond\"");
            result.Should().Contain("<polygon");
        }

        [TestMethod]
        public void GetMarkerDefinitions_includes_inheritance_arrow_marker()
        {
            var result = _renderer.GetMarkerDefinitions();

            result.Should().Contain("id=\"inheritance-arrow\"");
        }

        #endregion

        #region GetMarkerDefinitionsContent Tests

        [TestMethod]
        public void GetMarkerDefinitionsContent_returns_marker_elements_without_defs_wrapper()
        {
            var result = _renderer.GetMarkerDefinitionsContent();

            result.Should().Contain("<marker");
            result.Should().NotContain("<defs>");
            result.Should().NotContain("</defs>");
        }

        [TestMethod]
        public void GetMarkerDefinitionsContent_diamond_marker_has_correct_viewBox()
        {
            var result = _renderer.GetMarkerDefinitionsContent();

            result.Should().Contain("id=\"diamond\"");
            result.Should().Contain("viewBox=\"0 0 12 12\"");
        }

        [TestMethod]
        public void GetMarkerDefinitionsContent_diamond_marker_has_polygon_shape()
        {
            var result = _renderer.GetMarkerDefinitionsContent();

            // Diamond shape polygon points
            result.Should().Contain("points=\"6,1 11,6 6,11 1,6\"");
        }

        [TestMethod]
        public void GetMarkerDefinitionsContent_inheritance_arrow_has_correct_viewBox()
        {
            var result = _renderer.GetMarkerDefinitionsContent();

            result.Should().Contain("id=\"inheritance-arrow\"");
            result.Should().Contain("viewBox=\"0 0 12 12\"");
        }

        [TestMethod]
        public void GetMarkerDefinitionsContent_inheritance_arrow_uses_hollow_css_class()
        {
            var result = _renderer.GetMarkerDefinitionsContent();

            // Hollow arrow uses CSS class for fill="none" styling
            result.Should().Contain("class=\"arrow-hollow\"");
        }

        [TestMethod]
        public void GetMarkerDefinitionsContent_diamond_uses_css_class()
        {
            var result = _renderer.GetMarkerDefinitionsContent();

            // Diamond marker uses CSS class for styling
            result.Should().Contain("class=\"diamond\"");
        }

        [TestMethod]
        public void GetMarkerDefinitionsContent_markers_have_auto_orient()
        {
            var result = _renderer.GetMarkerDefinitionsContent();

            result.Should().Contain("orient=\"auto\"");
        }

        #endregion

        #region RenderAssociationConnector Tests

        [TestMethod]
        public void RenderAssociationConnector_with_null_connector_returns_empty_string()
        {
            var result = _renderer.RenderAssociationConnector(null, 0, 0);

            result.Should().Be(string.Empty);
        }

        #endregion

        #region RenderInheritanceConnector Tests

        [TestMethod]
        public void RenderInheritanceConnector_with_null_connector_returns_empty_string()
        {
            var result = _renderer.RenderInheritanceConnector(null, 0, 0);

            result.Should().Be(string.Empty);
        }

        #endregion
    }
}
