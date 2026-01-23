// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.Modeling.Diagrams;

    /// <summary>
    /// Exports an EntityDesignerDiagram to SVG format.
    /// </summary>
    internal class SvgExporter
    {
        private const double Padding = 20.0;
        private readonly SvgShapeRenderer _shapeRenderer;
        private readonly SvgConnectorRenderer _connectorRenderer;
        private readonly SvgIconManager _iconManager;
        private readonly SvgStylesheetManager _stylesheetManager;

        public SvgExporter()
        {
            _iconManager = new SvgIconManager();
            _stylesheetManager = new SvgStylesheetManager();
            _shapeRenderer = new SvgShapeRenderer(_iconManager);
            _connectorRenderer = new SvgConnectorRenderer();
        }

        /// <summary>
        /// Exports the diagram to an SVG file using DiagramExportOptions.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options specifying path and settings.</param>
        public void Export(EntityDesignerDiagram diagram, DiagramExportOptions options)
        {
            if (diagram is null)
            {
                throw new ArgumentNullException("diagram");
            }

            if (options is null)
            {
                throw new ArgumentNullException("options");
            }

            var svgContent = GenerateSvg(diagram, options);
            File.WriteAllText(options.FilePath, svgContent, Encoding.UTF8);
        }

        /// <summary>
        /// Exports the diagram to an SVG file.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="filePath">The path to save the SVG file.</param>
        public void ExportToSvg(EntityDesignerDiagram diagram, string filePath)
        {
            ExportToSvg(diagram, filePath, transparentBackground: true, showTypes: true);
        }

        /// <summary>
        /// Exports the diagram to an SVG file with export options.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="filePath">The path to save the SVG file.</param>
        /// <param name="transparentBackground">If true, renders with a transparent background.</param>
        /// <param name="showTypes">If true, shows data types alongside property names.</param>
        public void ExportToSvg(EntityDesignerDiagram diagram, string filePath, bool transparentBackground, bool showTypes)
        {
            if (diagram is null)
            {
                throw new ArgumentNullException("diagram");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            var svgContent = GenerateSvg(diagram, transparentBackground, showTypes);
            File.WriteAllText(filePath, svgContent, Encoding.UTF8);
        }

        /// <summary>
        /// Generates SVG content for the diagram.
        /// </summary>
        public string GenerateSvg(EntityDesignerDiagram diagram)
        {
            return GenerateSvg(diagram, transparentBackground: true, showTypes: true);
        }

        /// <summary>
        /// Generates SVG content for the diagram using DiagramExportOptions.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options specifying settings.</param>
        public string GenerateSvg(EntityDesignerDiagram diagram, DiagramExportOptions options)
        {
            if (diagram is null)
            {
                throw new ArgumentNullException("diagram");
            }

            if (options is null)
            {
                throw new ArgumentNullException("options");
            }

            // Reset icon tracking for this export
            _iconManager.ResetUsedIcons();

            var sb = new StringBuilder();

            // Calculate diagram bounds
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (ShapeElement shape in diagram.NestedChildShapes)
            {
                var nodeShape = shape as NodeShape;
                if (nodeShape != null)
                {
                    var bounds = nodeShape.AbsoluteBounds;
                    // Convert inches to pixels (96 DPI)
                    var x = bounds.X * 96;
                    var y = bounds.Y * 96;
                    var right = (bounds.X + bounds.Width) * 96;
                    var bottom = (bounds.Y + bounds.Height) * 96;

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, right);
                    maxY = Math.Max(maxY, bottom);
                }
            }

            // Handle empty diagrams
            if (minX == double.MaxValue)
            {
                minX = 0;
                minY = 0;
                maxX = 800;
                maxY = 600;
            }

            // Calculate SVG dimensions with padding
            var offsetX = minX - Padding;
            var offsetY = minY - Padding;
            var width = (maxX - minX) + (Padding * 2);
            var height = (maxY - minY) + (Padding * 2);

            // SVG header
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<svg xmlns=\"http://www.w3.org/2000/svg\" " +
                "xmlns:xlink=\"http://www.w3.org/1999/xlink\" " +
                "viewBox=\"0 0 {0} {1}\" " +
                "width=\"{0}\" height=\"{1}\">\n",
                SvgStylesheetManager.FormatDouble(width),
                SvgStylesheetManager.FormatDouble(height));

            // Title
            var diagramTitle = diagram.Title ?? "Entity Diagram";
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "  <title>{0}</title>\n",
                SvgStylesheetManager.EscapeXml(diagramTitle));

            // Pre-render all content to track which icons are used
            var contentBuilder = new StringBuilder();

            // Render all connectors first (so they appear behind shapes)
            foreach (ShapeElement shape in diagram.NestedChildShapes)
            {
                if (shape is AssociationConnector associationConnector)
                {
                    contentBuilder.Append(_connectorRenderer.RenderAssociationConnector(associationConnector, offsetX, offsetY));
                    continue;
                }

                if (shape is InheritanceConnector inheritanceConnector)
                {
                    contentBuilder.Append(_connectorRenderer.RenderInheritanceConnector(inheritanceConnector, offsetX, offsetY));
                }
            }

            // Render all entity shapes
            foreach (ShapeElement shape in diagram.NestedChildShapes)
            {
                if (shape is EntityTypeShape entityTypeShape)
                {
                    contentBuilder.Append(_shapeRenderer.RenderShape(entityTypeShape, offsetX, offsetY, options));
                }
            }

            // Now add defs with markers and used icon symbols
            sb.AppendLine("  <defs>");

            // Add CSS styles
            sb.Append(_stylesheetManager.GetStyleDefinitions());

            // Add marker definitions for connectors
            sb.Append(_connectorRenderer.GetMarkerDefinitionsContent());

            // Add icon symbol definitions (only those that were used)
            sb.Append(_iconManager.GetUsedSymbolDefinitions());

            sb.AppendLine("  </defs>");

            // Background (only render if not transparent)
            if (!options.TransparentBackground)
            {
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "  <rect x=\"0\" y=\"0\" width=\"{0}\" height=\"{1}\" fill=\"white\"/>\n",
                    SvgStylesheetManager.FormatDouble(width),
                    SvgStylesheetManager.FormatDouble(height));
            }

            // Add the pre-rendered content
            sb.Append(contentBuilder);

            // SVG footer
            sb.AppendLine("</svg>");

            return sb.ToString();
        }

        /// <summary>
        /// Generates SVG content for the diagram with export options.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="transparentBackground">If true, renders with a transparent background.</param>
        /// <param name="showTypes">If true, shows data types alongside property names.</param>
        public string GenerateSvg(EntityDesignerDiagram diagram, bool transparentBackground, bool showTypes)
        {
            if (diagram is null)
            {
                throw new ArgumentNullException("diagram");
            }

            // Reset icon tracking for this export
            _iconManager.ResetUsedIcons();

            var sb = new StringBuilder();

            // Calculate diagram bounds
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (ShapeElement shape in diagram.NestedChildShapes)
            {
                if (shape is NodeShape nodeShape)
                {
                    var bounds = nodeShape.AbsoluteBounds;
                    // Convert inches to pixels (96 DPI)
                    var x = bounds.X * 96;
                    var y = bounds.Y * 96;
                    var right = (bounds.X + bounds.Width) * 96;
                    var bottom = (bounds.Y + bounds.Height) * 96;

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, right);
                    maxY = Math.Max(maxY, bottom);
                }
            }

            // Handle empty diagrams
            if (minX == double.MaxValue)
            {
                minX = 0;
                minY = 0;
                maxX = 800;
                maxY = 600;
            }

            // Calculate SVG dimensions with padding
            var offsetX = minX - Padding;
            var offsetY = minY - Padding;
            var width = (maxX - minX) + (Padding * 2);
            var height = (maxY - minY) + (Padding * 2);

            // SVG header
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<svg xmlns=\"http://www.w3.org/2000/svg\" " +
                "xmlns:xlink=\"http://www.w3.org/1999/xlink\" " +
                "viewBox=\"0 0 {0} {1}\" " +
                "width=\"{0}\" height=\"{1}\">\n",
                SvgStylesheetManager.FormatDouble(width),
                SvgStylesheetManager.FormatDouble(height));

            // Title
            var diagramTitle = diagram.Title ?? "Entity Diagram";
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "  <title>{0}</title>\n",
                SvgStylesheetManager.EscapeXml(diagramTitle));

            // Pre-render all content to track which icons are used
            var contentBuilder = new StringBuilder();

            // Render all connectors first (so they appear behind shapes)
            foreach (ShapeElement shape in diagram.NestedChildShapes)
            {
                if (shape is AssociationConnector associationConnector)
                {
                    contentBuilder.Append(_connectorRenderer.RenderAssociationConnector(associationConnector, offsetX, offsetY));
                    continue;
                }

                if (shape is InheritanceConnector inheritanceConnector)
                {
                    contentBuilder.Append(_connectorRenderer.RenderInheritanceConnector(inheritanceConnector, offsetX, offsetY));
                }
            }

            // Render all entity shapes
            foreach (ShapeElement shape in diagram.NestedChildShapes)
            {
                if (shape is EntityTypeShape entityTypeShape)
                {
                    contentBuilder.Append(_shapeRenderer.RenderShape(entityTypeShape, offsetX, offsetY, transparentBackground, showTypes));
                }
            }

            // Now add defs with markers and used icon symbols
            sb.AppendLine("  <defs>");

            // Add CSS styles
            sb.Append(_stylesheetManager.GetStyleDefinitions());

            // Add marker definitions for connectors
            sb.Append(_connectorRenderer.GetMarkerDefinitionsContent());

            // Add icon symbol definitions (only those that were used)
            sb.Append(_iconManager.GetUsedSymbolDefinitions());

            sb.AppendLine("  </defs>");

            // Background (only render if not transparent)
            if (!transparentBackground)
            {
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "  <rect x=\"0\" y=\"0\" width=\"{0}\" height=\"{1}\" fill=\"white\"/>\n",
                    SvgStylesheetManager.FormatDouble(width),
                    SvgStylesheetManager.FormatDouble(height));
            }

            // Add the pre-rendered content
            sb.Append(contentBuilder);

            // SVG footer
            sb.AppendLine("</svg>");

            return sb.ToString();
        }
    }
}
