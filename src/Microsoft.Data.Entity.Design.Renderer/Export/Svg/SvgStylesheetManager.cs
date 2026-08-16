// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
using System;
using System.Drawing;
using System.Globalization;

namespace Microsoft.Data.Entity.Design.Dsl.View.Export
{

    /// <summary>
    /// Manages CSS stylesheet definitions for SVG export.
    /// Consolidates all CSS class definitions for icons, text, and layout.
    /// </summary>
    /// <remarks>
    /// This type is the single place where diagram values become SVG text, which is why the conversion helpers for
    /// colours, fonts, numbers, and escaping live here alongside the stylesheet itself. Every conversion is done
    /// against <see cref="CultureInfo.InvariantCulture"/> so that an export produced on a machine with a comma
    /// decimal separator is byte-for-byte identical to one produced anywhere else.
    /// </remarks>
    internal class SvgStylesheetManager
    {

        #region Public Methods

        /// <summary>
        /// Gets the consolidated CSS style definitions for the SVG.
        /// Includes icon classes, text classes, and any other shared styles.
        /// </summary>
        /// <returns>The complete &lt;style&gt; element, indented for placement inside the root &lt;svg&gt; element.</returns>
        /// <remarks>
        /// Declaring shape, text, icon, and connector appearance once here rather than as attributes on every
        /// element keeps the exported document small and lets a consumer restyle it without rewriting geometry.
        /// The literal is emitted verbatim, so any edit to it changes the exported bytes.
        /// </remarks>
        public string GetStyleDefinitions()
        {
            return @"    <style>
      /* Icon size classes - use CSS to define sizes once instead of on every element */
      .icon { width: 16px; height: 16px; }
      .icon-sm { width: 14px; height: 14px; }
      /* Icon fill classes */
      .icon-shadow { fill: #212121; opacity: 0.1; }
      .icon-fill { fill: #212121; opacity: 1; }
      .icon-accent-shadow { fill: #ffffff; opacity: 0.1; }
      .icon-accent { fill: #ffffff; opacity: 1; }
      .icon-blue { fill: #005dba; opacity: 1; }
      .icon-muted { opacity: 0.75; }
      /* Text classes */
      .text-base { font-family: Segoe UI, Arial, sans-serif; }
      .text-compartment { font-size: 11px; fill: #000000; }
      .text-header { font-size: 11px; fill: #FFFFFF; }
      .text-entity { font-size: 12px; font-weight: bold; }
      .text-property { font-size: 11px; fill: #000000; }
      .text-mult { font-size: 11px; fill: #000000; font-weight: bold; }
      /* Shape classes */
      .header-compartment { fill: #E0E0E0; height: 24px; }
      /* Connector line styles */
      .line { fill: none; stroke-width: 1.5px; }
      .association { stroke: #778899; stroke-dasharray: 5,3; }
      .inheritance { stroke: #4682B4; }
      /* Marker styles */
      .diamond { fill: #778899; stroke: #778899; }
      .arrow-hollow { fill: none; stroke: #4682B4; stroke-width: 1.5px; }
    </style>
";
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Escapes text for use in SVG.
        /// </summary>
        /// <param name="text">The text to escape.</param>
        /// <returns>The escaped text, or the input unchanged when it is null or empty.</returns>
        /// <remarks>
        /// The ampersand is replaced first so that the escape sequences introduced by the later replacements are
        /// not escaped a second time. Quotes and apostrophes are escaped as well because the same helper is used
        /// for attribute values, where an unescaped quote would terminate the attribute.
        /// </remarks>
        internal static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        /// <summary>
        /// Formats a double value for SVG attributes.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The value rendered with two decimal places using invariant formatting.</returns>
        /// <remarks>
        /// Fixed precision and invariant culture together are what make repeated exports comparable: a
        /// culture-sensitive format would emit a comma decimal separator, which SVG does not accept, and a
        /// variable precision would make otherwise identical documents differ.
        /// </remarks>
        internal static string FormatDouble(double value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates an SVG dash array string for dashed lines.
        /// </summary>
        /// <param name="pattern">The dash lengths, in user units, alternating dash and gap.</param>
        /// <returns>
        /// A comma-separated stroke-dasharray value, or <see langword="null"/> when the pattern is null or empty
        /// so the caller can omit the attribute entirely.
        /// </returns>
        internal static string GetDashArray(float[] pattern)
        {
            if (pattern is null || pattern.Length == 0)
            {
                return null;
            }

            var parts = new string[pattern.Length];
            for (int i = 0; i < pattern.Length; i++)
            {
                parts[i] = FormatDouble(pattern[i]);
            }

            return string.Join(",", parts);
        }

        /// <summary>
        /// Gets font family name for SVG.
        /// </summary>
        /// <param name="font">The font to describe, or null to fall back to the document default.</param>
        /// <returns>A CSS font-family value with a generic fallback appended.</returns>
        /// <remarks>
        /// The generic sans-serif fallback matters because the exported file is usually viewed on machines that do
        /// not have the designer's font installed.
        /// </remarks>
        internal static string GetFontFamily(Font font)
        {
            if (font is null)
            {
                return "Segoe UI, Arial, sans-serif";
            }

            return string.Format(CultureInfo.InvariantCulture, "'{0}', sans-serif", font.FontFamily.Name);
        }

        /// <summary>
        /// Gets font size in pixels for SVG.
        /// </summary>
        /// <param name="font">The font to measure, or null to fall back to the document default.</param>
        /// <returns>The size in pixels as an SVG attribute value.</returns>
        /// <remarks>
        /// SVG lengths are in user units that map to pixels, while a <see cref="Font"/> is measured in points, so
        /// the size is scaled by the ratio between 96 DPI and 72 points per inch.
        /// </remarks>
        internal static string GetFontSize(Font font)
        {
            if (font is null)
            {
                return "12";
            }
            // Convert points to pixels (approximately 1.33x)
            return FormatDouble(font.SizeInPoints * 1.33);
        }

        /// <summary>
        /// Gets font style for SVG.
        /// </summary>
        /// <param name="font">The font to inspect, or null to fall back to the document default.</param>
        /// <returns>"italic" when the font is italic; otherwise "normal".</returns>
        internal static string GetFontStyle(Font font)
        {
            if (font is not null && font.Italic)
            {
                return "italic";
            }

            return "normal";
        }

        /// <summary>
        /// Gets font weight for SVG.
        /// </summary>
        /// <param name="font">The font to inspect, or null to fall back to the document default.</param>
        /// <returns>"bold" when the font is bold; otherwise "normal".</returns>
        internal static string GetFontWeight(Font font)
        {
            if (font is not null && font.Bold)
            {
                return "bold";
            }

            return "normal";
        }

        /// <summary>
        /// Gets the appropriate text color (black or white) based on the fill color brightness.
        /// </summary>
        /// <param name="fillColor">The color the text will be drawn on top of.</param>
        /// <returns><see cref="Color.Black"/> over light fills and <see cref="Color.White"/> over dark ones.</returns>
        /// <remarks>
        /// Entity header colors come from the model and can be anything, so the label color has to be chosen at
        /// export time to stay readable rather than being fixed in the stylesheet.
        /// </remarks>
        internal static Color GetTextColorForFill(Color fillColor)
        {
            var brightness = GetRelativeBrightness(fillColor);

            return brightness > 0.5 ? Color.Black : Color.White;
        }

        /// <summary>
        /// Converts a Color to an SVG hex color string.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>An uppercase six-digit hex value, or "none" for <see cref="Color.Transparent"/>.</returns>
        /// <remarks>
        /// The alpha channel is discarded; use <see cref="ToSvgColorWithOpacity"/> when it needs to be preserved.
        /// </remarks>
        internal static string ToSvgColor(Color color)
        {
            if (color == Color.Transparent)
            {
                return "none";
            }

            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        /// <summary>
        /// Converts a Color to an SVG color with opacity attribute.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <param name="opacity">
        /// Receives the opacity as an SVG attribute value, or <see langword="null"/> when the color is fully
        /// opaque and the caller should omit the attribute.
        /// </param>
        /// <returns>An uppercase six-digit hex value, or "none" for <see cref="Color.Transparent"/>.</returns>
        /// <remarks>
        /// SVG carries alpha in a separate fill-opacity or stroke-opacity attribute rather than in the color
        /// value, which is why the opacity is returned out of band instead of being folded into the string.
        /// </remarks>
        internal static string ToSvgColorWithOpacity(Color color, out string opacity)
        {
            if (color == Color.Transparent)
            {
                opacity = "0";
                return "none";
            }

            opacity = color.A < 255
                ? (color.A / 255.0).ToString("F2", CultureInfo.InvariantCulture)
                : null;

            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the relative brightness of a color (0 = dark, 1 = bright).
        /// </summary>
        /// <param name="color">The color to measure.</param>
        /// <returns>The relative luminance, from 0 for black to 1 for white.</returns>
        /// <remarks>
        /// This is the WCAG relative luminance formula. The channel weights reflect how much each primary
        /// contributes to perceived brightness, so a saturated blue is correctly treated as dark even though its
        /// raw component value is high.
        /// </remarks>
        private static double GetRelativeBrightness(Color color)
        {
            return 0.2126 * GetRelativeColorPart(color.R)
                   + 0.7152 * GetRelativeColorPart(color.G)
                   + 0.0722 * GetRelativeColorPart(color.B);
        }

        /// <summary>
        /// Linearizes a single sRGB color channel for use in a luminance calculation.
        /// </summary>
        /// <param name="colorPart">The raw channel value, from 0 to 255.</param>
        /// <returns>The linearized channel value, from 0 to 1.</returns>
        /// <remarks>
        /// sRGB channel values are gamma encoded, so they must be converted to linear light before being weighted
        /// and summed. The piecewise form matches the WCAG definition: a short linear segment near black avoids
        /// the infinite slope of the power curve at zero.
        /// </remarks>
        private static double GetRelativeColorPart(byte colorPart)
        {
            var part = colorPart / 255.0;

            return part <= 0.03928 ? part / 12.92 : Math.Pow((part + 0.055) / 1.055, 2.4);
        }

        #endregion

    }

}
