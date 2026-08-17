// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Drawing;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Structure representing row and column information in the VirtualTreeControl with additional
    ///     information about the location of different UI elements.
    /// </summary>
    internal struct VirtualTreeExtendedHitInfo
    {
        private VirtualTreeHitInfo myHitInfo;
        private ExtraHitInfo myExtraHitInfo;

        internal VirtualTreeExtendedHitInfo(ref VirtualTreeHitInfo hitInfo, ref ExtraHitInfo extraHitInfo)
        {
            myHitInfo = hitInfo;
            myExtraHitInfo = extraHitInfo;
        }

        /// <summary>
        ///     The row of the item. If the item is a blank, this is the resolved row, which may differ from the RawRow.
        /// </summary>
        public int Row
        {
            get { return myHitInfo.Row; }
        }

        /// <summary>
        ///     Information about the area of the item we're on. This can also include the
        ///     VirtualTreeHitTargets.OnBlankItem if the hit is not directly over an item.
        /// </summary>
        public VirtualTreeHitTargets HitTarget
        {
            get { return myHitInfo.HitTarget; }
        }

        /// <summary>
        ///     The column as currently displayed in the tree. If the item is on a blank, this
        ///     is resolved to the anchor column of the blank. If there is a ColumnPermutation active,
        ///     then this differs from the NativeColumn.
        /// </summary>
        public int DisplayColumn
        {
            get { return myHitInfo.DisplayColumn; }
        }

        /// <summary>
        ///     The native column of the hit item. The native column
        ///     is used with the ITree interface and can differ from
        ///     the DisplayColumn if a ColumnPermutation has been
        ///     applied to the tree.
        /// </summary>
        public int NativeColumn
        {
            get { return myHitInfo.NativeColumn; }
        }

        /// <summary>
        ///     The RawRow is the row actually hovered on. In the case of blank expansions,
        ///     RawRow will correspond to the row actually hovered over, while the
        ///     Row will correspond to the resolved blank item anchor.
        /// </summary>
        public int RawRow
        {
            get { return myHitInfo.RawRow; }
        }

        /// <summary>
        ///     The RawColumn is the column actually hovered on. In the case of blank expansions,
        ///     RawColumn will correspond to the column actually hovered over, while
        ///     DisplayColumn will correspond to the resolved blank item anchor, and NativeColumn
        ///     the corresponding column to use with the current Tree.
        /// </summary>
        public int RawColumn
        {
            get { return myHitInfo.RawColumn; }
        }

        /// <summary>
        ///     Returns true if the text portion of the item is obscured
        /// </summary>
        public bool IsTruncated
        {
            get { return myExtraHitInfo.IsTruncated; }
        }

        /// <summary>
        ///     The label and glyphs, clipped for string truncation. The
        ///     rectangle accounts for the current position of the horizontal
        ///     scrollbar, but it may not be clipped by the client boundaries
        ///     of the control.
        /// </summary>
        public Rectangle ClippedItemRectangle
        {
            get { return myExtraHitInfo.ClippedItemRectangle; }
        }

        /// <summary>
        ///     The full label rectangle without glyphs or truncation. The
        ///     rectangle accounts for the current position of the horizontal
        ///     scrollbar.
        /// </summary>
        public Rectangle FullLabelRectangle
        {
            get { return myExtraHitInfo.FullLabelRectangle; }
        }

        /// <summary>
        ///     The width of the glyph regions
        /// </summary>
        public int LabelOffset
        {
            get { return myExtraHitInfo.LabelOffset; }
        }

        /// <summary>
        ///     The font used to draw the item
        /// </summary>
        public Font LabelFont
        {
            get { return myExtraHitInfo.LabelFont; }
        }

        #region Equals override and related functions

        /// <summary>
        ///     VirtualTreeExtendedHitInfo structures should not be compared
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
        /// <returns>Returns base hashcode</returns>
        public override int GetHashCode()
        {
            // We're forced to override this with the Equals override.
            return base.GetHashCode();
        }

        /// <summary>
        ///     VirtualTreeExtendedHitInfo structures should not be compared
        /// </summary>
        public static bool operator ==(VirtualTreeExtendedHitInfo operand1, VirtualTreeExtendedHitInfo operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     VirtualTreeExtendedHitInfo structures should not be compared
        /// </summary>
        public static bool Compare(VirtualTreeExtendedHitInfo operand1, VirtualTreeExtendedHitInfo operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     VirtualTreeExtendedHitInfo structures should not be compared
        /// </summary>
        public static bool operator !=(VirtualTreeExtendedHitInfo operand1, VirtualTreeExtendedHitInfo operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return true;
        }

        #endregion // Equals override and related functions

    }

}
