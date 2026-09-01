// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Routing.Rectilinear;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConnectorMode = Microsoft.Data.Entity.Design.Edmx.Designer.ConnectorMode;
using EntityDesignerRes = Microsoft.Data.Entity.Design.Diagrams.Properties.DiagramsResources;
using LayoutMode = Microsoft.Data.Entity.Design.Edmx.Designer.LayoutMode;
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
        ///     The connector mode used when a diagram in this layout mode does not name a usable one.
        /// </summary>
        /// <remarks>
        ///     Reached when a hand-edited file pairs <see cref="LayoutMode.Modern" /> with
        ///     <see cref="ConnectorMode.Legacy" />, which nothing in the designer produces. Substituting the
        ///     default costs that diagram its preferred connectors; refusing to lay out would cost it the whole
        ///     arrangement.
        /// </remarks>
        private const ConnectorMode DefaultConnectorMode = ConnectorMode.Orthogonal;

        /// <summary>
        ///     What the graph's root cluster is called.
        /// </summary>
        /// <remarks>
        ///     Never shown. It exists only because the root's user data cannot be null - see
        ///     <c>AddClusters</c>.
        /// </remarks>
        private const string RootClusterName = "<root>";

        /// <summary>
        ///     The grouping handed to the graph builder when grouping is off - no shape in any cluster.
        /// </summary>
        private static readonly IReadOnlyDictionary<EntityTypeShape, string> EmptyGroups =
            new Dictionary<EntityTypeShape, string>();

        #endregion

        #region Properties

        /// <summary>
        ///     How shapes are placed. Defaults to <see cref="LayoutAlgorithm.Layered" />.
        /// </summary>
        /// <remarks>
        ///     A property rather than a constructor argument because the engine is registered once for the life of
        ///     the host, so taking this at construction would mean an instance per algorithm. Unlike the connector
        ///     mode it is not persisted per diagram, because nothing offers it to the user yet.
        /// </remarks>
        public LayoutAlgorithm Algorithm { get; set; } = LayoutAlgorithm.Layered;

        /// <inheritdoc />
        public override string DisplayName
        {
            get { return EntityDesignerRes.LayoutEngine_MsAgl; }
        }

        /// <inheritdoc />
        public override LayoutMode Mode
        {
            get { return LayoutMode.Modern; }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void Layout(EntityDesignerSurface surface, IList shapes, ConnectorMode connectorMode)
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

            var routing = connectorMode == ConnectorMode.Legacy ? DefaultConnectorMode : connectorMode;

            // Grouping and name generation are opt-in, both off by default, and name generation is subordinate to
            // grouping - so nothing is generated while grouping is off, whatever the generate flag says. See
            // specs/diagram-layout-engines.md.
            //   grouping off            -> no clusters, nothing written
            //   grouping on, gen off     -> cluster by names already in the file only
            //   grouping on, gen on      -> detect, cluster, and write back (never overwriting)
            // Grouped before anything is placed, and written back before anything is moved, so the names the
            // layout used are the names in the file even if the arrangement is later undone.
            IReadOnlyDictionary<EntityTypeShape, string> groups;
            if (!surface.EnableGrouping)
            {
                groups = EmptyGroups;
            }
            else if (surface.GenerateGroupNames)
            {
                groups = GroupingLayout.Group(entityShapes);
                surface.PersistGroupNames(groups);
            }
            else
            {
                groups = GroupingLayout.GroupByExisting(entityShapes);
            }

            using (surface.BeginLongOperation())
            {
                var graph = BuildGraph(entityShapes, groups, out var nodesByShape, out var connectorsByEdge);

                // Places the shapes and routes the connectors in one pass. Routing is deliberately left to MSAGL
                // rather than run afterwards - see RouteConnectors below for what that cost.
                LayoutHelpers.CalculateLayout(graph, MsAglConstants.CreateSettings(Algorithm, routing), null);

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
                        ApplyConnectorRoutes(connectorsByEdge, origin, markManuallyRouted: true);
                    });
            }
        }

        /// <inheritdoc />
        public override void RouteConnectors(EntityDesignerSurface surface, IList connectors)
        {
            if (surface is null || connectors is null)
            {
                return;
            }

            var links = connectors.OfType<BinaryLinkShape>().ToList();
            if (links.Count == 0)
            {
                return;
            }

            // Every shape on the surface is an obstacle, placed at its current position - nothing moves. The graph
            // carries only the connectors being redrawn as edges, so the router paths those and leaves the rest.
            // Designer coordinates map to MSAGL's (Y up) as (x*scale, -y*scale); feeding that in and reading it back
            // through the origin=(0,0) mirror in ApplyConnectorRoutes returns points in the shapes' own coordinates.
            var graph = new GeometryGraph();
            var nodesByShape = new Dictionary<EntityTypeShape, MsAglNode>();

            foreach (var shape in surface.NestedChildShapes.OfType<EntityTypeShape>())
            {
                var bounds = shape.AbsoluteBounds;
                var center = new MsAglPoint(
                    (bounds.X + bounds.Width / 2.0) * MsAglConstants.DpiScale,
                    -(bounds.Y + bounds.Height / 2.0) * MsAglConstants.DpiScale);

                var node = new MsAglNode(
                    CurveFactory.CreateRectangle(
                        bounds.Width * MsAglConstants.DpiScale,
                        bounds.Height * MsAglConstants.DpiScale,
                        center),
                    shape);

                graph.Nodes.Add(node);
                nodesByShape[shape] = node;
            }

            var connectorsByEdge = new Dictionary<Edge, BinaryLinkShape>();
            var seen = new HashSet<BinaryLinkShape>();

            foreach (var link in links)
            {
                if (!seen.Add(link)
                    || link.FromShape is not EntityTypeShape from
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

            if (connectorsByEdge.Count == 0)
            {
                return;
            }

            using (surface.BeginLongOperation())
            {
                var router = new RectilinearEdgeRouter(
                    graph,
                    MsAglConstants.RouterPadding,
                    MsAglConstants.RouterCornerFitRadius,
                    MsAglConstants.RouterUseSparseVisibilityGraph,
                    MsAglConstants.RouterEdgeSeparation);
                router.Run();

                // markManuallyRouted: false - a redraw repaints the route and leaves its provenance flag alone.
                surface.InDiagramTransaction(
                    EntityDesignerRes.Tx_LayoutDiagram,
                    () => ApplyConnectorRoutes(connectorsByEdge, new MsAglPoint(0.0, 0.0), markManuallyRouted: false));
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
        ///     <para>
        ///     A full layout owns the routes it produces and sets the flag; a "redraw this connector" pass leaves
        ///     the flag as it found it (<paramref name="markManuallyRouted" /> is <see langword="false" />) - it is
        ///     only repainting one connector, not deciding whose route it is.
        ///     </para>
        /// </remarks>
        private static void ApplyConnectorRoutes(
            IDictionary<Edge, BinaryLinkShape> connectorsByEdge, MsAglPoint origin, bool markManuallyRouted)
        {
            foreach (var pair in connectorsByEdge)
            {
                var points = ToEdgePoints(pair.Key.Curve, origin);
                if (points is null)
                {
                    continue;
                }

                if (markManuallyRouted)
                {
                    pair.Value.ManuallyRouted = true;
                }

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
        /// <param name="groups">The group each shape belongs to, which becomes an MSAGL cluster.</param>
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
            IReadOnlyDictionary<EntityTypeShape, string> groups,
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

            AddClusters(graph, groups, nodesByShape);

            return graph;
        }

        /// <summary>
        ///     Turns the detected groups into MSAGL clusters, so each group is placed as a block.
        /// </summary>
        /// <param name="graph">The graph being built.</param>
        /// <param name="groups">The group each shape belongs to.</param>
        /// <param name="nodesByShape">The node standing in for each shape.</param>
        /// <remarks>
        ///     Nothing is added for a single group. One cluster holding everything is the same arrangement with
        ///     an extra layout pass over it, and MSAGL's clustered path is the more expensive of the two.
        ///     <para>
        ///     <c>RootCluster.UserData</c> has to be non-null. MSAGL looks each cluster up in the settings'
        ///     cluster table on the way in, including the root, and a null key throws out of
        ///     <c>Dictionary.ContainsKey</c> before any layout happens.
        ///     </para>
        /// </remarks>
        private static void AddClusters(
            GeometryGraph graph,
            IReadOnlyDictionary<EntityTypeShape, string> groups,
            IDictionary<EntityTypeShape, MsAglNode> nodesByShape)
        {
            if (groups is null || groups.Count == 0)
            {
                return;
            }

            var byGroup = groups
                .Where(pair => nodesByShape.ContainsKey(pair.Key))
                .GroupBy(pair => pair.Value, StringComparer.Ordinal)
                .ToList();

            if (byGroup.Count < 2)
            {
                return;
            }

            graph.RootCluster.UserData = RootClusterName;

            foreach (var group in byGroup)
            {
                var cluster = new Cluster
                {
                    UserData = group.Key,
                    RectangularBoundary = new RectangularClusterBoundary()
                };

                foreach (var pair in group)
                {
                    cluster.AddChild(nodesByShape[pair.Key]);
                }

                graph.RootCluster.AddChild(cluster);
            }
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
                    points.AddRange(composite.Segments.Select(segment => segment.Start));
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
