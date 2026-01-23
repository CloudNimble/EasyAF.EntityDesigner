// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View.Export
{
    using System.Drawing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class SvgStyleHelperTests
    {
        [TestMethod]
        public void ToSvgColor_converts_color_to_hex()
        {
            Assert.Equal("#FF0000", SvgStylesheetManager.ToSvgColor(Color.Red));
            Assert.Equal("#00FF00", SvgStylesheetManager.ToSvgColor(Color.Lime));
            Assert.Equal("#0000FF", SvgStylesheetManager.ToSvgColor(Color.Blue));
            Assert.Equal("#000000", SvgStylesheetManager.ToSvgColor(Color.Black));
            Assert.Equal("#FFFFFF", SvgStylesheetManager.ToSvgColor(Color.White));
        }

        [TestMethod]
        public void ToSvgColor_returns_none_for_transparent()
        {
            Assert.Equal("none", SvgStylesheetManager.ToSvgColor(Color.Transparent));
        }

        [TestMethod]
        public void FormatDouble_uses_invariant_culture()
        {
            Assert.Equal("123.46", SvgStylesheetManager.FormatDouble(123.456));
            Assert.Equal("0.00", SvgStylesheetManager.FormatDouble(0));
            Assert.Equal("-50.50", SvgStylesheetManager.FormatDouble(-50.5));
        }

        [TestMethod]
        public void GetTextColorForFill_returns_white_for_dark_fills()
        {
            Assert.Equal(Color.White, SvgStylesheetManager.GetTextColorForFill(Color.Black));
            Assert.Equal(Color.White, SvgStylesheetManager.GetTextColorForFill(Color.DarkBlue));
            Assert.Equal(Color.White, SvgStylesheetManager.GetTextColorForFill(Color.DarkGreen));
        }

        [TestMethod]
        public void GetTextColorForFill_returns_black_for_light_fills()
        {
            Assert.Equal(Color.Black, SvgStylesheetManager.GetTextColorForFill(Color.White));
            Assert.Equal(Color.Black, SvgStylesheetManager.GetTextColorForFill(Color.Yellow));
            Assert.Equal(Color.Black, SvgStylesheetManager.GetTextColorForFill(Color.LightGray));
        }

        [TestMethod]
        public void GetDashArray_creates_comma_separated_values()
        {
            var pattern = new float[] { 5, 3 };
            Assert.Equal("5.00,3.00", SvgStylesheetManager.GetDashArray(pattern));
        }

        [TestMethod]
        public void GetDashArray_returns_null_for_empty_pattern()
        {
            SvgStylesheetManager.GetDashArray(new float[0].Should().BeNull());
            SvgStylesheetManager.GetDashArray(null.Should().BeNull());
        }

        [TestMethod]
        public void EscapeXml_escapes_special_characters()
        {
            Assert.Equal("&amp;", SvgStylesheetManager.EscapeXml("&"));
            Assert.Equal("&lt;", SvgStylesheetManager.EscapeXml("<"));
            Assert.Equal("&gt;", SvgStylesheetManager.EscapeXml(">"));
            Assert.Equal("&quot;", SvgStylesheetManager.EscapeXml("\""));
            Assert.Equal("&apos;", SvgStylesheetManager.EscapeXml("'"));
        }

        [TestMethod]
        public void EscapeXml_escapes_combined_special_characters()
        {
            Assert.Equal("&lt;div class=&quot;test&quot;&gt;A &amp; B&lt;/div&gt;",
SvgStylesheetManager.EscapeXml("<div class=\"test\">A & B</div>"));
        }

        [TestMethod]
        public void EscapeXml_returns_input_for_null_or_empty()
        {
            SvgStylesheetManager.EscapeXml(null.Should().BeNull());
            Assert.Equal(string.Empty, SvgStylesheetManager.EscapeXml(string.Empty));
        }

        [TestMethod]
        public void EscapeXml_preserves_normal_text()
        {
            Assert.Equal("Hello World", SvgStylesheetManager.EscapeXml("Hello World"));
            Assert.Equal("CustomerOrder", SvgStylesheetManager.EscapeXml("CustomerOrder"));
        }

        [TestMethod]
        public void GetFontFamily_returns_default_for_null()
        {
            Assert.Equal("Segoe UI, Arial, sans-serif", SvgStylesheetManager.GetFontFamily(null));
        }

        [TestMethod]
        public void GetFontSize_returns_default_for_null()
        {
            Assert.Equal("12", SvgStylesheetManager.GetFontSize(null));
        }

        [TestMethod]
        public void GetFontWeight_returns_normal_for_null()
        {
            Assert.Equal("normal", SvgStylesheetManager.GetFontWeight(null));
        }

        [TestMethod]
        public void GetFontStyle_returns_normal_for_null()
        {
            Assert.Equal("normal", SvgStylesheetManager.GetFontStyle(null));
        }

        [TestMethod]
        public void ToSvgColorWithOpacity_handles_semi_transparent_colors()
        {
            var semiTransparent = Color.FromArgb(128, 255, 0, 0);
            string opacity;
            var color = SvgStylesheetManager.ToSvgColorWithOpacity(semiTransparent, out opacity);

            color.Should().Be("#FF0000");
            opacity.Should().NotBeNull();
            opacity.Should().Be("0.50");
        }

        [TestMethod]
        public void ToSvgColorWithOpacity_returns_null_opacity_for_opaque_colors()
        {
            string opacity;
            var color = SvgStylesheetManager.ToSvgColorWithOpacity(Color.Red, out opacity);

            color.Should().Be("#FF0000");
            opacity.Should().BeNull();
        }
    }
}
