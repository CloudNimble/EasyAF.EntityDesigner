// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments describing a range of items in the tree to be synchronized.
    /// </summary>
    internal class SynchronizeStateEventArgs : EventArgs
    {
        private readonly ColumnItemEnumerator myItemsToSynchronize;
        private readonly IBranch myMatchBranch;
        private readonly int myMatchRow;
        private readonly int myMatchColumn;
        private readonly VirtualTree myTree;

        /// <summary>
        ///     Create a new SynchronizeStateEventArgs.
        /// </summary>
        /// <param name="tree">Tree raising this event.  Necessary to allow clients that handle synchronization themeselves to raise events back to the tree.</param>
        /// <param name="itemsToSynchronize">Enumerator of items to be synchronized.</param>
        /// <param name="matchBranch">Branch whose state should be matched.</param>
        /// <param name="matchRow">Row whose state should be matched.</param>
        /// <param name="matchColumn">Column whose state should be matched.</param>
        public SynchronizeStateEventArgs(
            ITree tree, ColumnItemEnumerator itemsToSynchronize, IBranch matchBranch, int matchRow, int matchColumn)
        {
            Handled = false;
            myItemsToSynchronize = itemsToSynchronize;
            myMatchBranch = matchBranch;
            myMatchRow = matchRow;
            myMatchColumn = matchColumn;
            myTree = tree as VirtualTree;
        }

        /// <summary>
        ///     Flag that indicates whether this Synchronization event has been processed.  If this is set to true by a handler
        ///     of the ITree.SynchronizationBeginning event, subsequent calls to IBranch.SynchonizeState will not be made.  The
        ///     ITree.SynchronizationEnding event will still be raised, however.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Enumerator that allows listeners to iterate over the items to be synchronized.  Handlers should not change the structure
        ///     of the tree during this event, so this enumerator does not get out of sync.  Also, before use, handlers should call
        ///     Reset() to ensure enumeration starts from the beginning.
        /// </summary>
        public ColumnItemEnumerator ItemsToSynchronize
        {
            get { return myItemsToSynchronize; }
        }

        /// <summary>
        ///     The branch whose state the items in the ItemsToSynchronize enumerator should be syncronized to.
        /// </summary>
        public IBranch MatchBranch
        {
            get { return myMatchBranch; }
        }

        /// <summary>
        ///     The row index (relative to MatchBranch) that indicates the item in MatchBranch that the items in the ItemsToSynchronize enumerator should be syncronized to.
        /// </summary>
        public int MatchRow
        {
            get { return myMatchRow; }
        }

        /// <summary>
        ///     The column index that indicates the item in MatchBranch that the items in the ItemsToSynchronize enumerator should be syncronized to.
        /// </summary>
        public int MatchColumn
        {
            get { return myMatchColumn; }
        }

        /// <summary>
        ///     Allows providers listening to synchronization events to communicate state changes back to the tree.  This
        ///     is required so that the tree sends out the appropriate notifications, which, when the tree is attached to a
        ///     control, will result in things like correct repaint and firing accessibility state change events.
        /// </summary>
        /// <param name="stateChanges">Description of changes.</param>
        /// <param name="row">Row that changed.</param>
        /// <param name="column">Column that changed.</param>
        public void NotifyStateChange(StateRefreshChanges stateChanges, int row, int column)
        {
            Debug.Assert(myTree != null, "unable to notify state change.");
            myTree?.NotifyStateChange(row, column, stateChanges);
        }
    }

}
