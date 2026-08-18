// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using EntityDesignerRes = Microsoft.Data.Entity.Design.Diagrams.Properties.DiagramsResources;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     The Modeling SDK's own layout, in three passes.
    /// </summary>
    /// <remarks>
    ///     This is the designer's historical behaviour, moved out of
    ///     <see cref="EntityDesignerSurface.AutoLayoutDiagram(IList)" /> unchanged so it stays available as the
    ///     default while other engines are developed against it.
    ///     <para>
    ///     It is not really an algorithm. It is three calls to the SDK's <c>AutoLayoutShapeElements</c> with
    ///     different routing and placement styles, freezing a different subset of shapes each time, and the
    ///     comments below still carry the Microsoft bug numbers the passes were added for. Treat it as a
    ///     baseline to beat rather than a design to learn from. See specs/diagram-layout-engines.md.
    ///     </para>
    /// </remarks>
    internal sealed class DslLayoutEngine : LayoutEngineBase
    {

        #region Fields

        /// <summary>
        ///     The key <see cref="DslLayoutEngine" /> is registered under.
        /// </summary>
        internal const string EngineKey = "Dsl";

        /// <summary>
        ///     Tells the layout to treat a shape as invisible: do not place it, and let other shapes sit on top.
        /// </summary>
        /// <remarks>
        ///     These flag combinations are taken from the Class Designer's auto layout.
        /// </remarks>
        private const VGNodeFixedStates IgnoreShapeFlags =
            VGNodeFixedStates.FixedPlace |      // don't consider for placement
            VGNodeFixedStates.PermeablePlace;   // place on top if desired (ignore for placement purposes)

        /// <summary>
        ///     Tells the layout to route around a shape but never move it.
        /// </summary>
        private const VGNodeFixedStates NoMoveShapeFlags = VGNodeFixedStates.FixedPlace;

        #endregion

        #region Properties

        /// <inheritdoc />
        public override string DisplayName
        {
            get { return EntityDesignerRes.LayoutEngine_Dsl; }
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

            // Put up an hourglass because this may take a while
            using (surface.BeginLongOperation())
            {
                // Inheritance lines need to be placed using a different styling
                // so that the lines join at the same point. Sort out which shapes we have
                List<ShapeElement> inheritanceLinks = new List<ShapeElement>();
                List<ShapeElement> inheritanceShapes = new List<ShapeElement>();
                List<ShapeElement> otherShapes = new List<ShapeElement>();

                Partition(shapes, inheritanceLinks, inheritanceShapes, otherShapes);

                // Perform the auto layout
                surface.InDiagramTransaction(
                    EntityDesignerRes.Tx_LayoutDiagram,
                    () =>
                    {
                        using (new SaveLayoutFlags(inheritanceShapes, IgnoreShapeFlags))
                        {
                            // Since the inheritance shapes will be moved later,
                            // tell this layout to ignore them and place other objects on
                            // top if necessary
                            surface.AutoLayoutShapeElements(
                                shapes,
                                VGRoutingStyle.VGRouteNetwork,
                                PlacementValueStyle.VGPlaceWE,
                                false);
                        }

                        using (new SaveLayoutFlags(otherShapes, NoMoveShapeFlags))
                        {
                            // DD 40487: Move any classes that have inheritance, while keeping
                            // the others in place. Use org chart and PlaceSN so that parent
                            // classes appear above child ones
                            surface.AutoLayoutShapeElements(
                                shapes,
                                VGRoutingStyle.VGRouteOrgChartNS,
                                PlacementValueStyle.VGPlaceSN,
                                false);
                        }

                        using (new SaveLayoutFlags(shapes, NoMoveShapeFlags))
                        {
                            // DD 40516: Make inheritance lines connect at single point and
                            // don't move anything else
                            surface.AutoLayoutShapeElements(
                                inheritanceLinks,
                                VGRoutingStyle.VGRouteRightAngle,
                                PlacementValueStyle.VGPlaceUndirected,
                                false);
                        }

                        surface.Reroute();
                    });
            } // restore cursor
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Splits <paramref name="shapes" /> into the three groups the passes below treat differently.
        /// </summary>
        /// <remarks>
        ///     Both ends of an inheritance belong in <paramref name="inheritanceShapes" />, not just the derived
        ///     one. Including only the derived type is what used to send shapes to the bottom of the screen when
        ///     a diagram containing inheritance was laid out more than once.
        /// </remarks>
        private static void Partition(
            IList shapes,
            List<ShapeElement> inheritanceLinks,
            List<ShapeElement> inheritanceShapes,
            List<ShapeElement> otherShapes)
        {
            foreach (ShapeElement shape in shapes)
            {
                var isInheritanceClass = false;

                // get a list of all lines leading to/from the shape
                if (shape is EntityTypeShape entityTypeShape)
                {
                    ArrayList allLinks = new ArrayList(entityTypeShape.FromRoleLinkShapes);
                    allLinks.AddRange(entityTypeShape.ToRoleLinkShapes);

                    foreach (LinkShape link in allLinks)
                    {
                        if (link is InheritanceConnector)
                        {
                            isInheritanceClass = true;
                            break;
                        }
                    }
                }

                if (isInheritanceClass)
                {
                    inheritanceShapes.Add(shape);
                }
                else if (shape is InheritanceConnector)
                {
                    inheritanceLinks.Add(shape);
                }
                else
                {
                    otherShapes.Add(shape);
                }
            }
        }

        #endregion

    }

}
