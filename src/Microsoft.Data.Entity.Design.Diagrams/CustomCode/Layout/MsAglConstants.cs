// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using System;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     The tuned MSAGL settings, in one place.
    /// </summary>
    /// <remarks>
    ///     Switching algorithms is picking a different <see cref="LayoutAlgorithm" />; everything either one needs
    ///     is configured here. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal static class MsAglConstants
    {

        #region Fields

        /// <summary>
        ///     Units per inch used while talking to MSAGL.
        /// </summary>
        /// <remarks>
        ///     The designer measures in inches and MSAGL is unitless, so a scale has to be picked. It cannot be
        ///     1 (inches directly), because <see cref="SugiyamaLayoutSettings.LayerSeparation" /> clamps its
        ///     setter to <c>Math.Max(10, value)</c> - every separation under ten inches would silently become ten.
        ///     96 is the same figure the SVG exporter uses to turn diagram inches into pixels.
        /// </remarks>
        internal const double DpiScale = 96.0;

        /// <summary>
        ///     Radius of the arc inscribed into each corner of a routed connector, in MSAGL units.
        /// </summary>
        /// <remarks>
        ///     Zero, because the designer draws connectors as polylines. A non-zero radius makes MSAGL emit arc
        ///     segments that would have to be flattened back into corner points anyway.
        /// </remarks>
        internal const double RouterCornerFitRadius = 0.0;

        /// <summary>
        ///     Minimum gap between two connectors running alongside each other, in MSAGL units.
        /// </summary>
        internal const double RouterEdgeSeparation = 6.0;

        /// <summary>
        ///     Clearance between a connector and the shapes it routes around, in MSAGL units.
        /// </summary>
        /// <remarks>
        ///     Roughly a tenth of an inch. MSAGL's own default is 1, which at this scale is a hundredth of an inch
        ///     and puts lines flush against the shapes they pass.
        /// </remarks>
        internal const double RouterPadding = 10.0;

        /// <summary>
        ///     Whether the router builds a sparse visibility graph.
        /// </summary>
        /// <remarks>
        ///     False. Sparse saves memory on very large graphs at the cost of choosing worse paths, and the models
        ///     this runs on are tens of entities rather than thousands.
        /// </remarks>
        internal const bool RouterUseSparseVisibilityGraph = false;

        /// <summary>
        ///     Gap between neighbouring rows, in MSAGL units. Roughly two thirds of an inch.
        /// </summary>
        internal const double LayerSeparation = 64.0;

        /// <summary>
        ///     Gap between neighbouring shapes within a row, in MSAGL units. Roughly half an inch.
        /// </summary>
        internal const double NodeSeparation = 48.0;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Builds the tuned settings for <paramref name="algorithm" /> and <paramref name="routing" />.
        /// </summary>
        /// <param name="algorithm">The algorithm that decides where shapes go.</param>
        /// <param name="routing">How connectors between those shapes are drawn.</param>
        /// <returns>A fresh settings instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Either argument is not a known value.</exception>
        /// <example>
        ///     <code>
        ///     var settings = MsAglConstants.CreateSettings(LayoutAlgorithm.Layered, ConnectorMode.Orthogonal);
        ///     LayoutHelpers.CalculateLayout(graph, settings, null);
        ///     </code>
        /// </example>
        /// <remarks>
        ///     A new instance every call, deliberately. MSAGL settings objects are not inert - a layout run writes
        ///     back to <c>Transformation</c> and the cluster and constraint collections - so a shared instance
        ///     would carry one diagram's state into the next.
        ///     <para>
        ///     Routing is left to MSAGL, which performs it as part of the layout run rather than as a separate
        ///     pass. That matters for <see cref="ConnectorMode.Layered" />: only the routing MSAGL does itself
        ///     follows the channels its crossing-reduction phase ordered, so routing afterwards discards that work.
        ///     </para>
        /// </remarks>
        internal static LayoutAlgorithmSettings CreateSettings(LayoutAlgorithm algorithm, ConnectorMode routing)
        {
            LayoutAlgorithmSettings settings = algorithm switch
            {
                LayoutAlgorithm.Layered => new SugiyamaLayoutSettings
                {
                    LayerSeparation = LayerSeparation,
                    NodeSeparation = NodeSeparation
                },
                LayoutAlgorithm.ForceDirected => new MdsLayoutSettings
                {
                    NodeSeparation = NodeSeparation
                },
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown layout algorithm.")
            };

            settings.EdgeRoutingSettings.EdgeRoutingMode = routing switch
            {
                ConnectorMode.Orthogonal => EdgeRoutingMode.Rectilinear,
                ConnectorMode.Layered => EdgeRoutingMode.SugiyamaSplines,
                ConnectorMode.Curved => EdgeRoutingMode.Spline,
                ConnectorMode.Straight => EdgeRoutingMode.StraightLine,

                // Legacy means the Modeling SDK draws the connectors, which is not something MSAGL can be
                // configured to do. Reaching here means a diagram is in Modern layout with Legacy connectors,
                // a pairing MsAglLayoutEngine is supposed to have normalized away before calling.
                ConnectorMode.Legacy => throw new ArgumentOutOfRangeException(
                    nameof(routing), routing, "MSAGL cannot draw legacy connectors."),

                _ => throw new ArgumentOutOfRangeException(nameof(routing), routing, "Unknown connector mode.")
            };

            settings.EdgeRoutingSettings.Padding = RouterPadding;
            settings.EdgeRoutingSettings.CornerRadius = RouterCornerFitRadius;
            settings.EdgeRoutingSettings.EdgeSeparationRectilinear = RouterEdgeSeparation;

            return settings;
        }

        #endregion

    }

}
