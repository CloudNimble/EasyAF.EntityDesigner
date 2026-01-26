// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    using System;
    using System.Drawing;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
    using Microsoft.VisualStudio.Modeling;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using FluentAssertions;

    [TestClass]
    public class SvgShapeRendererTests
    {
        private SvgIconManager _iconManager;
        private SvgShapeRenderer _renderer;
        private Store _store;

        [TestInitialize]
        public void TestInitialize()
        {
            _iconManager = new SvgIconManager();
            _renderer = new SvgShapeRenderer(_iconManager);
        }

        private Store GetStore() => _store ??= DslTestHelper.CreateStore();

        [TestCleanup]
        public void TestCleanup()
        {
            try { _store?.Dispose(); } catch { }
            _store = null;
        }

        #region RenderCompartmentHeader Tests

        [TestMethod]
        public void RenderCompartmentHeader_renders_dark_gray_background_rectangle()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", true, 10, 50, 200);

            var result = sb.ToString();
            result.Should().Contain("<rect");
            result.Should().Contain("x=\"10.00\"");
            result.Should().Contain("y=\"50.00\"");
            result.Should().Contain("width=\"200.00\"");
            result.Should().Contain("class=\"header-compartment\"");
        }

        [TestMethod]
        public void RenderCompartmentHeader_renders_Collapse_icon_when_expanded()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", isExpanded: true, 10, 50, 200);

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-Collapse\"");
        }

        [TestMethod]
        public void RenderCompartmentHeader_renders_Expand_icon_when_collapsed()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", isExpanded: false, 10, 50, 200);

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-Expand\"");
        }

        [TestMethod]
        public void RenderCompartmentHeader_renders_text_label_with_css_class()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", true, 10, 50, 200);

            var result = sb.ToString();
            result.Should().Contain("<text");
            result.Should().Contain("class=\"text-base text-compartment\"");
            result.Should().Contain(">Properties</text>");
        }

        [TestMethod]
        public void RenderCompartmentHeader_renders_NavigationProperties_label()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Navigation Properties", true, 10, 50, 200);

            var result = sb.ToString();
            result.Should().Contain(">Navigation Properties</text>");
        }

        [TestMethod]
        public void RenderCompartmentHeader_escapes_special_xml_characters_in_header_text()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Props & Nav <test>", true, 10, 50, 200);

            var result = sb.ToString();
            result.Should().Contain("&amp;");
            result.Should().Contain("&lt;");
            result.Should().Contain("&gt;");
        }

        [DataTestMethod]
        [DataRow(0, 0, 100)]
        [DataRow(50, 100, 250)]
        [DataRow(123.5, 456.7, 300)]
        public void RenderCompartmentHeader_positions_elements_correctly(double x, double y, double width)
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", true, x, y, width);

            var result = sb.ToString();
            result.Should().Contain(string.Format("x=\"{0}\"", SvgStylesheetManager.FormatDouble(x)));
            result.Should().Contain(string.Format("y=\"{0}\"", SvgStylesheetManager.FormatDouble(y)));
            result.Should().Contain(string.Format("width=\"{0}\"", SvgStylesheetManager.FormatDouble(width)));
        }

        [TestMethod]
        public void RenderCompartmentHeader_chevron_is_positioned_with_padding()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", true, 10, 50, 200);

            var result = sb.ToString();
            // Chevron X should be x + chevronPadding (2.0) = 12.0
            // Chevron Y should be GetCenteredIconY(50, 24, 14) = 50 + (24 - 14) / 2 = 55.0
            result.Should().Contain("x=\"12.00\"");
            result.Should().Contain("y=\"55.00\"");
        }

        [TestMethod]
        public void RenderCompartmentHeader_text_is_positioned_after_chevron()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", true, 10, 50, 200);

            var result = sb.ToString();
            // Text X should be x + chevronSize (14) + 6 = 10 + 14 + 6 = 30
            result.Should().Contain("x=\"30.00\"");
            // Text Y should be GetCenteredTextBaselineY(50, 24, 11) = 50 + 12 + 3.85 = 65.85
            result.Should().Contain("y=\"65.85\"");
        }

        [TestMethod]
        public void RenderCompartmentHeader_uses_css_classes_for_text_styling()
        {
            var sb = new StringBuilder();
            _renderer.RenderCompartmentHeader(sb, "Properties", true, 10, 50, 200);

            var result = sb.ToString();
            // Should use CSS classes instead of inline styles
            result.Should().Contain("class=\"text-base text-compartment\"");
            // Should NOT contain inline font styles
            result.Should().NotContain("font-family=");
            result.Should().NotContain("font-size=");
        }

        #endregion

        #region GetOutlineColor Tests

        [TestMethod]
        public void GetOutlineColor_returns_lighter_color_for_dark_fill()
        {
            var darkColor = Color.FromArgb(50, 50, 50);
            var outlineColor = _renderer.GetOutlineColor(darkColor);

            // Should be lighter than input
            var brightness = (outlineColor.R + outlineColor.G + outlineColor.B) / 3.0;
            var originalBrightness = (darkColor.R + darkColor.G + darkColor.B) / 3.0;
            brightness.Should().BeGreaterThan(originalBrightness);
        }

        [TestMethod]
        public void GetOutlineColor_returns_darker_color_for_light_fill()
        {
            var lightColor = Color.FromArgb(200, 200, 200);
            var outlineColor = _renderer.GetOutlineColor(lightColor);

            // Should be darker than input
            var brightness = (outlineColor.R + outlineColor.G + outlineColor.B) / 3.0;
            var originalBrightness = (lightColor.R + lightColor.G + lightColor.B) / 3.0;
            brightness.Should().BeLessThan(originalBrightness);
        }

        [TestMethod]
        public void GetOutlineColor_handles_pure_black()
        {
            var outlineColor = _renderer.GetOutlineColor(Color.Black);

            // Should return a valid color (slightly lighter)
            outlineColor.Should().NotBe(Color.Black);
        }

        [TestMethod]
        public void GetOutlineColor_handles_pure_white()
        {
            var outlineColor = _renderer.GetOutlineColor(Color.White);

            // Should return a valid color (slightly darker)
            outlineColor.Should().NotBe(Color.White);
        }

        #endregion

        #region GetPropertyDisplayText Tests

        [TestMethod]
        public void GetPropertyDisplayText_with_null_item_returns_property_fallback()
        {
            var result = _renderer.GetPropertyDisplayText(null, isScalarProperty: true, showTypes: false);

            result.Should().Be("Property");
        }

        [TestMethod]
        public void GetPropertyDisplayText_with_scalar_property_returns_name()
        {
            var prop = DslTestHelper.CreateScalarProperty(GetStore(), "CustomerName", "String", false);

            var result = _renderer.GetPropertyDisplayText(prop, isScalarProperty: true, showTypes: false);

            result.Should().Be("CustomerName");
        }

        [TestMethod]
        public void GetPropertyDisplayText_with_scalar_property_and_show_types_returns_name_and_type()
        {
            var prop = DslTestHelper.CreateScalarProperty(GetStore(), "CustomerName", "String", false);

            var result = _renderer.GetPropertyDisplayText(prop, isScalarProperty: true, showTypes: true);

            result.Should().Be("CustomerName : String");
        }

        [TestMethod]
        public void GetPropertyDisplayText_with_navigation_property_returns_name()
        {
            var prop = DslTestHelper.CreateNavigationProperty(GetStore(), "Orders");

            var result = _renderer.GetPropertyDisplayText(prop, isScalarProperty: false, showTypes: false);

            result.Should().Be("Orders");
        }

        [TestMethod]
        public void GetPropertyDisplayText_with_complex_property_returns_name()
        {
            var prop = DslTestHelper.CreateComplexProperty(GetStore(), "Address");

            var result = _renderer.GetPropertyDisplayText(prop, isScalarProperty: true, showTypes: false);

            result.Should().Be("Address");
        }

        #endregion

        #region RenderPropertyIcon Tests

        [TestMethod]
        public void RenderPropertyIcon_uses_default_Property_icon_for_unknown_type()
        {
            var sb = new StringBuilder();

            _renderer.RenderPropertyIcon(sb, 10, 20, new object());

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-Property\"");
        }

        [TestMethod]
        public void RenderPropertyIcon_uses_PropertyKey_icon_for_entity_key()
        {
            var prop = DslTestHelper.CreateScalarProperty(GetStore(), "Id", "Int32", isKey: true);
            var sb = new StringBuilder();

            _renderer.RenderPropertyIcon(sb, 10, 20, prop);

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-PropertyKey\"");
        }

        [TestMethod]
        public void RenderPropertyIcon_uses_Property_icon_for_non_key_scalar()
        {
            var prop = DslTestHelper.CreateScalarProperty(GetStore(), "Name", "String", isKey: false);
            var sb = new StringBuilder();

            _renderer.RenderPropertyIcon(sb, 10, 20, prop);

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-Property\"");
        }

        [TestMethod]
        public void RenderPropertyIcon_uses_ComplexProperty_icon_for_complex_property()
        {
            var prop = DslTestHelper.CreateComplexProperty(GetStore(), "Address");
            var sb = new StringBuilder();

            _renderer.RenderPropertyIcon(sb, 10, 20, prop);

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-ComplexProperty\"");
        }

        [TestMethod]
        public void RenderPropertyIcon_uses_NavigationProperty_icon_for_navigation_property()
        {
            var prop = DslTestHelper.CreateNavigationProperty(GetStore(), "Orders");
            var sb = new StringBuilder();

            _renderer.RenderPropertyIcon(sb, 10, 20, prop);

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-NavigationProperty\"");
        }

        #endregion

        #region RenderEntityIcon Tests

        [TestMethod]
        public void RenderEntityIcon_uses_Class_icon()
        {
            var sb = new StringBuilder();

            _renderer.RenderEntityIcon(sb, 10, 20, Color.White);

            var result = sb.ToString();
            result.Should().Contain("href=\"#icon-Class\"");
        }

        [TestMethod]
        public void RenderEntityIcon_appends_icon_reference_to_builder()
        {
            var sb = new StringBuilder();

            _renderer.RenderEntityIcon(sb, 10, 20, Color.White);

            var result = sb.ToString();
            result.Should().Contain("<use href=\"#icon-Class\"");
        }

        #endregion

        #region Vertical Centering Tests

        [DataTestMethod]
        [DataRow(0, 30, 16, 7)]      // Entity header: (30-16)/2 = 7
        [DataRow(0, 24, 14, 5)]      // Compartment header: (24-14)/2 = 5
        [DataRow(0, 18, 14, 2)]      // Property row: (18-14)/2 = 2
        [DataRow(100, 40, 20, 110)]  // Offset container: 100 + (40-20)/2 = 110
        public void GetCenteredIconY_returns_correct_position(
            double containerY, double containerHeight, double iconSize, double expectedY)
        {
            var result = SvgShapeRenderer.GetCenteredIconY(containerY, containerHeight, iconSize);
            result.Should().BeApproximately(expectedY, precision: 0.01);
        }

        [DataTestMethod]
        [DataRow(0, 30, 12, 19.2)]   // Entity header: 15 + (12*0.7/2) = 15 + 4.2 = 19.2
        [DataRow(0, 24, 11, 15.85)]  // Compartment header: 12 + (11*0.7/2) = 12 + 3.85 = 15.85
        [DataRow(0, 18, 11, 12.85)]  // Property row: 9 + (11*0.7/2) = 9 + 3.85 = 12.85
        public void GetCenteredTextBaselineY_returns_correct_position(
            double containerY, double containerHeight, double fontSizePx, double expectedY)
        {
            var result = SvgShapeRenderer.GetCenteredTextBaselineY(containerY, containerHeight, fontSizePx);
            result.Should().BeApproximately(expectedY, precision: 0.01);
        }

        [TestMethod]
        public void Centering_formulas_align_icon_and_text_to_same_centerline()
        {
            const double containerHeight = 30;
            const double iconSize = 16;
            const double fontSizePx = 12;
            const double capHeightRatio = 0.70;

            var iconY = SvgShapeRenderer.GetCenteredIconY(0, containerHeight, iconSize);
            var textBaselineY = SvgShapeRenderer.GetCenteredTextBaselineY(0, containerHeight, fontSizePx);

            // Icon visual center
            var iconCenter = iconY + iconSize / 2;

            // Text visual center (capHeight/2 above baseline)
            var capHeight = fontSizePx * capHeightRatio;
            var textVisualCenter = textBaselineY - capHeight / 2;

            // Both should equal containerHeight / 2
            iconCenter.Should().BeApproximately(containerHeight / 2, precision: 0.01);
            textVisualCenter.Should().BeApproximately(containerHeight / 2, precision: 0.01);
        }

        #endregion
    }
}
