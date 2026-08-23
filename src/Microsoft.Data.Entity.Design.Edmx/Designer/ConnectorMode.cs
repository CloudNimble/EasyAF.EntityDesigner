// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Edmx.Designer
{

    /// <summary>
    ///     How a diagram's connectors are drawn, persisted as the <c>ConnectorMode</c> attribute on <c>Diagram</c>.
    /// </summary>
    /// <remarks>
    ///     <see cref="Legacy" /> pairs with <see cref="LayoutMode.Legacy" /> and with nothing else. The other four
    ///     are modes of the modern engine, named for what each one draws rather than for the MSAGL routing mode it
    ///     maps to. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal enum ConnectorMode
    {

        /// <summary>
        ///     The Modeling SDK draws the connectors. The only mode <see cref="LayoutMode.Legacy" /> supports, and
        ///     the default, so a file written before this attribute existed draws exactly as it always did.
        /// </summary>
        Legacy = 0,

        /// <summary>
        ///     Right-angle segments that route around the shapes in the way.
        /// </summary>
        /// <remarks>
        ///     The conventional look for an entity diagram, and what <see cref="LayoutMode.Modern" /> uses unless
        ///     told otherwise. Each connector is pathed on its own as the shortest route avoiding obstacles, so it
        ///     does not use the crossing reduction the layered algorithm performed while ordering the shapes - see
        ///     <see cref="Layered" />.
        /// </remarks>
        Orthogonal = 1,

        /// <summary>
        ///     Curves that follow the rows the layered algorithm created.
        /// </summary>
        /// <remarks>
        ///     The only mode that consumes the crossing reduction done during layout, because it routes through the
        ///     same channels the ordering phase arranged. Expect visibly fewer crossings, and curved rather than
        ///     square lines. Meaningless with a force-directed placement, which has no rows.
        /// </remarks>
        Layered = 2,

        /// <summary>
        ///     Smooth curves routed around the shapes in the way.
        /// </summary>
        Curved = 3,

        /// <summary>
        ///     Direct lines from shape to shape, passing over anything in between.
        /// </summary>
        /// <remarks>
        ///     Not useful for reading a diagram. Useful for seeing what the placement alone did, with routing taken
        ///     out of the picture entirely.
        /// </remarks>
        Straight = 4

    }

}
