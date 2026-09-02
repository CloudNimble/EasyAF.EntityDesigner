// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Specifies how drag effects are combined when multiple
    ///     items are selected for dragging. The drag object returned
    ///     by the tree needs to decide if feedback should be provided
    ///     as a union or intersection of the effects supported by the
    ///     different drag objects.
    /// </summary>
    internal enum DragEffectCombinationMode
    {
        /// <summary>
        ///     Combine drag/drop effects with the 'binary and' operator. If
        ///     the intersection is empty, then the drag operation is aborted.
        /// </summary>
        Intersection,

        /// <summary>
        ///     Combine drag/drop effects with the 'binary or' operator. The
        ///     returned effects for a union of effects supported by all of the nodes.
        /// </summary>
        Union,
    }

}
