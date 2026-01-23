// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
    using System;
    using System.Collections;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Renders EntityTypeShape elements to SVG format.
    /// </summary>
    internal class SvgShapeRenderer
    {
        private const double CornerRadius = 3.0;
        private const double HeaderHeight = 30.0;
        private const double PropertyRowHeight = 18.0;
        private const double CompartmentHeaderHeight = 24.0;
        private const double Padding = 8.0;
        private const double IconSize = 16.0;           // .icon { width: 16px } in CSS
        private const double SmallIconSize = 14.0;      // .icon-sm { width: 14px } in CSS
        private const double PropertyIconSize = 14.0;   // Uses .icon-sm CSS class
        private const double PropertyIconWidth = 20.0;
        private const double WidthBuffer = 1.15;        // 15% buffer for font variations

        private const string FontFamily = "Segoe UI";
        private const float HeaderFontSize = 12f;
        private const float PropertyFontSize = 11f;

        // Font sizes in pixels - must match CSS in SvgStylesheetManager
        // .text-entity { font-size: 12px }
        // .text-compartment, .text-property { font-size: 11px }
        private const double EntityFontSizePx = 12.0;
        private const double CompartmentFontSizePx = 11.0;

        // Cap height ratio for Segoe UI (visual center of text relative to baseline)
        private const double CapHeightRatio = 0.70;

        private readonly SvgIconManager _iconManager;

        /// <summary>
        /// Initializes a new instance of SvgShapeRenderer with an icon manager.
        /// </summary>
        public SvgShapeRenderer(SvgIconManager iconManager)
        {
            _iconManager = iconManager;
        }

        /// <summary>
        /// Calculates Y position to vertically center an icon within a container.
        /// </summary>
        /// <param name="containerY">Top Y coordinate of the container.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="iconSize">Size of the icon (width/height).</param>
        /// <returns>Y position for the icon.</returns>
        internal static double GetCenteredIconY(double containerY, double containerHeight, double iconSize)
        {
            return containerY + (containerHeight - iconSize) / 2.0;
        }

        /// <summary>
        /// Calculates text baseline Y position to vertically center text within a container.
        /// The visual center of capital letters is approximately (capHeight/2) above the baseline.
        /// </summary>
        /// <param name="containerY">Top Y coordinate of the container.</param>
        /// <param name="containerHeight">Height of the container.</param>
        /// <param name="fontSizePx">Font size in pixels.</param>
        /// <returns>Y position for the text baseline.</returns>
        internal static double GetCenteredTextBaselineY(double containerY, double containerHeight, double fontSizePx)
        {
            var capHeight = fontSizePx * CapHeightRatio;
            return containerY + (containerHeight / 2.0) + (capHeight / 2.0);
        }

        /// <summary>
        /// Renders an EntityTypeShape to SVG elements.
        /// </summary>
        internal string RenderShape(EntityTypeShape shape, double offsetX, double offsetY)
        {
            return RenderShape(shape, offsetX, offsetY, transparentBackground: true, showTypes: true);
        }

        /// <summary>
        /// Renders an EntityTypeShape to SVG elements using DiagramExportOptions.
        /// </summary>
        /// <param name="shape">The shape to render.</param>
        /// <param name="offsetX">X offset for positioning.</param>
        /// <param name="offsetY">Y offset for positioning.</param>
        /// <param name="options">The export options specifying settings.</param>
        internal string RenderShape(EntityTypeShape shape, double offsetX, double offsetY, DiagramExportOptions options)
        {
            if (shape == null || shape.ModelElement == null)
            {
                return string.Empty;
            }

            var bounds = shape.AbsoluteBounds;
            var x = (bounds.X * 96) - offsetX; // Convert inches to pixels
            var y = (bounds.Y * 96) - offsetY;
            var originalWidth = bounds.Width * 96;
            var height = bounds.Height * 96;

            var fillColor = shape.FillColor;
            var textColor = SvgStylesheetManager.GetTextColorForFill(fillColor);
            var outlineColor = GetOutlineColor(fillColor);

            // Compartment background color (light mode style for entity internals)
            var compartmentBgColor = Color.White;

            var entityType = shape.ModelElement as EntityType;
            var entityName = entityType?.Name ?? "Entity";

            // Calculate required width based on text content, using UseOriginalDimensions optimization
            var width = CalculateRequiredWidth(shape, entityName, options, originalWidth);

            var sb = new StringBuilder();

            // Start group for the entity
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "  <g id=\"entity_{0}\" class=\"entity-shape\">\n",
                SvgStylesheetManager.EscapeXml(entityName.Replace(" ", "_")));

            // Main body rectangle with rounded corners
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{4}\" " +
                "fill=\"{5}\" stroke=\"{6}\" stroke-width=\"1\"/>\n",
                SvgStylesheetManager.FormatDouble(x),
                SvgStylesheetManager.FormatDouble(y),
                SvgStylesheetManager.FormatDouble(width),
                SvgStylesheetManager.FormatDouble(height),
                SvgStylesheetManager.FormatDouble(CornerRadius),
                SvgStylesheetManager.ToSvgColor(fillColor),
                SvgStylesheetManager.ToSvgColor(outlineColor));

            // Header background (slightly rounded top corners)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <path d=\"M {0} {1} L {2} {1} L {2} {3} L {0} {3} Z\" fill=\"{4}\" fill-opacity=\"0.2\"/>\n",
                SvgStylesheetManager.FormatDouble(x + CornerRadius),
                SvgStylesheetManager.FormatDouble(y),
                SvgStylesheetManager.FormatDouble(x + width - CornerRadius),
                SvgStylesheetManager.FormatDouble(y + HeaderHeight),
                SvgStylesheetManager.ToSvgColor(Color.Black));

            // Entity name in header (vertically centered)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <text x=\"{0}\" y=\"{1}\" fill=\"{2}\" class=\"text-base text-entity\">{3}</text>\n",
                SvgStylesheetManager.FormatDouble(x + Padding + IconSize + 4),
                SvgStylesheetManager.FormatDouble(GetCenteredTextBaselineY(y, HeaderHeight, EntityFontSizePx)),
                SvgStylesheetManager.ToSvgColor(textColor),
                SvgStylesheetManager.EscapeXml(entityName));

            // Entity icon (vertically centered)
            RenderEntityIcon(sb, x + Padding, GetCenteredIconY(y, HeaderHeight, IconSize), textColor);

            // Render properties if shape is expanded
            if (shape.IsExpanded)
            {
                RenderCompartments(sb, shape, x, y + HeaderHeight, width, textColor, fillColor, compartmentBgColor, options.ShowTypes);
            }

            sb.AppendLine("  </g>");

            return sb.ToString();
        }

        /// <summary>
        /// Renders an EntityTypeShape to SVG elements with export options.
        /// </summary>
        /// <param name="shape">The shape to render.</param>
        /// <param name="offsetX">X offset for positioning.</param>
        /// <param name="offsetY">Y offset for positioning.</param>
        /// <param name="transparentBackground">If true, renders with transparent compartment backgrounds.</param>
        /// <param name="showTypes">If true, shows data types alongside property names.</param>
        internal string RenderShape(EntityTypeShape shape, double offsetX, double offsetY, bool transparentBackground, bool showTypes)
        {
            if (shape == null || shape.ModelElement == null)
            {
                return string.Empty;
            }

            var bounds = shape.AbsoluteBounds;
            var x = (bounds.X * 96) - offsetX; // Convert inches to pixels
            var y = (bounds.Y * 96) - offsetY;
            var originalWidth = bounds.Width * 96;
            var height = bounds.Height * 96;

            var fillColor = shape.FillColor;
            var textColor = SvgStylesheetManager.GetTextColorForFill(fillColor);
            var outlineColor = GetOutlineColor(fillColor);

            // Compartment background color (light mode style for entity internals)
            var compartmentBgColor = Color.White;

            var entityType = shape.ModelElement as EntityType;
            var entityName = entityType?.Name ?? "Entity";

            // Calculate required width based on text content
            var width = CalculateRequiredWidth(shape, entityName, showTypes, originalWidth);

            var sb = new StringBuilder();

            // Start group for the entity
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "  <g id=\"entity_{0}\" class=\"entity-shape\">\n",
                SvgStylesheetManager.EscapeXml(entityName.Replace(" ", "_")));

            // Main body rectangle with rounded corners
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" rx=\"{4}\" " +
                "fill=\"{5}\" stroke=\"{6}\" stroke-width=\"1\"/>\n",
                SvgStylesheetManager.FormatDouble(x),
                SvgStylesheetManager.FormatDouble(y),
                SvgStylesheetManager.FormatDouble(width),
                SvgStylesheetManager.FormatDouble(height),
                SvgStylesheetManager.FormatDouble(CornerRadius),
                SvgStylesheetManager.ToSvgColor(fillColor),
                SvgStylesheetManager.ToSvgColor(outlineColor));

            // Header background (slightly rounded top corners)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <path d=\"M {0} {1} L {2} {1} L {2} {3} L {0} {3} Z\" fill=\"{4}\" fill-opacity=\"0.2\"/>\n",
                SvgStylesheetManager.FormatDouble(x + CornerRadius),
                SvgStylesheetManager.FormatDouble(y),
                SvgStylesheetManager.FormatDouble(x + width - CornerRadius),
                SvgStylesheetManager.FormatDouble(y + HeaderHeight),
                SvgStylesheetManager.ToSvgColor(Color.Black));

            // Entity name in header (vertically centered)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <text x=\"{0}\" y=\"{1}\" fill=\"{2}\" class=\"text-base text-entity\">{3}</text>\n",
                SvgStylesheetManager.FormatDouble(x + Padding + IconSize + 4),
                SvgStylesheetManager.FormatDouble(GetCenteredTextBaselineY(y, HeaderHeight, EntityFontSizePx)),
                SvgStylesheetManager.ToSvgColor(textColor),
                SvgStylesheetManager.EscapeXml(entityName));

            // Entity icon (vertically centered)
            RenderEntityIcon(sb, x + Padding, GetCenteredIconY(y, HeaderHeight, IconSize), textColor);

            // Render properties if shape is expanded
            if (shape.IsExpanded)
            {
                RenderCompartments(sb, shape, x, y + HeaderHeight, width, textColor, fillColor, compartmentBgColor, showTypes);
            }

            sb.AppendLine("  </g>");

            return sb.ToString();
        }

        internal void RenderEntityIcon(StringBuilder sb, double x, double y, Color color)
        {
            // Use the Class icon from the icon manager
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    {0}\n",
                _iconManager.GetIconReference("Class", x, y));
        }

        /// <summary>
        /// Renders a compartment section header with expand/collapse icon and label.
        /// </summary>
        internal void RenderCompartmentHeader(StringBuilder sb, string headerText, bool isExpanded,
            double x, double y, double width)
        {
            const double headerHeight = CompartmentHeaderHeight;
            const double chevronSize = SmallIconSize; // 14px to match .icon-sm CSS class
            const double chevronPadding = 2.0;

            // Header background
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <rect x=\"{0}\" y=\"{1}\" width=\"{2}\" class=\"header-compartment\"/>\n",
                SvgStylesheetManager.FormatDouble(x),
                SvgStylesheetManager.FormatDouble(y),
                SvgStylesheetManager.FormatDouble(width));

            // Expand/Collapse chevron icon (vertically centered)
            var chevronIcon = isExpanded ? "Collapse" : "Expand";
            var chevronX = x + chevronPadding;
            var chevronY = GetCenteredIconY(y, headerHeight, chevronSize);
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    {0}\n",
                _iconManager.GetIconReference(chevronIcon, chevronX, chevronY, small: true));

            // Header text (vertically centered)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <text x=\"{0}\" y=\"{1}\" class=\"text-base text-compartment\">{2}</text>\n",
                SvgStylesheetManager.FormatDouble(x + chevronSize + 6),
                SvgStylesheetManager.FormatDouble(GetCenteredTextBaselineY(y, headerHeight, CompartmentFontSizePx)),
                SvgStylesheetManager.EscapeXml(headerText));
        }

        internal void RenderCompartments(StringBuilder sb, EntityTypeShape shape, double x, double startY,
            double width, Color textColor, Color fillColor, Color compartmentBgColor, bool showTypes)
        {
            var currentY = startY;
            var separatorColor = Color.Black;

            // Properties compartment
            var propertiesCompartment = shape.PropertiesCompartment;
            if (propertiesCompartment != null && propertiesCompartment.Items.Count > 0)
            {
                // Render "Properties" section header
                RenderCompartmentHeader(sb, "Properties", propertiesCompartment.IsExpanded, x, currentY, width);
                currentY += CompartmentHeaderHeight;

                // Render property items (only if expanded)
                if (propertiesCompartment.IsExpanded)
                {
                    currentY = RenderCompartmentItems(
                        sb, propertiesCompartment.Items,
                        x, currentY, width, compartmentBgColor, true, showTypes, separatorColor);
                }
            }

            // Navigation properties compartment
            var navigationCompartment = shape.NavigationCompartment;
            if (navigationCompartment != null && navigationCompartment.Items.Count > 0)
            {
                // Render "Navigation Properties" section header
                RenderCompartmentHeader(sb, "Navigation Properties", navigationCompartment.IsExpanded, x, currentY, width);
                currentY += CompartmentHeaderHeight;

                // Render navigation items (only if expanded)
                if (navigationCompartment.IsExpanded)
                {
                    RenderCompartmentItems(
                        sb, navigationCompartment.Items,
                        x, currentY, width, compartmentBgColor, false, showTypes, separatorColor);
                }
            }
        }

        internal double RenderCompartmentItems(StringBuilder sb,
            IList items, double x, double startY, double width,
            Color fillColor, bool isScalarProperties, bool showTypes, Color separatorColor)
        {
            var currentY = startY;

            // Calculate compartment height for background
            var compartmentHeight = (items.Count * PropertyRowHeight) + 4;

            // Compartment background fill
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"{4}\"/>\n",
                SvgStylesheetManager.FormatDouble(x),
                SvgStylesheetManager.FormatDouble(currentY),
                SvgStylesheetManager.FormatDouble(width),
                SvgStylesheetManager.FormatDouble(compartmentHeight),
                SvgStylesheetManager.ToSvgColor(fillColor));

            // Compartment separator line
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <line x1=\"{0}\" y1=\"{1}\" x2=\"{2}\" y2=\"{1}\" stroke=\"{3}\" stroke-width=\"1\" stroke-opacity=\"0.3\"/>\n",
                SvgStylesheetManager.FormatDouble(x),
                SvgStylesheetManager.FormatDouble(currentY),
                SvgStylesheetManager.FormatDouble(x + width),
                SvgStylesheetManager.ToSvgColor(separatorColor));

            currentY += 2;

            // Render each property item
            foreach (var item in items)
            {
                var propertyText = GetPropertyDisplayText(item, isScalarProperties, showTypes);

                // Property icon (vertically centered)
                var iconX = x + 4;
                var iconY = GetCenteredIconY(currentY, PropertyRowHeight, PropertyIconSize);
                RenderPropertyIcon(sb, iconX, iconY, item);

                // Property text (vertically centered)
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "    <text x=\"{0}\" y=\"{1}\" class=\"text-base text-property\">{2}</text>\n",
                    SvgStylesheetManager.FormatDouble(x + 24),
                    SvgStylesheetManager.FormatDouble(GetCenteredTextBaselineY(currentY, PropertyRowHeight, CompartmentFontSizePx)),
                    SvgStylesheetManager.EscapeXml(propertyText));

                currentY += PropertyRowHeight;
            }

            return currentY;
        }

        internal void RenderPropertyIcon(StringBuilder sb, double x, double y, object item)
        {
            string iconName;

            // Determine the appropriate icon based on property type
            var scalarProp = item as ScalarProperty;
            if (scalarProp != null)
            {
                // Use PropertyKey icon for key properties, Property icon otherwise
                iconName = scalarProp.EntityKey ? "PropertyKey" : "Property";
            }
            else if (item is ComplexProperty)
            {
                // Use ComplexProperty icon
                iconName = "ComplexProperty";
            }
            else if (item is NavigationProperty)
            {
                // Use NavigationProperty icon
                iconName = "NavigationProperty";
            }
            else
            {
                // Default to Property icon
                iconName = "Property";
            }

            // Use small: true for property icons (14x14 via CSS class)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    {0}\n",
                _iconManager.GetIconReference(iconName, x, y, small: true));
        }

        internal string GetPropertyDisplayText(object item, bool isScalarProperty, bool showTypes)
        {
            if (isScalarProperty)
            {
                var scalarProp = item as ScalarProperty;
                if (scalarProp != null)
                {
                    if (showTypes)
                    {
                        return string.Format(CultureInfo.InvariantCulture, "{0} : {1}", scalarProp.Name, scalarProp.Type);
                    }
                    return scalarProp.Name;
                }

                var complexProp = item as ComplexProperty;
                if (complexProp != null)
                {
                    // ComplexProperty type is determined by its relationship; just display the name
                    return complexProp.Name;
                }
            }
            else
            {
                var navProp = item as NavigationProperty;
                if (navProp != null)
                {
                    return navProp.Name;
                }
            }

            var namedItem = item as NameableItem;
            return namedItem?.Name ?? "Property";
        }

        internal Color GetOutlineColor(Color fillColor)
        {
            // Create a slightly darker/lighter version for the outline
            var hsl = RgbToHsl(fillColor);
            hsl.L = hsl.L < 0.5 ? hsl.L + 0.15 : hsl.L - 0.15;
            return HslToRgb(hsl);
        }

        private struct HslColor
        {
            public double H, S, L;
        }

        private HslColor RgbToHsl(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            var hsl = new HslColor { L = (max + min) / 2 };

            if (Math.Abs(max - min) < 0.0001)
            {
                hsl.H = hsl.S = 0;
            }
            else
            {
                double d = max - min;
                hsl.S = hsl.L > 0.5 ? d / (2 - max - min) : d / (max + min);

                if (Math.Abs(max - r) < 0.0001)
                {
                    hsl.H = (g - b) / d + (g < b ? 6 : 0);
                }
                else if (Math.Abs(max - g) < 0.0001)
                {
                    hsl.H = (b - r) / d + 2;
                }
                else
                {
                    hsl.H = (r - g) / d + 4;
                }
                hsl.H /= 6;
            }

            return hsl;
        }

        private Color HslToRgb(HslColor hsl)
        {
            double r, g, b;

            if (Math.Abs(hsl.S) < 0.0001)
            {
                r = g = b = hsl.L;
            }
            else
            {
                double q = hsl.L < 0.5 ? hsl.L * (1 + hsl.S) : hsl.L + hsl.S - hsl.L * hsl.S;
                double p = 2 * hsl.L - q;
                r = HueToRgb(p, q, hsl.H + 1.0 / 3);
                g = HueToRgb(p, q, hsl.H);
                b = HueToRgb(p, q, hsl.H - 1.0 / 3);
            }

            return Color.FromArgb(
                (int)Math.Round(r * 255),
                (int)Math.Round(g * 255),
                (int)Math.Round(b * 255));
        }

        private double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }

        /// <summary>
        /// Calculates the required width for the entity shape based on text content.
        /// Uses UseOriginalDimensions optimization when export settings match diagram settings.
        /// </summary>
        /// <param name="shape">The entity shape.</param>
        /// <param name="entityName">The entity name.</param>
        /// <param name="options">The export options.</param>
        /// <param name="originalWidth">The original shape width from the diagram.</param>
        /// <returns>The calculated width.</returns>
        internal double CalculateRequiredWidth(EntityTypeShape shape, string entityName, DiagramExportOptions options, double originalWidth)
        {
            // If export settings match diagram settings, use original width without recalculation
            if (options.UseOriginalDimensions)
            {
                return originalWidth;
            }

            // Otherwise, calculate based on the showTypes setting
            return CalculateRequiredWidth(shape, entityName, options.ShowTypes, originalWidth);
        }

        /// <summary>
        /// Calculates the required width for the entity shape based on text content.
        /// </summary>
        internal double CalculateRequiredWidth(EntityTypeShape shape, string entityName, bool showTypes, double originalWidth)
        {
            if (!showTypes) return originalWidth;
            var maxTextWidth = 0.0;

            // Measure entity name (bold header)
            var headerTextWidth = MeasureTextWidth(entityName, HeaderFontSize, bold: true);
            var headerRequiredWidth = headerTextWidth + Padding + IconSize + Padding;
            maxTextWidth = Math.Max(maxTextWidth, headerRequiredWidth);

            // Measure property texts if shape is expanded
            if (shape.IsExpanded)
            {
                // Scalar/Complex properties
                var propertiesCompartment = shape.PropertiesCompartment;
                if (propertiesCompartment != null)
                {
                    foreach (var item in propertiesCompartment.Items)
                    {
                        var propertyText = GetPropertyDisplayText(item, isScalarProperty: true, showTypes: showTypes);
                        var textWidth = MeasureTextWidth(propertyText, PropertyFontSize, bold: false);
                        var rowWidth = textWidth + PropertyIconWidth + Padding * 2;
                        maxTextWidth = Math.Max(maxTextWidth, rowWidth);
                    }
                }

                // Navigation properties
                var navigationCompartment = shape.NavigationCompartment;
                if (navigationCompartment != null)
                {
                    foreach (var item in navigationCompartment.Items)
                    {
                        var propertyText = GetPropertyDisplayText(item, isScalarProperty: false, showTypes: showTypes);
                        var textWidth = MeasureTextWidth(propertyText, PropertyFontSize, bold: false);
                        var rowWidth = textWidth + PropertyIconWidth + Padding * 2;
                        maxTextWidth = Math.Max(maxTextWidth, rowWidth);
                    }
                }
            }

            // Apply buffer for font variations and return the larger of calculated or original
            var calculatedWidth = maxTextWidth * WidthBuffer;
            return Math.Max(calculatedWidth, originalWidth);
        }

        /// <summary>
        /// Measures the width of text using GDI+.
        /// </summary>
        private static double MeasureTextWidth(string text, float fontSize, bool bold)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            using (var bmp = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bmp))
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                var fontStyle = bold ? FontStyle.Bold : FontStyle.Regular;
                using (var font = new Font(FontFamily, fontSize, fontStyle, GraphicsUnit.Point))
                {
                    var size = graphics.MeasureString(text, font);
                    return size.Width;
                }
            }
        }
    }
}
