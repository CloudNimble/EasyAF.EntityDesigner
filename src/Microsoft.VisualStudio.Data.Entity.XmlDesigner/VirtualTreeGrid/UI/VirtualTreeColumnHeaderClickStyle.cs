// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     The type of column header click initiated by the user. User with
    ///     VirtualTreeColumnHeaderClickEventArgs.
    /// </summary>
    internal enum VirtualTreeColumnHeaderClickStyle
    {
        /// <summary>
        ///     The user clicked a column header with the left mouse button
        /// </summary>
        Click,

        /// <summary>
        ///     The user double clicked on a column header with the left mouse button
        /// </summary>
        DoubleClick,

        /// <summary>
        ///     The user double clicked on a column header divider with the left mouse
        ///     button. The default action is to resize the column to fit the column contents
        ///     as closely as possible. Note that for proportional columns, the full width
        ///     may not be available to the column.
        /// </summary>
        DividerDoubleClick,

        /// <summary>
        ///     The user requested a context menu on a header
        /// </summary>
        ContextMenu,
    }

}
