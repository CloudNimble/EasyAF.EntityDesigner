// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments for the IBranch.OnBranchModification event. Static
    ///     methods on this class are used to generate arguments for firing this
    ///     event, which modifies all parties using the branch that a modification
    ///     has occurred.
    /// </summary>
    internal abstract class BranchModificationEventArgs : EventArgs
    {
        private BranchModificationEventArgs()
        {
        }

        //These are marked internal so that the appropriate constructors are used
        //to simulate the methods on the tree branch instead of setting the fields directly

        private class BranchModificationMost : BranchModificationEventArgs
        {
            public BranchModificationMost(
                BranchModificationAction action,
                IBranch branch,
                int index,
                int count)
            {
                Action = action;
                Branch = branch;
                Index = index;
                Count = count;
                Flag = false;
            }

            public BranchModificationMost(
                BranchModificationAction action,
                bool flag)
            {
                Action = action;
                Flag = flag;
                Branch = null;
            }

            public BranchModificationMost(
                BranchModificationAction action,
                IBranch branch,
                int index,
                int count,
                bool flag)
            {
                Action = action;
                Branch = branch;
                Index = index;
                Count = count;
                Flag = flag;
            }

            public BranchModificationMost(
                BranchModificationAction action,
                IBranch branch)
            {
                Action = action;
                Branch = branch;
            }
        }

        internal class BranchModificationDisplayData : BranchModificationEventArgs
        {
            // The rest are used for DisplayDataChanged only
            internal int Column;
            internal VirtualTreeDisplayDataChanges Changes;

            public BranchModificationDisplayData(BranchModificationAction action, ref DisplayDataChangedData changeData)
            {
                Action = action;
                Changes = changeData.Changes;
                Branch = changeData.Branch;
                Index = changeData.StartRow;
                Column = changeData.Column;
                Count = changeData.Count;
                Flag = false;
            }
        }

        internal class BranchModificationLevelShift : BranchModificationEventArgs
        {
            // The rest are used for ShiftBranchLevels only
            internal int Depth;
            internal int NewCount;
            internal int RemoveLevels;
            internal int InsertLevels;
            internal IBranch ReplacementBranch;
            internal ILevelShiftAdjuster BranchTester;

            public BranchModificationLevelShift(BranchModificationAction action, ref ShiftBranchLevelsData shiftData)
            {
                Action = action;
                Branch = shiftData.Branch;
                Flag = false;
                Index = shiftData.StartIndex;
                Count = shiftData.Count;
                Depth = shiftData.Depth;
                NewCount = shiftData.NewCount;
                RemoveLevels = shiftData.RemoveLevels;
                InsertLevels = shiftData.InsertLevels;
                ReplacementBranch = shiftData.ReplacementBranch;
                BranchTester = shiftData.BranchTester;
            }
        }

        /// <summary>
        ///     The displayed data for the branch has changed, and
        ///     any views on this tree need to be notified.
        /// </summary>
        /// <param name="changeData">The DisplayDataChangedData structure</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs DisplayDataChanged(DisplayDataChangedData changeData)
        {
            return new BranchModificationDisplayData(BranchModificationAction.DisplayDataChanged, ref changeData);
        }

        /// <summary>
        ///     The order and count of the items in this branch has changed.
        /// </summary>
        /// <param name="branch">The branch to modify, or null for all branches</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs Realign(IBranch branch)
        {
            return new BranchModificationMost(BranchModificationAction.Realign, branch);
        }

        /// <summary>
        ///     Use to insert items without forcing a realign. InsertItems should be used
        ///     to adjust an existing branch: do not add a node with no children then immediately
        ///     call InsertItems repeatedly to add children. The branches VisibleItemCount is
        ///     assumed to be adjusted for the new items before a call to InsertItems. If the
        ///     items are inserted at the end of the visible portion of the branch, then
        ///     VisibleItemCount will be used to determine if the new items are hidden or visible.
        /// </summary>
        /// <param name="branch">The branch where items have been inserted.</param>
        /// <param name="after">The index to insert after. Use -1 to insert at the beginning</param>
        /// <param name="count">The number of items that have been inserted.</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs InsertItems(IBranch branch, int after, int count)
        {
            return new BranchModificationMost(BranchModificationAction.InsertItems, branch, after, count);
        }

        /// <summary>
        ///     Delete specific items without calling Realign
        /// </summary>
        /// <param name="branch">The branch where items have been removed</param>
        /// <param name="start">The first item to deleted</param>
        /// <param name="count">The number of items to delete</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs DeleteItems(IBranch branch, int start, int count)
        {
            return new BranchModificationMost(BranchModificationAction.DeleteItems, branch, start, count);
        }

        /// <summary>
        ///     Change the position of a single item in a branch
        /// </summary>
        /// <param name="branch">The branch where the item moved</param>
        /// <param name="fromRow">The row the item used to be on</param>
        /// <param name="toRow">The row the item is on now</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs MoveItem(IBranch branch, int fromRow, int toRow)
        {
            return new BranchModificationMost(BranchModificationAction.MoveItem, branch, fromRow, toRow);
        }

        /// <summary>
        ///     Views on the tree should not attempt to redraw any items when Redraw is off.
        ///     Calls to Redraw can be nested, and must be balanced.
        /// </summary>
        /// <param name="value">false to turn off redraw, true to restore it.</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs Redraw(bool value)
        {
            return new BranchModificationMost(BranchModificationAction.Redraw, value);
        }

        /// <summary>
        ///     Used to batch calls to Redraw without triggering unnecessary redraw operations.
        ///     Set this property to true if an operation may cause one or more redraw calls, then
        ///     to false on completion. The cost is negligible if Redraw is never triggered, whereas
        ///     an unneeded Redraw true/false can be very expensive. Calls to DelayRedraw can
        ///     be nested, and must be balanced.
        /// </summary>
        /// <param name="value">true to delay, false to finish operation</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs DelayRedraw(bool value)
        {
            return new BranchModificationMost(BranchModificationAction.DelayRedraw, value);
        }

        /// <summary>
        ///     A significant change is being made to the layout of the list, so give
        ///     any views on this object the chance to cache selection information. Calls to
        ///     ListShuffle can be nested, and must be balanced.
        /// </summary>
        /// <param name="value">true to start a shuffle (cache state), false to end one</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs ListShuffle(bool value)
        {
            return new BranchModificationMost(BranchModificationAction.ListShuffle, value);
        }

        /// <summary>
        ///     Used to batch calls to ListShuffle without triggering unnecessary shuffle operations.
        ///     Set this property to true if an operation may cause one or more list shuffles, then
        ///     to false on completion. The cost is negligible if a shuffle is never triggered, whereas
        ///     an unneeded ListShuffle true/false can be very expensive. Calls to DelayListShuffle can
        ///     be nested, and must be balanced.
        /// </summary>
        /// <param name="value">true to delay, false to finish operation</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs DelayListShuffle(bool value)
        {
            return new BranchModificationMost(BranchModificationAction.DelayListShuffle, value);
        }

        /// <summary>
        ///     Add or remove entire levels from existing branch structures in the tree
        /// </summary>
        /// <param name="shiftData">The data for the shift operation</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs ShiftBranchLevels(ShiftBranchLevelsData shiftData)
        {
            return new BranchModificationLevelShift(BranchModificationAction.ShiftBranchLevels, ref shiftData);
        }

        /// <summary>
        ///     A mechanism for changing a cell from simple or expandable
        ///     to complex, or vice versa. This enables a potentially
        ///     complex cell to begin life as a simple cell, then switch later.
        ///     The makeComplex variable is interpreted according to the
        ///     cell style settings for the given branch.
        /// </summary>
        /// <param name="branch">The branch to modify</param>
        /// <param name="row">Target row</param>
        /// <param name="column">Target column</param>
        /// <param name="makeComplex">True to switch to a complex cell, false to switch to a simple cell</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs UpdateCellStyle(IBranch branch, int row, int column, bool makeComplex)
        {
            return new BranchModificationMost(BranchModificationAction.UpdateCellStyle, branch, row, column, makeComplex);
        }

        /// <summary>
        ///     Remove all occurrences of branch from the tree. Note that removing all
        ///     items from a branch is not the same as removing the branch itself.
        /// </summary>
        /// <param name="branch">The branch to remove</param>
        /// <returns>An events args object for IBranch.OnBranchModification</returns>
        public static BranchModificationEventArgs RemoveBranch(IBranch branch)
        {
            return new BranchModificationMost(BranchModificationAction.RemoveBranch, branch);
        }

        /// <summary>
        ///     The action represented by this branch modification
        /// </summary>
        public BranchModificationAction Action { get; set; }

        /// <summary>
        ///     The Branch property corresponds to the branch parameter, if present, for all events
        /// </summary>
        public IBranch Branch { get; set; }

        /// <summary>
        ///     The Flag property corresponds to different input parameters for different events.
        ///     Redraw:value,
        ///     DelayRedraw:value,
        ///     ListShuffle:value,
        ///     DelayListShuffle:value,
        ///     UpdateCellStyle:makeComplex,
        /// </summary>
        public bool Flag { get; set; }

        /// <summary>
        ///     The Index property corresponds to different input parameters for different events.
        ///     DisplayDataChanged:startIndex,
        ///     InsertItems:after,
        ///     DeleteItems:start,
        ///     ShiftBranchLevels:row,
        ///     DisplayDataChanged:count,
        ///     UpdateCellStyle:column,
        ///     MoveItem:from
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     The Count property corresponds to different input parameters for different events.
        ///     InsertItems:count,
        ///     DeleteItems:count,
        ///     ShiftBranchLevels:count,
        ///     DisplayDataChanged:count,
        ///     UpdateCellStyle:column,
        ///     MoveItem:to
        /// </summary>
        public int Count { get; set; }
    }

}
