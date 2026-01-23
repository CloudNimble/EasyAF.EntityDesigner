// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Text;
    using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
    using Microsoft.VisualStudio.Modeling.Diagrams;

    /// <summary>
    /// Renders Association and Inheritance connectors to SVG format.
    /// </summary>
    internal class SvgConnectorRenderer
    {
        private const double ArrowSize = 8.0;
        private const double DiamondSize = 6.0;

        // Offset to pull connector endpoints away from entity boundary so markers are fully visible
        // This should be at least half the marker size to ensure the marker doesn't overlap the entity
        private const double EndpointOffset = 4.0;

        // Diagonal offset distances for multiplicity labels (matching VS designer visual appearance)
        private const double MultLabelOffsetX = 8.0;
        private const double MultLabelOffsetY = 8.0;

        // Edge detection tolerance for floating point comparison
        private const double EdgeDetectionTolerance = 2.0;

        /// <summary>
        /// Represents which edge of an entity shape a connector endpoint attaches to.
        /// </summary>
        private enum ConnectorEdge
        {
            Top,
            Bottom,
            Left,
            Right
        }

        private static readonly Color AssociationColor = Color.FromArgb(119, 136, 153); // LightSlateGray
        private static readonly Color InheritanceColor = Color.FromArgb(70, 130, 180); // SteelBlue
        private static readonly float[] AssociationDashPattern = new float[] { 5, 3 };

        /// <summary>
        /// Renders an AssociationConnector to SVG elements.
        /// </summary>
        internal string RenderAssociationConnector(AssociationConnector connector, double offsetX, double offsetY)
        {
            if (connector == null || connector.ModelElement == null)
            {
                return string.Empty;
            }

            var association = connector.ModelElement as Association;
            if (association == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var connectorId = string.Format(
                CultureInfo.InvariantCulture,
                "assoc_{0}_{1}",
                SvgStylesheetManager.EscapeXml(association.SourceEntityType?.Name?.Replace(" ", "_") ?? "Source"),
                SvgStylesheetManager.EscapeXml(association.TargetEntityType?.Name?.Replace(" ", "_") ?? "Target"));

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "  <g id=\"{0}\" class=\"association-connector\">\n",
                connectorId);

            // Get connector edge points
            var pathData = GetConnectorPath(connector, offsetX, offsetY);

            // Render the connector line (dashed for associations, using CSS class)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <path d=\"{0}\" class=\"line association\" " +
                "marker-start=\"url(#diamond)\" marker-end=\"url(#diamond)\"/>\n",
                pathData);

            // Render cardinality labels
            RenderCardinalityLabels(sb, connector, association, offsetX, offsetY);

            sb.AppendLine("  </g>");

            return sb.ToString();
        }

        /// <summary>
        /// Renders an InheritanceConnector to SVG elements.
        /// </summary>
        internal string RenderInheritanceConnector(InheritanceConnector connector, double offsetX, double offsetY)
        {
            if (connector == null || connector.ModelElement == null)
            {
                return string.Empty;
            }

            var inheritance = connector.ModelElement as Inheritance;
            if (inheritance == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var connectorId = string.Format(
                CultureInfo.InvariantCulture,
                "inherit_{0}_{1}",
                SvgStylesheetManager.EscapeXml(inheritance.SourceEntityType?.Name?.Replace(" ", "_") ?? "Derived"),
                SvgStylesheetManager.EscapeXml(inheritance.TargetEntityType?.Name?.Replace(" ", "_") ?? "Base"));

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "  <g id=\"{0}\" class=\"inheritance-connector\">\n",
                connectorId);

            // Get connector edge points
            var pathData = GetConnectorPath(connector, offsetX, offsetY);

            // Render the connector line (solid for inheritance, using CSS class)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <path d=\"{0}\" class=\"line inheritance\" " +
                "marker-end=\"url(#inheritance-arrow)\"/>\n",
                pathData);

            sb.AppendLine("  </g>");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the SVG marker definitions for connectors (wrapped in defs element).
        /// </summary>
        internal string GetMarkerDefinitions()
        {
            var sb = new StringBuilder();
            sb.AppendLine("  <defs>");
            sb.Append(GetMarkerDefinitionsContent());
            sb.AppendLine("  </defs>");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the SVG marker definitions content without the defs wrapper.
        /// </summary>
        internal string GetMarkerDefinitionsContent()
        {
            var sb = new StringBuilder();

            // Diamond marker for associations (using CSS class)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <marker id=\"diamond\" viewBox=\"0 0 12 12\" refX=\"6\" refY=\"6\" " +
                "markerWidth=\"{0}\" markerHeight=\"{0}\" orient=\"auto\">\n",
                SvgStylesheetManager.FormatDouble(DiamondSize));
            sb.AppendLine("      <polygon points=\"6,1 11,6 6,11 1,6\" class=\"diamond\"/>");
            sb.AppendLine("    </marker>");

            // Hollow arrow marker for inheritance (using CSS class)
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "    <marker id=\"inheritance-arrow\" viewBox=\"0 0 12 12\" refX=\"12\" refY=\"6\" " +
                "markerWidth=\"{0}\" markerHeight=\"{0}\" orient=\"auto\">\n",
                SvgStylesheetManager.FormatDouble(ArrowSize));
            sb.AppendLine("      <polygon points=\"0,0 12,6 0,12\" class=\"arrow-hollow\"/>");
            sb.AppendLine("    </marker>");

            return sb.ToString();
        }

        private string GetConnectorPath(BinaryLinkShape connector, double offsetX, double offsetY)
        {
            return GetConnectorPathWithOffset(connector, offsetX, offsetY, EndpointOffset);
        }

        /// <summary>
        /// Builds the SVG path for a connector, optionally offsetting endpoints away from entities.
        /// </summary>
        /// <param name="connector">The connector shape.</param>
        /// <param name="offsetX">X offset for coordinate translation.</param>
        /// <param name="offsetY">Y offset for coordinate translation.</param>
        /// <param name="endpointPullback">Distance to pull endpoints away from entities (to show markers).</param>
        internal string GetConnectorPathWithOffset(BinaryLinkShape connector, double offsetX, double offsetY, double endpointPullback)
        {
            var sb = new StringBuilder();

            var edgePoints = connector.EdgePoints;
            if (edgePoints == null || edgePoints.Count < 2)
            {
                // Fallback to straight line from source to target
                var fromShape = connector.FromShape;
                var toShape = connector.ToShape;
                if (fromShape != null && toShape != null)
                {
                    var fromCenter = GetShapeCenter(fromShape, offsetX, offsetY);
                    var toCenter = GetShapeCenter(toShape, offsetX, offsetY);
                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "M {0} {1} L {2} {3}",
                        SvgStylesheetManager.FormatDouble(fromCenter.X),
                        SvgStylesheetManager.FormatDouble(fromCenter.Y),
                        SvgStylesheetManager.FormatDouble(toCenter.X),
                        SvgStylesheetManager.FormatDouble(toCenter.Y));
                }
                return sb.ToString();
            }

            // Convert edge points to pixel coordinates
            var points = new List<PointD>();
            foreach (EdgePoint point in edgePoints)
            {
                var x = (point.Point.X * 96) - offsetX;
                var y = (point.Point.Y * 96) - offsetY;
                points.Add(new PointD(x, y));
            }

            // Offset the start point away from the first entity (pull back along the connector direction)
            if (points.Count >= 2 && endpointPullback > 0)
            {
                var startDir = GetDirectionVector(points[0], points[1]);
                points[0] = new PointD(
                    points[0].X + startDir.X * endpointPullback,
                    points[0].Y + startDir.Y * endpointPullback);

                // Offset the end point away from the last entity
                var lastIdx = points.Count - 1;
                var endDir = GetDirectionVector(points[lastIdx], points[lastIdx - 1]);
                points[lastIdx] = new PointD(
                    points[lastIdx].X + endDir.X * endpointPullback,
                    points[lastIdx].Y + endDir.Y * endpointPullback);
            }

            // Build path from adjusted points
            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0)
                {
                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "M {0} {1}",
                        SvgStylesheetManager.FormatDouble(points[i].X),
                        SvgStylesheetManager.FormatDouble(points[i].Y));
                }
                else
                {
                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        " L {0} {1}",
                        SvgStylesheetManager.FormatDouble(points[i].X),
                        SvgStylesheetManager.FormatDouble(points[i].Y));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a normalized direction vector from point A to point B.
        /// </summary>
        private static PointD GetDirectionVector(PointD from, PointD to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.001)
            {
                return new PointD(0, 0);
            }
            return new PointD(dx / length, dy / length);
        }

        private PointD GetShapeCenter(NodeShape shape, double offsetX, double offsetY)
        {
            var bounds = shape.AbsoluteBounds;
            return new PointD(
                (bounds.X + bounds.Width / 2) * 96 - offsetX,
                (bounds.Y + bounds.Height / 2) * 96 - offsetY);
        }

        /// <summary>
        /// Determines which edge of an entity shape a connector endpoint attaches to.
        /// </summary>
        /// <param name="endpointX">X coordinate of the endpoint in pixels.</param>
        /// <param name="endpointY">Y coordinate of the endpoint in pixels.</param>
        /// <param name="entityBounds">The absolute bounds of the entity shape (in inches).</param>
        /// <returns>The edge of the entity where the connector attaches.</returns>
        private static ConnectorEdge GetConnectorEdge(double endpointX, double endpointY, RectangleD entityBounds)
        {
            // Convert entity bounds to pixels (96 DPI)
            var left = entityBounds.X * 96;
            var top = entityBounds.Y * 96;
            var right = (entityBounds.X + entityBounds.Width) * 96;
            var bottom = (entityBounds.Y + entityBounds.Height) * 96;

            // Calculate distance to each edge
            var distToLeft = Math.Abs(endpointX - left);
            var distToRight = Math.Abs(endpointX - right);
            var distToTop = Math.Abs(endpointY - top);
            var distToBottom = Math.Abs(endpointY - bottom);

            // Find minimum distance
            var minDist = Math.Min(Math.Min(distToLeft, distToRight), Math.Min(distToTop, distToBottom));

            // Return the edge with minimum distance (using tolerance for floating point comparison)
            if (Math.Abs(distToBottom - minDist) < EdgeDetectionTolerance) return ConnectorEdge.Bottom;
            if (Math.Abs(distToTop - minDist) < EdgeDetectionTolerance) return ConnectorEdge.Top;
            if (Math.Abs(distToLeft - minDist) < EdgeDetectionTolerance) return ConnectorEdge.Left;
            return ConnectorEdge.Right;
        }

        private void RenderCardinalityLabels(StringBuilder sb, AssociationConnector connector,
            Association association, double offsetX, double offsetY)
        {
            var sourceMultiplicity = association.SourceMultiplicity ?? "";
            var targetMultiplicity = association.TargetMultiplicity ?? "";

            var edgePoints = connector.EdgePoints;
            if (edgePoints == null || edgePoints.Count < 2)
            {
                return;
            }

            // Source cardinality (near first point)
            if (!string.IsNullOrEmpty(sourceMultiplicity) && connector.FromShape != null)
            {
                var sourcePoint = edgePoints[0].Point;
                var sourceX = (sourcePoint.X * 96) - offsetX;
                var sourceY = (sourcePoint.Y * 96) - offsetY;

                // Determine which edge of the source entity the connector attaches to
                var sourceEdge = GetConnectorEdge(
                    sourcePoint.X * 96,
                    sourcePoint.Y * 96,
                    connector.FromShape.AbsoluteBounds);

                var labelPos = GetMultiplicityLabelPosition(sourceX, sourceY, sourceEdge);

                // Left edge labels need text-anchor="end" so text extends leftward
                var textAnchor = sourceEdge == ConnectorEdge.Left ? " text-anchor=\"end\"" : "";
                //var textAnchor = "";
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "    <text x=\"{0}\" y=\"{1}\" class=\"text-base text-mult\"{3}>{2}</text>\n",
                    SvgStylesheetManager.FormatDouble(labelPos.X),
                    SvgStylesheetManager.FormatDouble(labelPos.Y),
                    SvgStylesheetManager.EscapeXml(sourceMultiplicity),
                    textAnchor);
            }

            // Target cardinality (near last point)
            if (!string.IsNullOrEmpty(targetMultiplicity) && connector.ToShape != null)
            {
                var lastIdx = edgePoints.Count - 1;
                var targetPoint = edgePoints[lastIdx].Point;
                var targetX = (targetPoint.X * 96) - offsetX;
                var targetY = (targetPoint.Y * 96) - offsetY;

                // Determine which edge of the target entity the connector attaches to
                var targetEdge = GetConnectorEdge(
                    targetPoint.X * 96,
                    targetPoint.Y * 96,
                    connector.ToShape.AbsoluteBounds);

                var labelPos = GetMultiplicityLabelPosition(targetX, targetY, targetEdge);

                // Left edge labels need text-anchor="end" so text extends leftward
                var textAnchor = targetEdge == ConnectorEdge.Left ? " text-anchor=\"end\"" : "";
                //var textAnchor = "";
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "    <text x=\"{0}\" y=\"{1}\" class=\"text-base text-mult\"{3}>{2}</text>\n",
                    SvgStylesheetManager.FormatDouble(labelPos.X),
                    SvgStylesheetManager.FormatDouble(labelPos.Y),
                    SvgStylesheetManager.EscapeXml(targetMultiplicity),
                    textAnchor);
            }
        }

        /// <summary>
        /// Calculates the position for a multiplicity label based on which entity edge the connector attaches to.
        /// Labels are positioned at a diagonal offset away from the entity, matching the VS designer.
        /// </summary>
        /// <param name="endpointX">X coordinate of the connector endpoint.</param>
        /// <param name="endpointY">Y coordinate of the connector endpoint.</param>
        /// <param name="edge">The edge of the entity where the connector attaches.</param>
        /// <returns>The position for the multiplicity label.</returns>
        /// <remarks>
        /// The <see cref="ConnectorEdge.Left" /> is the only one with the label to the left of the diamond.
        /// The <see cref="ConnectorEdge.Top" /> is the only one with the label above the diamond.
        /// </remarks>
        private static PointD GetMultiplicityLabelPosition(double endpointX, double endpointY, ConnectorEdge edge)
        {
            double labelX, labelY;

            switch (edge)
            {

                case ConnectorEdge.Bottom:
                    // Label below and to the right of diamond
                    // Extra 8px offset for bottom edge to clear the diamond marker
                    labelX = endpointX + MultLabelOffsetX;
                    labelY = endpointY + (MultLabelOffsetY * 2);
                    break;
                case ConnectorEdge.Top:
                    // Label above and to the right of diamond (only case where label goes up)
                    labelX = endpointX + MultLabelOffsetX;
                    labelY = endpointY - MultLabelOffsetY;
                    break;
                case ConnectorEdge.Left:
                    // Label below and to the left of diamond
                    labelX = endpointX - MultLabelOffsetX;
                    labelY = endpointY + (MultLabelOffsetY * 2);
                    break;
                case ConnectorEdge.Right:
                default:
                    // Label below and to the right of diamond
                    labelX = endpointX + MultLabelOffsetX;
                    labelY = endpointY + (MultLabelOffsetY * 2);
                    break;
            }

            return new PointD(labelX, labelY);
        }
    }
}
