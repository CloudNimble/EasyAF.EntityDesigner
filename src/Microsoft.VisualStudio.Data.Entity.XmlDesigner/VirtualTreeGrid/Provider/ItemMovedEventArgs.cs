// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments indicating that an item has moved within branch
    /// </summary>
    internal sealed class ItemMovedEventArgs : EventArgs
    {
        private readonly ITree myTree;
        private readonly int myColumn;
        private readonly int myFromRow;
        private readonly int myToRow;
        private readonly int myItemCount;
        private readonly bool myUpdateTrailingColumns;

        internal ItemMovedEventArgs(ITree tree, int column, int fromRow, int toRow, int itemCount, bool updateTrailingColumns)
        {
            myTree = tree;
            myColumn = column;
            myFromRow = fromRow;
            myToRow = toRow;
            myItemCount = itemCount;
            myUpdateTrailingColumns = updateTrailingColumns;
        }

        /// <summary>
        ///     The tree sending the event
        /// </summary>
        public ITree Tree
        {
            get { return myTree; }
        }

        /// <summary>
        ///     The column the change happened in. All columns to the
        ///     right of this column must also be changed.
        /// </summary>
        public int Column
        {
            get { return myColumn; }
        }

        /// <summary>
        ///     The row the item moved from.
        /// </summary>
        public int FromRow
        {
            get { return myFromRow; }
        }

        /// <summary>
        ///     The row the item moved to. If ToRow is greater than FromRow, the
        ///     the ToRow value is the final row and assumes that ItemCount items
        ///     are no longer at FromRow.
        /// </summary>
        public int ToRow
        {
            get { return myToRow; }
        }

        /// <summary>
        ///     The number of items to move. If StartRow is not expanded and
        ///     does not have any complex subitems, then this number will be 1.
        /// </summary>
        public int ItemCount
        {
            get { return myItemCount; }
        }

        /// <summary>
        ///     Set to true if items in all columns to the right of the specified column are updated
        /// </summary>
        /// <value>True to move trailing column values with the column, false if only the specified comment is affected</value>
        public bool UpdateTrailingColumns
        {
            get { return myUpdateTrailingColumns; }
        }
    }

}
