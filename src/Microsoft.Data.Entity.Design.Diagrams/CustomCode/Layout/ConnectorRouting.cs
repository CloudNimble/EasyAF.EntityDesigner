// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     How <see cref="MsAglLayoutEngine" /> draws the connectors between shapes.
    /// </summary>
    /// <remarks>
    ///     Named for what each one does rather than for the MSAGL enum it maps to. Every option is routed by MSAGL;
    ///     none of them involves the Modeling SDK. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal enum ConnectorRouting
    {

        /// <summary>
        ///     Right-angle segments that route around the shapes in the way.
        /// </summary>
        /// <remarks>
        ///     The conventional look for an entity diagram, and the default. Each connector is pathed on its own as
        ///     the shortest route avoiding obstacles, which means it does not use the crossing reduction the layered
        ///     algorithm performed while ordering the shapes - see <see cref="Layered" />.
        /// </remarks>
        Orthogonal = 0,

        /// <summary>
        ///     Curves that follow the rows the layered algorithm created.
        /// </summary>
        /// <remarks>
        ///     The only option that consumes the crossing reduction done during layout, because it routes through
        ///     the same channels the ordering phase arranged. Expect visibly fewer crossings and curved rather than
        ///     square lines. Meaningless with <see cref="LayoutAlgorithm.ForceDirected" />, which has no rows.
        /// </remarks>
        Layered = 1,

        /// <summary>
        ///     Smooth curves routed around the shapes in the way.
        /// </summary>
        Curved = 2,

        /// <summary>
        ///     Direct lines from shape to shape, passing over anything in between.
        /// </summary>
        /// <remarks>
        ///     Not useful for reading a diagram. Useful for seeing what the placement alone did, with routing taken
        ///     out of the picture entirely.
        /// </remarks>
        Straight = 3

    }

}
