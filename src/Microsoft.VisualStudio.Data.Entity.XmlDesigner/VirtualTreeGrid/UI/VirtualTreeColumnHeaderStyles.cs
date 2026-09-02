// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Specify style information for a VirtualTreeColumnHeader
    /// </summary>
    [Flags]
    internal enum VirtualTreeColumnHeaderStyles
    {
        /// <summary>
        ///     Use default style settings
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Text is left aligned
        /// </summary>
        AlignLeft = 0,

        /// <summary>
        ///     Text is center aligned
        /// </summary>
        AlignCenter = 1,

        /// <summary>
        ///     Text is right aligned
        /// </summary>
        AlignRight = 2,

        /// <summary>
        ///     The image is displayed to the right of the text (default is left)
        /// </summary>
        ImageOnRight = 4,

        /// <summary>
        ///     Display an up arrow on the header
        /// </summary>
        DisplayUpArrow = 8,

        /// <summary>
        ///     Display a down arrow on the header
        /// </summary>
        DisplayDownArrow = 0x10,

        /// <summary>
        ///     This item cannot be dragged. Disables dragging if VirtualTreeControl.HeaderDragDrop is true.
        /// </summary>
        DragDisabled = 0x20,

        /// <summary>
        ///     The order of this item cannot be changed. This is stronger than the DragDisabled
        ///     flag because it also blocks other items from being dropped in locations that would change
        ///     the position of this item. This flag is respected by header operations, but ignore when
        ///     setting the ColumnPermutation. Otherwise, it would be impossible to place the column.
        /// </summary>
        ColumnPositionLocked = 0x40,

        /// <summary>
        ///     DrawItemHeader events will fire when this flag is set.  The header control itself will
        ///     do no drawing, it is all up to the event handler.
        /// </summary>
        OwnerDraw = 0x80,

        /// <summary>
        ///     DrawItemHeader events will fire when this flag is set.  The header control will draw
        ///     normally first, providing the event handler a chance to do further drawing (such as an
        ///     image overlay) later.
        /// </summary>
        OwnerDrawOverlay = 0x100
    }

}
