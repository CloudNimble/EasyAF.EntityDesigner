// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Edmx.Designer;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     Arranges the shapes on an <see cref="EntityDesignerSurface" />.
    /// </summary>
    /// <remarks>
    ///     Implementations are held by <see cref="LayoutEngineManager" /> and chosen by the diagram's own
    ///     <see cref="Edmx.Designer.Diagram.LayoutMode" />, so a surface never names a concrete engine.
    ///     <para>
    ///     Engines carry no per-diagram state. One instance serves every open diagram, so everything that varies
    ///     between diagrams arrives as an argument to <see cref="Layout" />. An engine owns its own transaction
    ///     and progress reporting, because how much work it does - and therefore whether the user needs to be
    ///     told about it - is the engine's business rather than the surface's.
    ///     </para>
    ///     See specs/diagram-layout-engines.md.
    /// </remarks>
    internal abstract class LayoutEngineBase
    {

        #region Properties

        /// <summary>
        ///     The name shown to the user when this engine is offered as a choice.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        ///     The mode a diagram names to select this engine.
        /// </summary>
        /// <remarks>
        ///     Persisted as the <c>LayoutMode</c> attribute and passed on the command line, so the enum's member
        ///     names are part of the file format. <see cref="DisplayName" /> is the one free to be reworded.
        /// </remarks>
        public abstract LayoutMode Mode { get; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Arranges <paramref name="shapes" /> on <paramref name="surface" />.
        /// </summary>
        /// <param name="surface">The diagram whose shapes are being arranged.</param>
        /// <param name="shapes">
        ///     The shapes to arrange. Callers pass anything from the whole of
        ///     <see cref="Microsoft.VisualStudio.Modeling.Diagrams.ShapeElement.NestedChildShapes" /> down to the
        ///     handful of shapes a drag-and-drop just created.
        /// </param>
        /// <param name="connectorMode">How the user wants the connectors drawn.</param>
        /// <remarks>
        ///     <paramref name="shapes" /> is a weakly typed <see cref="IList" /> rather than
        ///     <c>IList&lt;ShapeElement&gt;</c> because that is what the Modeling SDK's own
        ///     <c>AutoLayoutShapeElements</c> takes and what every existing caller already holds. Tightening it
        ///     would push a cast onto all five call sites and buy nothing.
        ///     <para>
        ///     An engine with only one way of drawing connectors ignores <paramref name="connectorMode" />.
        ///     </para>
        /// </remarks>
        public abstract void Layout(EntityDesignerSurface surface, IList shapes, ConnectorMode connectorMode);

        /// <summary>
        ///     Re-routes <paramref name="connectors" /> in place, without moving any shape.
        /// </summary>
        /// <param name="surface">The diagram the connectors belong to.</param>
        /// <param name="connectors">
        ///     The connectors to re-route, as <see cref="Microsoft.VisualStudio.Modeling.Diagrams.BinaryLinkShape" />s.
        ///     Anything in the list that is not a routable connector is ignored.
        /// </param>
        /// <remarks>
        ///     This is placement-free: every shape stays exactly where it is and only the named connectors are
        ///     redrawn, using the rest of the diagram as obstacles. It exists for "redraw this connector" - a user
        ///     who dislikes how one route paints and wants the engine to redo just that one. The connector's
        ///     <see cref="Microsoft.VisualStudio.Modeling.Diagrams.LinkShape.ManuallyRouted" /> flag is left
        ///     untouched: whether the new route is a human's or the engine's is not this method's call to change,
        ///     and the in-memory view model owns what is painted. See specs/diagram-layout-engines.md.
        /// </remarks>
        public abstract void RouteConnectors(EntityDesignerSurface surface, IList connectors);

        #endregion

    }

}
