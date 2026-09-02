// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Determine the action a MultiSelect VirtualTreeControl should take
    ///     when the selection column changes.
    /// </summary>
    internal enum ColumnSelectionTransferAction
    {
        /// <summary>
        ///     Select all non-blank cells in the new column that were
        ///     in rows that were selected in the old column.
        /// </summary>
        PreserveNonBlankCells,

        /// <summary>
        ///     Select all cells in the new column that were selected
        ///     in the old column and are either non-blank or have
        ///     a blank expansion anchor on the same row.
        /// </summary>
        PreserveAnchoredCells,

        /// <summary>
        ///     Select all cells in the new column that were selected
        ///     in the old column and are either non-blank or have
        ///     a blank expansion anchor on the same row.
        /// </summary>
        PreserveSharedAnchors,

        /// <summary>
        ///     Select all cells in the new column that share a blank
        ///     expansion anchor with cells in the old column.
        /// </summary>
        PreserveSharedAnchorsOnly,

        /// <summary>
        ///     Do not move selection state from the old column to the new
        /// </summary>
        ClearSelectedRows,
    }

}
