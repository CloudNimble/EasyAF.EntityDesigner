// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Drawing;
using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    [TestClass]
    public class SvgStyleHelperTests
    {
        [TestMethod]
        public void ToSvgColor_converts_color_to_hex()
        {
            SvgStylesheetManager.ToSvgColor(Color.Red).Should().Be("#FF0000");
            SvgStylesheetManager.ToSvgColor(Color.Lime).Should().Be("#00FF00");
            SvgStylesheetManager.ToSvgColor(Color.Blue).Should().Be("#0000FF");
            SvgStylesheetManager.ToSvgColor(Color.Black).Should().Be("#000000");
            SvgStylesheetManager.ToSvgColor(Color.White).Should().Be("#FFFFFF");
        }

        [TestMethod]
        public void ToSvgColor_returns_none_for_transparent()
        {
            SvgStylesheetManager.ToSvgColor(Color.Transparent).Should().Be("none");
        }

        [TestMethod]
        public void FormatDouble_uses_invariant_culture()
        {
            SvgStylesheetManager.FormatDouble(123.456).Should().Be("123.46");
            SvgStylesheetManager.FormatDouble(0).Should().Be("0.00");
            SvgStylesheetManager.FormatDouble(-50.5).Should().Be("-50.50");
        }

        [TestMethod]
        public void GetTextColorForFill_returns_white_for_dark_fills()
        {
            SvgStylesheetManager.GetTextColorForFill(Color.Black).Should().Be(Color.White);
            SvgStylesheetManager.GetTextColorForFill(Color.DarkBlue).Should().Be(Color.White);
            SvgStylesheetManager.GetTextColorForFill(Color.DarkGreen).Should().Be(Color.White);
        }

        [TestMethod]
        public void GetTextColorForFill_returns_black_for_light_fills()
        {
            SvgStylesheetManager.GetTextColorForFill(Color.White).Should().Be(Color.Black);
            SvgStylesheetManager.GetTextColorForFill(Color.Yellow).Should().Be(Color.Black);
            SvgStylesheetManager.GetTextColorForFill(Color.LightGray).Should().Be(Color.Black);
        }

        [TestMethod]
        public void GetDashArray_creates_comma_separated_values()
        {
            var pattern = new float[] { 5, 3 };
            SvgStylesheetManager.GetDashArray(pattern).Should().Be("5.00,3.00");
        }

        [TestMethod]
        public void GetDashArray_returns_null_for_empty_pattern()
        {
            SvgStylesheetManager.GetDashArray(new float[0]).Should().BeNull();
            SvgStylesheetManager.GetDashArray(null).Should().BeNull();
        }

        [TestMethod]
        public void EscapeXml_escapes_special_characters()
        {
            SvgStylesheetManager.EscapeXml("&").Should().Be("&amp;");
            SvgStylesheetManager.EscapeXml("<").Should().Be("&lt;");
            SvgStylesheetManager.EscapeXml(">").Should().Be("&gt;");
            SvgStylesheetManager.EscapeXml("\"").Should().Be("&quot;");
            SvgStylesheetManager.EscapeXml("'").Should().Be("&apos;");
        }

        [TestMethod]
        public void EscapeXml_escapes_combined_special_characters()
        {
            SvgStylesheetManager.EscapeXml("<div class=\"test\">A & B</div>")
                .Should().Be("&lt;div class=&quot;test&quot;&gt;A &amp; B&lt;/div&gt;");
        }

        [TestMethod]
        public void EscapeXml_returns_input_for_null_or_empty()
        {
            SvgStylesheetManager.EscapeXml(null).Should().BeNull();
            SvgStylesheetManager.EscapeXml(string.Empty).Should().Be(string.Empty);
        }

        [TestMethod]
        public void EscapeXml_preserves_normal_text()
        {
            SvgStylesheetManager.EscapeXml("Hello World").Should().Be("Hello World");
            SvgStylesheetManager.EscapeXml("CustomerOrder").Should().Be("CustomerOrder");
        }

        [TestMethod]
        public void GetFontFamily_returns_default_for_null()
        {
            SvgStylesheetManager.GetFontFamily(null).Should().Be("Segoe UI, Arial, sans-serif");
        }

        [TestMethod]
        public void GetFontSize_returns_default_for_null()
        {
            SvgStylesheetManager.GetFontSize(null).Should().Be("12");
        }

        [TestMethod]
        public void GetFontWeight_returns_normal_for_null()
        {
            SvgStylesheetManager.GetFontWeight(null).Should().Be("normal");
        }

        [TestMethod]
        public void GetFontStyle_returns_normal_for_null()
        {
            SvgStylesheetManager.GetFontStyle(null).Should().Be("normal");
        }

        [TestMethod]
        public void ToSvgColorWithOpacity_handles_semi_transparent_colors()
        {
            Color semiTransparent = Color.FromArgb(128, 255, 0, 0);
            var color = SvgStylesheetManager.ToSvgColorWithOpacity(semiTransparent, out string opacity);

            color.Should().Be("#FF0000");
            opacity.Should().NotBeNull();
            opacity.Should().Be("0.50");
        }

        [TestMethod]
        public void ToSvgColorWithOpacity_returns_null_opacity_for_opaque_colors()
        {
            var color = SvgStylesheetManager.ToSvgColorWithOpacity(Color.Red, out string opacity);

            color.Should().Be("#FF0000");
            opacity.Should().BeNull();
        }
    }
}
