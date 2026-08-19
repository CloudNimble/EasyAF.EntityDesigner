// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using EntityDesignerRes = Microsoft.Data.Entity.Design.Diagrams.Properties.DiagramsResources;
using MsAglLineSegment = Microsoft.Msagl.Core.Geometry.Curves.LineSegment;
using MsAglNode = Microsoft.Msagl.Core.Layout.Node;
using MsAglPoint = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     Places shapes and routes connectors with MSAGL, writing both back to the diagram.
    /// </summary>
    /// <remarks>
    ///     Self-contained to MSAGL. This engine calls none of the Modeling SDK's layout or routing - not
    ///     <c>AutoLayoutShapeElements</c>, not <c>Reroute</c> - at any stage. The SDK routing is what this engine
    ///     exists to replace, so producing a diagram that depends on it would defeat the point.
    ///     <para>
    ///     Every connector it touches comes back <see cref="LinkShape.ManuallyRouted" /> with explicit
    ///     <see cref="LinkShape.EdgePoints" />, which the existing connector change rules persist to the EDMX as
    ///     <c>ConnectorPoint</c> elements. See specs/diagram-layout-engines.md.
    ///     </para>
    /// </remarks>
    internal sealed class MsAglLayoutEngine : LayoutEngineBase
    {

        #region Fields

        /// <summary>
        ///     The key <see cref="MsAglLayoutEngine" /> is registered under.
        /// </summary>
        internal const string EngineKey = "MsAgl";

        /// <summary>
        ///     Points sampled along each curved segment when flattening a route into a polyline.
        /// </summary>
        /// <remarks>
        ///     Eight is enough that a connector-sized arc reads as a curve rather than a chord, without inflating
        ///     the <c>ConnectorPoint</c> elements written to the EDMX more than it has to.
        /// </remarks>
        private const int CurveFlatteningSteps = 8;

        #endregion

        #region Properties

        /// <summary>
        ///     How shapes are placed. Defaults to <see cref="LayoutAlgorithm.Layered" />.
        /// </summary>
        /// <remarks>
        ///     A property rather than a constructor argument because the engine is registered once for the life of
        ///     the host, so taking this at construction would mean an instance per algorithm.
        /// </remarks>
        public LayoutAlgorithm Algorithm { get; set; } = LayoutAlgorithm.Layered;

        /// <summary>
        ///     How connectors are drawn. Defaults to <see cref="ConnectorRouting.Orthogonal" />.
        /// </summary>
        /// <remarks>
        ///     A property for the same reason <see cref="Algorithm" /> is one: the engine is registered once for
        ///     the life of the host.
        /// </remarks>
        public ConnectorRouting Routing { get; set; } = ConnectorRouting.Orthogonal;

        /// <inheritdoc />
        public override string DisplayName
        {
            get { return EntityDesignerRes.LayoutEngine_MsAgl; }
        }

        /// <inheritdoc />
        public override string Key
        {
            get { return EngineKey; }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void Layout(EntityDesignerSurface surface, IList shapes)
        {
            if (surface is null || shapes is null)
            {
                return;
            }

            var entityShapes = shapes.OfType<EntityTypeShape>().ToList();
            if (entityShapes.Count == 0)
            {
                return;
            }

            using (surface.BeginLongOperation())
            {
                var graph = BuildGraph(entityShapes, out var nodesByShape, out var connectorsByEdge);

                // Places the shapes and routes the connectors in one pass. Routing is deliberately left to MSAGL
                // rather than run afterwards - see RouteConnectors below for what that cost.
                LayoutHelpers.CalculateLayout(graph, MsAglConstants.CreateSettings(Algorithm, Routing), null);

                // MSAGL measures upward from the bottom left, the designer downward from the top left, so every
                // coordinate is mirrored about the top edge on the way back.
                //
                // Taken from the nodes rather than graph.BoundingBox, which carries MSAGL's own margin and would
                // push the whole diagram in from the corner by however much that happens to be.
                var origin = new MsAglPoint(
                    graph.Nodes.Min(node => node.BoundingBox.Left),
                    graph.Nodes.Max(node => node.BoundingBox.Top));

                surface.InDiagramTransaction(
                    EntityDesignerRes.Tx_LayoutDiagram,
                    () =>
                    {
                        ApplyShapePositions(nodesByShape, origin);
                        ApplyConnectorRoutes(connectorsByEdge, origin);
                    });
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Writes each routed edge back onto its connector as an explicit polyline.
        /// </summary>
        /// <remarks>
        ///     Setting <see cref="LinkShape.ManuallyRouted" /> is what stops the Modeling SDK rerouting these and
        ///     what makes the connector change rules persist the points to the EDMX. An edge the router could not
        ///     path is left alone rather than given a straight line, so a failure is visible instead of quietly
        ///     drawn through whatever is in the way.
        /// </remarks>
        private static void ApplyConnectorRoutes(
            IDictionary<Edge, BinaryLinkShape> connectorsByEdge, MsAglPoint origin)
        {
            foreach (var pair in connectorsByEdge)
            {
                var points = ToEdgePoints(pair.Key.Curve, origin);
                if (points is null)
                {
                    continue;
                }

                pair.Value.ManuallyRouted = true;
                pair.Value.EdgePoints = points;
            }
        }

        /// <summary>
        ///     Moves each shape to where the layout put it, without resizing it.
        /// </summary>
        /// <remarks>
        ///     Width and height are carried over from the shape rather than taken from the MSAGL node. An entity
        ///     shape sizes itself from the properties it is showing, so writing a height back would fight the
        ///     compartments.
        /// </remarks>
        private static void ApplyShapePositions(
            IDictionary<EntityTypeShape, MsAglNode> nodesByShape, MsAglPoint origin)
        {
            foreach (var pair in nodesByShape)
            {
                var bounds = pair.Key.AbsoluteBounds;
                var box = pair.Value.BoundingBox;

                pair.Key.AbsoluteBounds = new RectangleD(
                    (box.Left - origin.X) / MsAglConstants.DpiScale,
                    (origin.Y - box.Top) / MsAglConstants.DpiScale,
                    bounds.Width,
                    bounds.Height);
            }
        }

        /// <summary>
        ///     Builds the MSAGL graph that mirrors the given shapes and the connectors between them.
        /// </summary>
        /// <param name="entityShapes">The shapes to lay out.</param>
        /// <param name="nodesByShape">Receives the shape to node mapping, for writing positions back.</param>
        /// <param name="connectorsByEdge">Receives the edge to connector mapping, for writing routes back.</param>
        /// <returns>The graph, ready to lay out.</returns>
        /// <remarks>
        ///     Only connectors with both ends in <paramref name="entityShapes" /> are included. A drag-and-drop
        ///     lays out the handful of shapes it just created, and an edge to a shape outside that set has no node
        ///     to attach to.
        ///     <para>
        ///     Each connector is visited from one end only, through <see cref="EntityTypeShape.ConnectedLinks" />,
        ///     so a connector between two shapes in the set is not added twice.
        ///     </para>
        /// </remarks>
        private static GeometryGraph BuildGraph(
            IReadOnlyList<EntityTypeShape> entityShapes,
            out IDictionary<EntityTypeShape, MsAglNode> nodesByShape,
            out IDictionary<Edge, BinaryLinkShape> connectorsByEdge)
        {
            var graph = new GeometryGraph();
            nodesByShape = new Dictionary<EntityTypeShape, MsAglNode>(entityShapes.Count);
            connectorsByEdge = new Dictionary<Edge, BinaryLinkShape>();

            foreach (var shape in entityShapes)
            {
                var bounds = shape.AbsoluteBounds;
                var node = new MsAglNode(
                    CurveFactory.CreateRectangle(
                        bounds.Width * MsAglConstants.DpiScale,
                        bounds.Height * MsAglConstants.DpiScale,
                        new MsAglPoint()),
                    shape);

                graph.Nodes.Add(node);
                nodesByShape[shape] = node;
            }

            var seen = new HashSet<BinaryLinkShape>();

            foreach (var shape in entityShapes)
            {
                foreach (var link in shape.ConnectedLinks)
                {
                    if (!seen.Add(link))
                    {
                        continue;
                    }

                    if (link.FromShape is not EntityTypeShape from
                        || link.ToShape is not EntityTypeShape to
                        || !nodesByShape.TryGetValue(from, out var fromNode)
                        || !nodesByShape.TryGetValue(to, out var toNode))
                    {
                        continue;
                    }

                    var edge = new Edge(fromNode, toNode);
                    graph.Edges.Add(edge);
                    connectorsByEdge[edge] = link;
                }
            }

            return graph;
        }

        // Routing as a separate pass after layout. Kept, commented out, because it is the thing to reach for if
        // MSAGL's own routing is ever not enough - not because it should come back as it stands.
        //
        // Two things were wrong with it. The comment claimed LayoutHelpers.CalculateLayout ignores EdgeRoutingMode
        // for a graph with no clusters; it does not - LayeredLayoutEngine reads the mode and has a Rectilinear case
        // that builds this same router. And routing separately throws away the crossing reduction: the layered
        // algorithm orders shapes so that edges flowing through its channels cross as little as possible, and a
        // router run afterwards re-paths every edge independently as a shortest obstacle-avoiding route, with no
        // knowledge of those channels. Only EdgeRoutingMode.SugiyamaSplines consumes that work.
        //
        // Note the engine's own call is tuned differently from this one: padding NodeSeparation/3 and a sparse
        // visibility graph, against the fixed padding and dense graph below.
        //
        // private static void RouteConnectors(GeometryGraph graph)
        // {
        //     if (graph.Edges.Count == 0)
        //     {
        //         return;
        //     }
        //
        //     var router = new RectilinearEdgeRouter(
        //         graph,
        //         MsAglConstants.RouterPadding,
        //         MsAglConstants.RouterCornerFitRadius,
        //         MsAglConstants.RouterUseSparseVisibilityGraph,
        //         MsAglConstants.RouterEdgeSeparation);
        //
        //     router.Run();
        // }

        /// <summary>
        ///     Appends the start of <paramref name="segment" />, flattening it first when it is not a straight line.
        /// </summary>
        /// <param name="points">The polyline being built.</param>
        /// <param name="segment">The segment to append.</param>
        /// <remarks>
        ///     The designer draws connectors as polylines, so a curve has to be sampled into one. Taking only each
        ///     segment's endpoints would turn a spline into the crude polygon through its corners - fine for the
        ///     orthogonal routing, where every segment really is a line, and wrong for every other mode.
        /// </remarks>
        private static void AddSegment(ICollection<MsAglPoint> points, ICurve segment)
        {
            if (segment is MsAglLineSegment)
            {
                points.Add(segment.Start);

                return;
            }

            for (var step = 0; step < CurveFlatteningSteps; step++)
            {
                var t = segment.ParStart + ((segment.ParEnd - segment.ParStart) * step / CurveFlatteningSteps);
                points.Add(segment[t]);
            }
        }

        /// <summary>
        ///     Converts a routed curve into designer edge points, mirroring it back into designer coordinates.
        /// </summary>
        /// <param name="curve">The curve MSAGL routed, which may be null when routing failed.</param>
        /// <param name="origin">The graph's top left corner in MSAGL coordinates.</param>
        /// <returns>The polyline, or <see langword="null" /> when there is nothing usable to draw.</returns>
        /// <remarks>
        ///     Handles both shapes a rectilinear route arrives in. It is a <c>Polyline</c> when the router leaves
        ///     the corners square, and a <c>Curve</c> of segments once corner fitting has run - which it always
        ///     does, even at radius zero, where the segments come back as plain lines.
        /// </remarks>
        private static EdgePointCollection ToEdgePoints(ICurve curve, MsAglPoint origin)
        {
            if (curve is null)
            {
                return null;
            }

            var points = new List<MsAglPoint>();

            switch (curve)
            {
                case Polyline polyline:
                    points.AddRange(polyline);
                    break;

                case Curve composite when composite.Segments.Count > 0:
                    foreach (var segment in composite.Segments)
                    {
                        AddSegment(points, segment);
                    }

                    points.Add(composite.Segments[composite.Segments.Count - 1].End);
                    break;

                default:
                    points.Add(curve.Start);
                    points.Add(curve.End);
                    break;
            }

            if (points.Count < 2)
            {
                return null;
            }

            var edgePoints = new EdgePointCollection();
            foreach (var point in points)
            {
                edgePoints.Add(
                    new EdgePoint(
                        (point.X - origin.X) / MsAglConstants.DpiScale,
                        (origin.Y - point.Y) / MsAglConstants.DpiScale,
                        VGPointType.Normal));
            }

            return edgePoints;
        }

        #endregion

    }

}
