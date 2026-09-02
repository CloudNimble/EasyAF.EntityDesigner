// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Data returned by ITree.GetBlankExpansion
    /// </summary>
    internal struct BlankExpansionData
    {
        private readonly int myTopRow;
        private readonly int myLeftColumn;
        private readonly int myBottomRow;
        private readonly int myRightColumn;
        private readonly int myAnchorColumn;

        internal BlankExpansionData(int topRow, int leftColumn, int bottomRow, int rightColumn, int anchorColumn)
        {
            myTopRow = topRow;
            myLeftColumn = leftColumn;
            myBottomRow = bottomRow;
            myRightColumn = rightColumn;
            myAnchorColumn = anchorColumn;
        }

        internal BlankExpansionData(int row, int column)
        {
            myTopRow = myBottomRow = row;
            myLeftColumn = myRightColumn = myAnchorColumn = column;
        }

        /// <summary>
        ///     The top row of the expansion
        /// </summary>
        public int TopRow
        {
            get { return myTopRow; }
        }

        /// <summary>
        ///     The left column of the expansion
        /// </summary>
        public int LeftColumn
        {
            get { return myLeftColumn; }
        }

        /// <summary>
        ///     The bottom row of the expansion
        /// </summary>
        public int BottomRow
        {
            get { return myBottomRow; }
        }

        /// <summary>
        ///     The right column of the expansion
        /// </summary>
        public int RightColumn
        {
            get { return myRightColumn; }
        }

        /// <summary>
        ///     The anchor column. This will be the same as LeftColumn
        ///     unless column permutations are in place. The AnchorColumn
        ///     will be VirtualTreeConstant.NullIndex (-1) for a blank row.
        /// </summary>
        public int AnchorColumn
        {
            get { return myAnchorColumn; }
        }

        /// <summary>
        ///     Returns true if the expansion consists of a single cell
        /// </summary>
        public bool IsSingleCell
        {
            get { return myLeftColumn == myRightColumn && myTopRow == myBottomRow; }
        }

        /// <summary>
        ///     The number of columns in the expansion rectangle
        /// </summary>
        public int Width
        {
            get { return myRightColumn - myLeftColumn + 1; }
        }

        /// <summary>
        ///     The number of rows in the expansion rectangle
        /// </summary>
        public int Height
        {
            get { return myBottomRow - myTopRow + 1; }
        }

        /// <summary>
        ///     The anchor cell for the expansion. The coordinates of the this cell are
        ///     the only ones in the blank expansion that will return a non-blank VirtualTreeItemInfo
        ///     from ITree.GetItemInfo. However, if column permutations have been applied in such
        ///     a way that the entire row is blank (the UI should block this by not allowing root columns
        ///     with jagged branches or complex columns to be hidden), then the anchor will be invalid.
        /// </summary>
        public VirtualTreeCoordinate Anchor
        {
            get
            {
                if (myAnchorColumn == VirtualTreeConstant.NullIndex)
                {
                    return VirtualTreeCoordinate.Invalid;
                }
                return new VirtualTreeCoordinate(myTopRow, myAnchorColumn);
            }
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is BlankExpansionData)
            {
                return Compare(this, (BlankExpansionData)obj);
            }
            return false;
        }

        /// <summary>
        ///     GetHashCode override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // We're forced to override this with the Equals override.
            return base.GetHashCode();
        }

        /// <summary>
        ///     Equals operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator ==(BlankExpansionData operand1, BlankExpansionData operand2)
        {
            return Compare(operand1, operand2);
        }

        /// <summary>
        ///     Compare two BlankExpansionData structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>true if operands are equal</returns>
        public static bool Compare(BlankExpansionData operand1, BlankExpansionData operand2)
        {
            return operand1.myLeftColumn == operand2.myLeftColumn && operand1.myTopRow == operand2.myTopRow
                   && operand1.myRightColumn == operand2.myRightColumn && operand1.myBottomRow == operand2.myBottomRow
                   && operand1.myAnchorColumn == operand2.myAnchorColumn;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator !=(BlankExpansionData operand1, BlankExpansionData operand2)
        {
            return !Compare(operand1, operand2);
        }

        #endregion // Equals override and related functions
    }

}
