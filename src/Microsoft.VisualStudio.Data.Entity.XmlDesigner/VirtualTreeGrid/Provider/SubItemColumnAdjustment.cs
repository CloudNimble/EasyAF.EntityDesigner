// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Used with the OnToggleExpansion event to give change item change counts
    ///     for individual columns. Because multiple and complex subitems can introduce
    ///     blanks into a multi column tree grid, it is often possible to expand and
    ///     collapse items without adding or removing rows. Per-column adjustment counts
    ///     are needed to support subitem expansion in these cases without redrawing all
    ///     columns.
    /// </summary>
    internal struct SubItemColumnAdjustment
    {
        private readonly int myColumn;
        private readonly int myLastColumnOnRow;
        private readonly int myChange;
        private readonly int myContainedTrailingItems;
        private readonly int myItemsBelowAnchor;

        internal SubItemColumnAdjustment(int column, int lastColumnOnRow, int change, int containedTrailingItems, int itemsBelowAnchor)
        {
            myColumn = column;
            myChange = change;
            myContainedTrailingItems = containedTrailingItems;
            myLastColumnOnRow = lastColumnOnRow;
            myItemsBelowAnchor = itemsBelowAnchor;
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            Debug.Assert(false); // There is no need to compare these
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
        /// <returns>Always returns false, there is no need to compare two SubItemColumnAdjustment structures</returns>
        public static bool operator ==(SubItemColumnAdjustment operand1, SubItemColumnAdjustment operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     Compare two SubItemColumnAdjustment structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>Always returns false, there is no need to compare two SubItemColumnAdjustment structures</returns>
        public static bool Compare(SubItemColumnAdjustment operand1, SubItemColumnAdjustment operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>Always returns true, there is no need to compare two SubItemColumnAdjustment structures</returns>
        public static bool operator !=(SubItemColumnAdjustment operand1, SubItemColumnAdjustment operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return true;
        }

        #endregion // Equals override and related functions

        /// <summary>
        ///     The column number in the the grid
        /// </summary>
        public int Column
        {
            get { return myColumn; }
        }

        /// <summary>
        ///     The last column for the owning row of this column
        /// </summary>
        public int LastColumnOnRow
        {
            get { return myLastColumnOnRow; }
        }

        /// <summary>
        ///     The number of local items in the given column that need to
        ///     be added or removed.
        /// </summary>
        public int Change
        {
            get { return myChange; }
        }

        /// <summary>
        ///     The number of unchanged items below the change region that are still wholly contained
        ///     in the subitem column. This is used to optimize drawing of items that have not changed.
        /// </summary>
        public int ContainedTrailingItems
        {
            get { return myContainedTrailingItems; }
        }

        /// <summary>
        ///     The number of (pre-change) items after the anchor position that are contained in the
        ///     full subitem cell.
        /// </summary>
        public int ItemsBelowAnchor
        {
            get { return myItemsBelowAnchor; }
        }
    }

}
