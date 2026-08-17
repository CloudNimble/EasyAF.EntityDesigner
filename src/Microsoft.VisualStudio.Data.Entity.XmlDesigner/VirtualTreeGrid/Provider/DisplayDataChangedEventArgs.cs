// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments describing a display change. DisplayDataChanged events are fired when
    ///     items need updated, but there is no structural change to the tree.
    /// </summary>
    internal sealed class DisplayDataChangedEventArgs : EventArgs
    {
        private readonly ITree myTree;
        private readonly VirtualTreeDisplayDataChanges myChanges;
        private readonly int myStartRow;
        private readonly int myColumn;
        private readonly int myCount;

        internal DisplayDataChangedEventArgs(ITree tree, VirtualTreeDisplayDataChanges changes, int startRow, int column, int count)
        {
            myTree = tree;
            myChanges = changes;
            myStartRow = startRow;
            myColumn = column;
            myCount = count;
        }

        /// <summary>
        ///     The tree that needs refreshing
        /// </summary>
        /// <value>ITree</value>
        public ITree Tree
        {
            get { return myTree; }
        }

        /// <summary>
        ///     The type of update to make. Views can reduce flicker by only updating portions of an item.
        /// </summary>
        /// <value>VirtualTreeDisplayDataChanges</value>
        public VirtualTreeDisplayDataChanges Changes
        {
            get { return myChanges; }
        }

        /// <summary>
        ///     The first row to update.
        /// </summary>
        /// <value></value>
        public int StartRow
        {
            get { return myStartRow; }
        }

        /// <summary>
        ///     The column to update. -1 means all columns need to be refreshed.
        /// </summary>
        /// <value></value>
        public int Column
        {
            get { return myColumn; }
        }

        /// <summary>
        ///     The number of rows that need to be refreshed.
        /// </summary>
        /// <value></value>
        public int Count
        {
            get { return myCount; }
        }
    }

}
