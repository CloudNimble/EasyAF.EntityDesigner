// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments for an item count change. Provides details about the location and nature of the change.
    /// </summary>
    internal sealed class ItemCountChangedEventArgs : EventArgs
    {
        private readonly ITree myTree;
        private readonly int myRow;
        private readonly int myColumn;
        private readonly int myChange;
        private readonly int myParentRow;
        private readonly int myBlanksAfterAnchor;
        private readonly SubItemColumnAdjustment[] myColumnAdjustments;
        private SubItemColumnAdjustmentCollection myColumnAdjustmentsCollection;

        internal ItemCountChangedEventArgs(
            ITree tree, int anchorRow, int column, int change, int parentRow, int blanksAfterAnchor,
            SubItemColumnAdjustment[] subItemChanges, bool isToggle)
        {
            myTree = tree;
            myRow = anchorRow;
            myColumn = column;
            myChange = change;
            myParentRow = parentRow;
            myBlanksAfterAnchor = isToggle ? blanksAfterAnchor : -1;
            myColumnAdjustments = subItemChanges;
            myColumnAdjustmentsCollection = null;
        }

        /// <summary>
        ///     The tree where the change is happening
        /// </summary>
        public ITree Tree
        {
            get { return myTree; }
        }

        /// <summary>
        ///     The row immediately before the items being changed. If IsExpansionToggle
        ///     is true, then this is the row being expanded or collapsed. For an insertion (Change &lt; 0),
        ///     this is the row immediately before the items being added. For a deletion (Change &gt; 0), this
        ///     is the row before the first item being deleted.
        /// </summary>
        public int AnchorRow
        {
            get { return myRow; }
        }

        /// <summary>
        ///     The column where the count change is happening.
        /// </summary>
        public int Column
        {
            get { return myColumn; }
        }

        /// <summary>
        ///     The number of items added. Change will be negative for a deletion.
        /// </summary>
        public int Change
        {
            get { return myChange; }
        }

        /// <summary>
        ///     The parent row of the branch the change is occuring in. This can
        ///     be used to stop selection restoration for subitem columns from restoring
        ///     selection in a different subitem block.
        /// </summary>
        public int ParentRow
        {
            get { return myParentRow; }
        }

        /// <summary>
        ///     The number of blank items after the anchor row and before the changes.
        ///     This will only be set if IsExpansionToggle is true.
        /// </summary>
        public int BlanksAfterAnchor
        {
            get { return (myBlanksAfterAnchor == -1) ? 0 : myBlanksAfterAnchor; }
        }

        /// <summary>
        ///     Is this event firing as the result of an expansion being expanded or collapsed in the tree
        /// </summary>
        public bool IsExpansionToggle
        {
            get { return myBlanksAfterAnchor != -1; }
        }

        /// <summary>
        ///     Returns true if SubItemChanges does not return a null collection
        /// </summary>
        public bool HasSubItemChanges
        {
            get { return myColumnAdjustments != null; }
        }

        /// <summary>
        ///     The subitem adjustments that need to be made for this change. Sub item
        ///     adjustments are required if an expansion is made in a column that does
        ///     not affect the total number of items in the list (only applicable if there
        ///     are more than two columns).
        /// </summary>
        public SubItemColumnAdjustmentCollection SubItemChanges
        {
            get
            {
                if (myColumnAdjustmentsCollection == null
                    && myColumnAdjustments != null)
                {
                    myColumnAdjustmentsCollection = new SubItemColumnAdjustmentCollection(myColumnAdjustments);
                }
                return myColumnAdjustmentsCollection;
            }
        }
    }

}
