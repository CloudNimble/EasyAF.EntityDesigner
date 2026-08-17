// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Structure representing row and column information in the VirtualTreeControl
    /// </summary>
    internal struct VirtualTreeHitInfo
    {
        private int myRow;
        private readonly VirtualTreeHitTargets myHitTarget;
        private readonly int myDisplayColumn;
        private readonly int myNativeColumn;
        private int myRawRow;
        private readonly int myRawColumn;

        internal VirtualTreeHitInfo(int row, int column, VirtualTreeHitTargets target)
        {
            myRow = myRawRow = row;
            myHitTarget = target;
            myDisplayColumn = myNativeColumn = myRawColumn = column;
        }

        internal VirtualTreeHitInfo(int row, int displayColumn, int nativeColumn, int rawRow, int rawColumn, VirtualTreeHitTargets target)
        {
            myRow = row;
            myHitTarget = target;
            myDisplayColumn = displayColumn;
            myNativeColumn = nativeColumn;
            myRawRow = rawRow;
            myRawColumn = rawColumn;
        }

        /// <summary>
        ///     The row of the item. If the item is a blank, this is the resolved row, which may differ from the RawRow.
        /// </summary>
        public int Row
        {
            get { return myRow; }
        }

        internal void ClearRowData()
        {
            myRow = myRawRow = VirtualTreeConstant.NullIndex;
        }

        /// <summary>
        ///     Information about the area of the item we're on. This can also include the
        ///     VirtualTreeHitTargets.OnBlankItem if the hit is not directly over an item.
        /// </summary>
        public VirtualTreeHitTargets HitTarget
        {
            get { return myHitTarget; }
        }

        /// <summary>
        ///     The column as currently displayed in the tree. If the item is on a blank, this
        ///     is resolved to the anchor column of the blank. If there is a ColumnPermutation active,
        ///     then this differs from the NativeColumn.
        /// </summary>
        public int DisplayColumn
        {
            get { return myDisplayColumn; }
        }

        /// <summary>
        ///     The native column of the hit item. The native column
        ///     is used with the ITree interface and can differ from
        ///     the DisplayColumn if a ColumnPermutation has been
        ///     applied to the tree.
        /// </summary>
        public int NativeColumn
        {
            get { return myNativeColumn; }
        }

        /// <summary>
        ///     The RawRow is the row actually hovered on. In the case of blank expansions,
        ///     RawRow will correspond to the row actually hovered over, while the
        ///     Row will correspond to the resolved blank item anchor.
        /// </summary>
        public int RawRow
        {
            get { return myRawRow; }
        }

        /// <summary>
        ///     The RawColumn is the column actually hovered on. In the case of blank expansions,
        ///     RawColumn will correspond to the column actually hovered over, while
        ///     DisplayColumn will correspond to the resolved blank item anchor, and NativeColumn
        ///     the corresponding column to use with the current Tree.
        /// </summary>
        public int RawColumn
        {
            get { return myRawColumn; }
        }

        #region Equals override and related functions

        /// <summary>
        ///     VirtualTreeHitInfo structures should not be compared
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     GetHashCode
        /// </summary>
        /// <returns>Returns base hash code</returns>
        public override int GetHashCode()
        {
            // We're forced to override this with the Equals override.
            return base.GetHashCode();
        }

        /// <summary>
        ///     VirtualTreeHitInfo structures should not be compared
        /// </summary>
        public static bool operator ==(VirtualTreeHitInfo operand1, VirtualTreeHitInfo operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     VirtualTreeHitInfo structures should not be compared
        /// </summary>
        public static bool Compare(VirtualTreeHitInfo operand1, VirtualTreeHitInfo operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     VirtualTreeHitInfo structures should not be compared
        /// </summary>
        public static bool operator !=(VirtualTreeHitInfo operand1, VirtualTreeHitInfo operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return true;
        }

        #endregion // Equals override and related functions

    }

}
