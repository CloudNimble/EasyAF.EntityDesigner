// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Flags used to navigate in a tree or tree grid structures. Navigation is
    ///     used to locate adjacent or related items. It is not used to perform any
    ///     expansion operations on the items. For example, a right keystroke on a
    ///     collapsed node will generally expand the node, but navigating right will
    ///     either go to the next column or the first child expansion.
    /// </summary>
    internal enum TreeNavigation
    {
        /// <summary>
        ///     No data.
        /// </summary>
        None,

        /// <summary>
        ///     Go to the parent node. Available for any node that
        ///     is not in a root list (either the primary root list, or the
        ///     root list in a complex or expandable cell).
        /// </summary>
        Parent,

        /// <summary>
        ///     Go to the parent node. If the item is a the root of a subitem
        ///     list then this goes to the parent column.
        /// </summary>
        ComplexParent,

        /// <summary>
        ///     Move to the first child of an expanded node.
        /// </summary>
        FirstChild,

        /// <summary>
        ///     Move the last child of an expanded node.
        /// </summary>
        LastChild,

        /// <summary>
        ///     Move to the next sibling. A sibling is a node with the same parent, level, and column.
        /// </summary>
        NextSibling,

        /// <summary>
        ///     Move to the previous sibling. A sibling is a node with the same parent, level, and column.
        /// </summary>
        PreviousSibling,

        /// <summary>
        ///     Move up one item in the same column. Jump over blank cells as needed. If the current cell is blank, then move to the anchor for the blank range.
        /// </summary>
        Up,

        /// <summary>
        ///     Move down one item in the same column. Jump over blank cells as needed. If the current cell is blank, then move to the anchor for next blank range.
        /// </summary>
        Down,

        /// <summary>
        ///     Move to the parent node, or the previous column if no parent is available.
        /// </summary>
        Left,

        /// <summary>
        ///     Move to the closest cell in the previous column.
        /// </summary>
        LeftColumn,

        /// <summary>
        ///     Move to the next column, or the first child if no column is available.
        /// </summary>
        Right,

        /// <summary>
        ///     Move to the closest cell in the next column.
        /// </summary>
        RightColumn,
    };

}
