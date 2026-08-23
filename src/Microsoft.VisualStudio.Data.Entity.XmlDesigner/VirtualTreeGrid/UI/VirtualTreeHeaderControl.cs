// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     The control used to display column headers for the VirtualTreeControl
    /// </summary>
    internal class VirtualTreeHeaderControl : Control
    {
        #region Internal state flags

        [Flags]
        private enum StateFlags
        {
            /// <summary>
            ///     Currently drag
            /// </summary>
            Dragging = 1,

            /// <summary>
            ///     Received a WM_CANCELMODE or an escape key
            /// </summary>
            DragCanceled = 2,

            /// <summary>
            ///     Currently tracking
            /// </summary>
            Tracking = 4,

            /// <summary>
            ///     Received a successful end track
            /// </summary>
            HeaderTracked = 8,

            /// <summary>
            ///     An HDN_ENDTRACK notification has been received
            /// </summary>
            ReceivedTrack = 0x10,

            /// <summary>
            ///     Skip validation generally performed during the HDM_SETORDERARRAY message
            /// </summary>
            IgnoreSetOrderArray = 0x20,
        }

        private StateFlags myFlags;

        private bool GetFlag(StateFlags bit)
        {
            return (myFlags & bit) == bit;
        }

        private void SetFlag(StateFlags bit, bool value)
        {
            if (value)
            {
                myFlags |= bit;
            }
            else
            {
                myFlags &= ~bit;
            }
        }

        #endregion // Internal state flags

        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private int myHeaderHeight;

        private readonly VirtualTreeControl myAssociatedControl;
        private ArrayList myOwnerDrawItemQueue;

        private VirtualTreeHeaderControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            Size = new Size(0, 0);
            SetStyle(ControlStyles.Selectable, false);
        }

        /// <summary>
        ///     Create a new VirtualTreeHeaderControl to associate with a specific VirtualTreeControl. Should
        ///     only be called directly by VirtualTreeControl.CreateHeaderControl, or indirectly by an override
        ///     of that function and a constructor from a derived class.
        /// </summary>
        /// <param name="associatedControl"></param>
        public VirtualTreeHeaderControl(VirtualTreeControl associatedControl)
            : this()
        {
            myAssociatedControl = associatedControl;
        }

        /// <summary>
        ///     The VirtualTreeControl associated with this header control
        /// </summary>
        public VirtualTreeControl AssociatedControl
        {
            get { return myAssociatedControl; }
        }

        /// <summary>
        ///     Returns true if a header or splitter is being dragged
        /// </summary>
        public bool AdjustingColumnHeader
        {
            get
            {
                if (0 != (myFlags & (StateFlags.Dragging | StateFlags.Tracking)))
                {
                    return !GetFlag(StateFlags.DragCanceled);
                }
                return false;
            }
        }

        /// <summary>
        ///     Control.CreateHandle override. Ensures that the common controls dll is loaded.
        /// </summary>
        protected override void CreateHandle()
        {
            NativeMethods.INITCOMMONCONTROLSEX icc = new NativeMethods.INITCOMMONCONTROLSEX();
            icc.dwICC = NativeMethods.ICC_LISTVIEW_CLASSES;
            NativeMethods.InitCommonControlsEx(icc);
            base.CreateHandle();
        }

        /// <summary>
        ///     Control.OnHandleCreate override. Attaches the image list.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var headerImages = myAssociatedControl.HeaderImageList;
            if (headerImages != null)
            {
                NativeMethods.SendMessage(Handle, NativeMethods.HDM_SETIMAGELIST, 0, headerImages.Handle);
            }
        }

        /// <summary>
        ///     Control.SetBoundsCore override.  Ensures header height is always the same as the underlying header.
        /// </summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified boundsSpecified)
        {
            if (0 != (boundsSpecified & BoundsSpecified.Height))
            {
                height = HeaderHeight;
            }

            base.SetBoundsCore(x, y, width, height, boundsSpecified);
        }

        /// <summary>
        ///     Remove all items from the header control
        /// </summary>
        public void Clear()
        {
            if (IsHandleCreated)
            {
                var hWnd = Handle;
                var itemCount = (int)NativeMethods.SendMessage(hWnd, NativeMethods.HDM_GETITEMCOUNT, 0, 0);
                for (var i = itemCount - 1; i >= 0; --i)
                {
                    NativeMethods.SendMessage(hWnd, NativeMethods.HDM_DELETEITEM, i, 0);
                }
            }
        }

        /// <summary>
        ///     Add an item to the control. Always adds in the last position.
        /// </summary>
        /// <param name="columnHeader">The header information</param>
        /// <param name="itemWidth"></param>
        public void AddItem(VirtualTreeColumnHeader columnHeader, int itemWidth)
        {
            NativeMethods.HDITEM item = new NativeMethods.HDITEM();
            item.cxy = itemWidth;
            item.mask = NativeMethods.HDITEM.Mask.HDI_WIDTH;
            SetAppearanceFields(ref columnHeader, ref item);
            NativeMethods.SendMessage(Handle, NativeMethods.HDM_INSERTITEMW, int.MaxValue, ref item);
        }

        /// <summary>
        ///     Update the appearance fields (string, glyph, and style) to the new header settings. This
        ///     cannot be used to update the width or position of the header.
        /// </summary>
        /// <param name="columnHeader">The header with the current settings</param>
        /// <param name="displayColumn">The display column to update</param>
        public void UpdateItemAppearance(VirtualTreeColumnHeader columnHeader, int displayColumn)
        {
            if (IsHandleCreated)
            {
                NativeMethods.HDITEM item = new NativeMethods.HDITEM();
                SetAppearanceFields(ref columnHeader, ref item);
                if (item.mask != 0)
                {
                    NativeMethods.SendMessage(Handle, NativeMethods.HDM_SETITEMW, OrderToIndex(displayColumn), ref item);
                }
            }
        }

        private void SetAppearanceFields(ref VirtualTreeColumnHeader columnHeader, ref NativeMethods.HDITEM item)
        {
            var text = columnHeader.Text;
            var style = columnHeader.Style;
            var imageIndex = columnHeader.ImageIndex;
            var usingFormat = false;

            // Set the text
            if (text != null
                && text.Length != 0)
            {
                item.pszText = text;
                item.mask |= NativeMethods.HDITEM.Mask.HDI_TEXT;
                item.fmt |= NativeMethods.HDITEM.Format.HDF_STRING;
                usingFormat = true;
            }

            // Set the image
            if (imageIndex >= 0
                && myAssociatedControl.HeaderImageList != null)
            {
                item.iImage = imageIndex;
                item.mask |= NativeMethods.HDITEM.Mask.HDI_IMAGE;
                item.fmt |= NativeMethods.HDITEM.Format.HDF_IMAGE;
                usingFormat = true;
            }

            // Update style fields
            if ((style & ~(VirtualTreeColumnHeaderStyles.DragDisabled | VirtualTreeColumnHeaderStyles.ColumnPositionLocked)) != 0)
            {
                usingFormat = true;

                // Set the alignment
                if (0 != (style & VirtualTreeColumnHeaderStyles.AlignCenter))
                {
                    item.fmt |= NativeMethods.HDITEM.Format.HDF_CENTER;
                }
                else if (0 != (style & VirtualTreeColumnHeaderStyles.AlignRight))
                {
                    item.fmt |= NativeMethods.HDITEM.Format.HDF_RIGHT;
                }

                // Set the image location
                if (0 != (style & VirtualTreeColumnHeaderStyles.ImageOnRight))
                {
                    item.fmt |= NativeMethods.HDITEM.Format.HDF_BITMAP_ON_RIGHT;
                }

                // Set the up or down arrows
                if (0 != (style & VirtualTreeColumnHeaderStyles.DisplayUpArrow))
                {
                    item.fmt |= NativeMethods.HDITEM.Format.HDF_SORTUP;
                }
                else if (0 != (style & VirtualTreeColumnHeaderStyles.DisplayDownArrow))
                {
                    item.fmt |= NativeMethods.HDITEM.Format.HDF_SORTDOWN;
                }
            }

            // Set owner draw
            if (0 != (style & (VirtualTreeColumnHeaderStyles.OwnerDraw | VirtualTreeColumnHeaderStyles.OwnerDrawOverlay)))
            {
                usingFormat = true;
                item.fmt |= NativeMethods.HDITEM.Format.HDF_OWNERDRAW;
            }

            if (usingFormat)
            {
                item.mask |= NativeMethods.HDITEM.Mask.HDI_FORMAT;
            }
        }

        /// <summary>
        ///     Change the width of the given column. This is a helper function to
        ///     facilitate the final step in updating the header. SetItemWidth does
        ///     not update the data in the tree control's VirtualTreeColumnHeader
        ///     structures.
        /// </summary>
        /// <param name="index">The display column</param>
        /// <param name="itemWidth">The new width for the column</param>
        public void SetItemWidth(int index, int itemWidth)
        {
            // The header items can get out of sync here with drag/drop reordering. The
            // index requested here will always be the displayed order, not the true index,
            // so we need to switch to the true index before moving on.
            index = OrderToIndex(index);
            NativeMethods.HDITEM item = new NativeMethods.HDITEM();
            item.cxy = itemWidth;
            item.mask = NativeMethods.HDITEM.Mask.HDI_WIDTH;
            NativeMethods.SendMessage(Handle, NativeMethods.HDM_SETITEMW, index, ref item);
        }

        /// <summary>
        ///     Override on Control.CreateParams. Specifies this control as a standard windows
        ///     header control and applies the styles specified with the Header* properties on the
        ///     associated control
        /// </summary>
        protected override CreateParams CreateParams
        {
            [SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                SetStyle(ControlStyles.ContainerControl, false);
                SetStyle(ControlStyles.UserPaint, false);
                TabStop = false;
                var cp = base.CreateParams;
                cp.ClassName = NativeMethods.WC_HEADER;
                cp.Style |= NativeMethods.HDS_HORZ;
                if (myAssociatedControl.HeaderButtons)
                {
                    cp.Style |= NativeMethods.HDS_BUTTONS;
                }
                if (myAssociatedControl.HeaderDragDrop)
                {
                    cp.Style |= NativeMethods.HDS_DRAGDROP;
                }
                if (myAssociatedControl.HeaderFullDrag)
                {
                    cp.Style |= NativeMethods.HDS_FULLDRAG;
                }
                if (myAssociatedControl.HeaderHotTrack)
                {
                    cp.Style |= NativeMethods.HDS_HOTTRACK;
                }
                return cp;
            }
        }

        // Return false to block the change
        private bool OnPotentialOrderChange(int[] oldOrder, int[] newOrder, bool updateHeaderControl)
        {
            var columns = oldOrder.Length;
            var i = 0;
            for (i = 0; i < columns; ++i)
            {
                if (oldOrder[i] != newOrder[i])
                {
                    break;
                }
            }
            if (i == columns)
            {
                return true;
            }
            return OnOrderChange(oldOrder, newOrder, updateHeaderControl);
        }

        /// <summary>
        ///     The column order is being changed.
        /// </summary>
        /// <param name="oldOrder">The old order. Acts as a base order to determine new order</param>
        /// <param name="newOrder">The new order. Compare columns to old order to deduce current display order.</param>
        /// <param name="updateHeaderControl">Set to true if the headers should be updated during this function</param>
        /// <returns>Return true to allow the change, false to block it</returns>
        protected virtual bool OnOrderChange(int[] oldOrder, int[] newOrder, bool updateHeaderControl)
        {
            if (myAssociatedControl != null)
            {
                return myAssociatedControl.ChangeColumnOrder(oldOrder, newOrder, updateHeaderControl);
            }
            return true;
        }

        /// <summary>
        ///     Control.PreProcessMessage override. Catches escape key to enable cancel of header drag operations.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [SecuritySafeCritical]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public override bool PreProcessMessage(ref Message msg)
        {
            switch (msg.Msg)
            {
                case NativeMethods.WM_KEYDOWN:
                    if ((Keys)msg.WParam == Keys.Escape)
                    {
                        SetFlag(StateFlags.DragCanceled, true);
                    }
                    break;
            }
            return base.PreProcessMessage(ref msg);
        }

        /// <summary>
        ///     Control.WndProc override
        /// </summary>
        [SecuritySafeCritical]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            try
            {
                switch (m.Msg)
                {
                    case NativeMethods.WM_GETOBJECT:
                        // Call DefWndProc here to get the Windows standard accessibility object for the header control.
                        // base.WndProc provides the WinForms object, which does not work correctly on Vista (bug 89037). 
                        DefWndProc(ref m);
                        return;
                    case NativeMethods.WM_SETCURSOR:
                        // We want the standard header cursors, not the default WinForms.Control behavior
                        DefWndProc(ref m);
                        return;
                    case NativeMethods.HDM_SETORDERARRAY:
                        if (!GetFlag(StateFlags.IgnoreSetOrderArray))
                        {
                            var itemCount = (int)NativeMethods.SendMessage(m.HWnd, NativeMethods.HDM_GETITEMCOUNT, 0, 0);
                            if (itemCount != (int)m.WParam)
                            {
                                // Sanity test
                                m.Result = IntPtr.Zero;
                                return;
                            }
                            var oldOrder = new int[itemCount];
                            NativeMethods.SendMessage(m.HWnd, NativeMethods.HDM_GETORDERARRAY, oldOrder.Length, oldOrder);
                            var newOrder = new int[itemCount];
                            Marshal.Copy(m.LParam, newOrder, 0, itemCount);
                            if (OnPotentialOrderChange(oldOrder, newOrder, false))
                            {
                                base.WndProc(ref m);
                                myAssociatedControl.UpdateHeaderControlWidths(this);
                            }
                            else
                            {
                                m.Result = IntPtr.Zero;
                            }
                            return;
                        }
                        break;
                    case NativeMethods.WM_LBUTTONUP:
                        if (GetFlag(StateFlags.Dragging)
                            && !GetFlag(StateFlags.DragCanceled))
                        {
                            // I'd like to do this in respose to HDM_SETORDERARRAY, but the control
                            // does not fire this when it does the resize on its own.
                            var oldOrder = new int[(int)NativeMethods.SendMessage(m.HWnd, NativeMethods.HDM_GETITEMCOUNT, 0, 0)];
                            NativeMethods.SendMessage(m.HWnd, NativeMethods.HDM_GETORDERARRAY, oldOrder.Length, oldOrder);
                            base.WndProc(ref m);
                            if (!GetFlag(StateFlags.Dragging))
                            {
                                var newOrder = new int[oldOrder.Length];
                                NativeMethods.SendMessage(m.HWnd, NativeMethods.HDM_GETORDERARRAY, newOrder.Length, newOrder);
                                if (!OnPotentialOrderChange(oldOrder, newOrder, true))
                                {
                                    SetFlag(StateFlags.IgnoreSetOrderArray, true);
                                    NativeMethods.SendMessage(m.HWnd, NativeMethods.HDM_SETORDERARRAY, oldOrder.Length, oldOrder);
                                    SetFlag(StateFlags.IgnoreSetOrderArray, false);
                                }
                            }
                            SetFlag(StateFlags.Dragging, false); // In case enddrag didn't fire for whatever reason
                            return;
                        }
                        else if (GetFlag(StateFlags.HeaderTracked))
                        {
                            myAssociatedControl.UpdateHeaderControlWidths(this);
                        }
                        break;
                    case NativeMethods.WM_LBUTTONDOWN:
                        SetFlag(StateFlags.HeaderTracked | StateFlags.Tracking | StateFlags.DragCanceled, false);
                        break;
                    case NativeMethods.WM_MOUSEMOVE:
                        if (GetFlag(StateFlags.Tracking))
                        {
                            // If we're tracking, then the range we allow is not the same as the range
                            // allowed by the header control. However, there is no way to stop the header
                            // control from drawing its line, so we simply never let the mouse move message
                            // with the out-of-bounds x position to get through to the control.
                            var currentPoint = PointToScreen(new Point((int)m.LParam));
                            var newPoint = LimitTrackedMousePosition(currentPoint);
                            if (newPoint != currentPoint)
                            {
                                // Do this with a SendMessage instead of recreating a Message structure
                                // so that normal processing can occur and GetMessagePos returns the constrained value
                                // downstream from here.
                                newPoint = PointToClient(newPoint);
                                m.Result = NativeMethods.SendMessage(
                                    m.HWnd, m.Msg, (int)m.WParam, NativeMethods.MAKELONG(newPoint.X, newPoint.Y));
                                return;
                            }
                            // When we're in full drag mode, the control doesn't send a track notification. Send one now
                            // so that the lines draw correctly.
                            if (!GetFlag(StateFlags.ReceivedTrack))
                            {
                                base.WndProc(ref m);
                                if (!GetFlag(StateFlags.ReceivedTrack))
                                {
                                    TrackSplitter(NativeMethods.GetMessagePos());
                                }
                                return;
                            }
                        }
                        break;
                    case NativeMethods.WM_CONTEXTMENU:
                        {
                            var notifyPoint = NativeMethods.GetMessagePos();
                            NativeMethods.HDHITTESTINFO hitInfo = new NativeMethods.HDHITTESTINFO(PointToClient(notifyPoint));
                            var index = (int)NativeMethods.SendMessage(m.HWnd, NativeMethods.HDM_HITTEST, 0, ref hitInfo);
                            if (index != -1
                                && (0 != (hitInfo.flags & NativeMethods.HDHITTESTINFO.Flags.HHT_ONHEADER)))
                            {
                                myAssociatedControl.OnRawColumnHeaderEvent(
                                    IndexToOrder(index), VirtualTreeColumnHeaderClickStyle.ContextMenu, notifyPoint);
                            }
                            break;
                        }
                    case NativeMethods.WM_CANCELMODE:
                        SetFlag(StateFlags.DragCanceled, true);
                        break;
                    case NativeMethods.WM_PAINT:
                        if (GetStyle(ControlStyles.UserPaint))
                        {
                            // if UserPaint is turned on, we can't do owner-draw, so just let
                            // the base handle it.
                            base.WndProc(ref m);
                        }
                        else
                        {
                            myOwnerDrawItemQueue?.Clear();
                            NativeMethods.PAINTSTRUCT ps = new NativeMethods.PAINTSTRUCT();
                            var dc = NativeMethods.BeginPaint(Handle, ref ps);
                            var oldPal = NativeMethods.SelectPalette(dc, Graphics.GetHalftonePalette(), 1);
                            try
                            {
                                using (Graphics g = Graphics.FromHdc(dc))
                                {
                                    try
                                    {
                                        m.WParam = dc; // per MSDN, header control will use this DC for painting if provided.
                                        DefWndProc(ref m);
                                            // DefWndProc send the WM_DRAWITEM messages we will use to populate myOwnerDrawItemQueue.
                                        if (myOwnerDrawItemQueue != null)
                                        {
                                            for (var i = 0; i < myOwnerDrawItemQueue.Count; i++)
                                            {
                                                WmReflectDrawItem((NativeMethods.DRAWITEMSTRUCT)myOwnerDrawItemQueue[i], g);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        NativeMethods.EndPaint(Handle, ref ps);
                                    }
                                }
                            }
                            finally
                            {
                                if (oldPal != IntPtr.Zero)
                                {
                                    NativeMethods.SelectPalette(dc, oldPal, 0);
                                }
                            }
                        }
                        return;
                    case NativeMethods.WM_REFLECT + NativeMethods.WM_DRAWITEM:
                        if (myAssociatedControl != null)
                        {
                            NativeMethods.DRAWITEMSTRUCT dis = (NativeMethods.DRAWITEMSTRUCT)m.GetLParam(typeof(NativeMethods.DRAWITEMSTRUCT));
                            // transform itemId into a native column index. this will get passed in 
                            // the DrawItemEventArgs via the DrawColumnHeaderItem event.
                            dis.itemId = IndexToOrder(dis.itemId);
                            if (myAssociatedControl.ColumnPermutation != null)
                            {
                                dis.itemId = myAssociatedControl.ColumnPermutation.GetNativeColumn(dis.itemId);
                            }
                            var header = myAssociatedControl.GetColumnHeader(dis.itemId);

                            // Queue the draw item message.  This allows us to do overlay drawing, because we can let
                            // the underlying control paint first if necessary.
                            myOwnerDrawItemQueue ??= [];
                            myOwnerDrawItemQueue.Add(dis);

                            // return 0 here if we want the column header to draw normally (i.e., we're doing overlay drawing)
                            // otherwise 1 to indicate no further handling of the message is necessary.  Either way we'll
                            // send the DrawHeaderControlItem event at the end of WM_PAINT processing since we've added an entry to the queue.
                            m.Result = (0 != (header.Style & VirtualTreeColumnHeaderStyles.OwnerDrawOverlay)) ? IntPtr.Zero : (IntPtr)1;
                        }
                        return;
                    case NativeMethods.WM_REFLECT + NativeMethods.WM_NOTIFY:
                        if (myAssociatedControl != null)
                        {
                            IntPtr ptr = Marshal.ReadIntPtr(m.LParam, IntPtr.Size * 2);
                            // OR operation to convert 0xFFFFFEBA to 0xFFFFFFFFFFFFFEBA
                            long code = unchecked((Environment.Is64BitProcess ? ptr.ToInt64() : ptr.ToInt32()) | (long)0xFFFFFFFF00000000);
                            switch (code)
                            {
                                case NativeMethods.HDN_BEGINTRACKW:
                                    {
                                        if (
                                            !BeginTrackingSplitter(
                                                IndexToOrder(Marshal.ReadInt32(m.LParam, NativeMethods.NMHEADER.iItemOffset)),
                                                NativeMethods.GetMessagePos()))
                                        {
                                            m.Result = (IntPtr)1; // Block the tracking
                                            return;
                                        }
                                        SetFlag(StateFlags.DragCanceled, false);
                                        SetFlag(StateFlags.Tracking, true);
                                        SetFlag(StateFlags.ReceivedTrack, false);
                                        return;
                                    }
                                case NativeMethods.HDN_TRACKW:
                                    SetFlag(StateFlags.ReceivedTrack, true);
                                    if (GetFlag(StateFlags.DragCanceled))
                                    {
                                        m.Result = (IntPtr)1;
                                    }
                                    else
                                    {
                                        TrackSplitter(NativeMethods.GetMessagePos());
                                    }
                                    return;
                                case NativeMethods.HDN_ENDTRACKW:
                                    bool canceled = GetFlag(StateFlags.DragCanceled);
                                    SetFlag(StateFlags.Tracking, false);
                                    if (!canceled
                                        && !GetFlag(StateFlags.ReceivedTrack))
                                    {
                                        // Need at least one track notification so that FinishTrackingSplitter works correctly
                                        TrackSplitter(NativeMethods.GetMessagePos());
                                    }
                                    FinishTrackingSplitter(canceled);
                                    if (!canceled)
                                    {
                                        SetFlag(StateFlags.HeaderTracked, true);
                                    }
                                    // Always cancel at this point, we'll pick this up in the mouse up.
                                    // This way, the resize algorithms don't have to be exactly the same
                                    m.Result = (IntPtr)1;
                                    return;
                                case NativeMethods.HDN_BEGINDRAG:
                                    if (GetFlag(StateFlags.DragCanceled))
                                    {
                                        m.Result = (IntPtr)1;
                                    }
                                    else
                                    {
                                        var index = Marshal.ReadInt32(m.LParam, NativeMethods.NMHEADER.iItemOffset);
                                        if (index < 0)
                                        {
                                            // Not sure why this is happening, but handle it
                                        }
                                        else if (BeginDrag(IndexToOrder(index)))
                                        {
                                            SetFlag(StateFlags.Dragging, true);
                                            SetFlag(StateFlags.DragCanceled, false);
                                        }
                                        else
                                        {
                                            SetFlag(StateFlags.DragCanceled, true);
                                            m.Result = (IntPtr)1;
                                        }
                                    }
                                    return;
                                case NativeMethods.HDN_ENDDRAG:
                                    SetFlag(StateFlags.Dragging, false);
                                    return;
                                case NativeMethods.HDN_ITEMCLICKW:
                                case NativeMethods.HDN_ITEMDBLCLICKW:
                                case NativeMethods.HDN_DIVIDERDBLCLICKW:
                                    {
                                        VirtualTreeColumnHeaderClickStyle style;
                                        switch (code)
                                        {
                                            case NativeMethods.HDN_ITEMCLICKW:
                                                style = VirtualTreeColumnHeaderClickStyle.Click;
                                                break;
                                            case NativeMethods.HDN_ITEMDBLCLICKW:
                                                style = VirtualTreeColumnHeaderClickStyle.DoubleClick;
                                                break;
                                            case NativeMethods.HDN_DIVIDERDBLCLICKW:
                                                style = VirtualTreeColumnHeaderClickStyle.DividerDoubleClick;
                                                break;
                                            default:
                                                Debug.Assert(false, "false"); // Shouldn't be here
                                                return;
                                        }
                                        myAssociatedControl.OnRawColumnHeaderEvent(
                                            IndexToOrder(Marshal.ReadInt32(m.LParam, NativeMethods.NMHEADER.iItemOffset)), style,
                                            NativeMethods.GetMessagePos());
                                        break;
                                    }
                            }
                        }
                        break;
                }
                base.WndProc(ref m);
            }
            catch (Exception e)
            {
                if (CriticalException.IsCriticalException(e))
                {
                    throw;
                }

                VirtualTreeControl.DisplayException(myAssociatedControl.Site, e);
            }
        }

        internal void WmReflectDrawItem(NativeMethods.DRAWITEMSTRUCT dis, Graphics g)
        {
            Rectangle bounds = Rectangle.FromLTRB(dis.rcItem.left, dis.rcItem.top, dis.rcItem.right, dis.rcItem.bottom);
            if (!ClientRectangle.IntersectsWith(bounds))
            {
                return;
            }

            myAssociatedControl.OnDrawColumnHeaderItem(
                new DrawItemEventArgs(g, Font, bounds, dis.itemId, (DrawItemState)dis.itemState, ForeColor, BackColor));
        }

        /// <summary>
        ///     Start tracking a column. TrackSplitter and FinishTrackingSplitter
        ///     should also be overriden if an override for this function does
        ///     not call the base implementation.
        /// </summary>
        /// <param name="displayColumn">The column being adjusted</param>
        /// <param name="mousePosition">The current position of the mouse, in screen coordinates</param>
        /// <returns>True if tracking is enabled for this column</returns>
        protected virtual bool BeginTrackingSplitter(int displayColumn, Point mousePosition)
        {
            return myAssociatedControl.BeginTrackingSplitter(displayColumn, mousePosition);
        }

        /// <summary>
        ///     Track a splitter. Called multiple times between BeginTrackingSplitter and FinishTrackingSplitter
        /// </summary>
        /// <param name="mousePosition">The current position of the mouse, in screen coordinates</param>
        protected virtual void TrackSplitter(Point mousePosition)
        {
            myAssociatedControl.TrackSplitter(mousePosition);
        }

        /// <summary>
        ///     Limit the position of the mouse during tracking. The header control
        ///     is more liberal about this than the tree control, and will happy
        ///     draw its tracking line in an out of bounds position. If LimitTrackedMousePosition
        ///     returns a point that is not equal to the incoming point, then the current mouse
        ///     move will be eaten and a new one generated in the constrained position.
        /// </summary>
        /// <param name="mousePosition">The mouse position requested by a mouse move</param>
        /// <returns>The constrained position</returns>
        protected virtual Point LimitTrackedMousePosition(Point mousePosition)
        {
            return myAssociatedControl.LimitTrackedMousePosition(mousePosition);
        }

        /// <summary>
        ///     Finish tracking operation begun by BeginTrackingSplitter.
        /// </summary>
        /// <param name="cancel">True if the operation has been canceled</param>
        protected virtual void FinishTrackingSplitter(bool cancel)
        {
            myAssociatedControl.FinishSplitterAdjustment(cancel);
        }

        /// <summary>
        ///     Called when a drag operation is about to begin
        /// </summary>
        /// <param name="displayColumn">The column being dragged</param>
        /// <returns>true to enable the drag operation to continue</returns>
        protected virtual bool BeginDrag(int displayColumn)
        {
            return myAssociatedControl.BeginHeaderDrag(displayColumn);
        }

        /// <summary>
        ///     Get the current display column for a header index, such as the header
        ///     index sent with the NMHEADER structure to HDN_* notification messages.
        /// </summary>
        /// <param name="index">The header index</param>
        /// <returns>The column the header is currently displayed at.</returns>
        protected int IndexToOrder(int index)
        {
            if (IsHandleCreated)
            {
                NativeMethods.HDITEM item = new NativeMethods.HDITEM();
                item.iOrder = index;
                item.mask = NativeMethods.HDITEM.Mask.HDI_ORDER;
                NativeMethods.SendMessage(Handle, NativeMethods.HDM_GETITEMW, index, ref item);
                return item.iOrder;
            }
            return index;
        }

        /// <summary>
        ///     Get the index of the underlying header item based on display order.
        /// </summary>
        /// <param name="displayIndex">The order index</param>
        /// <returns>The index of the header item associated with the display column.</returns>
        protected int OrderToIndex(int displayIndex)
        {
            if (IsHandleCreated)
            {
                return (int)NativeMethods.SendMessage(Handle, NativeMethods.HDM_ORDERTOINDEX, displayIndex, 0);
            }
            return displayIndex;
        }

        /// <summary>
        ///     Returns the height required to display the header control with the current font.
        /// </summary>
        public virtual int HeaderHeight
        {
            get
            {
                if (myHeaderHeight > 0)
                {
                    return myHeaderHeight;
                }
                else if (IsHandleCreated)
                {
                    var hWnd = Handle;
                    NativeMethods.HDLAYOUT layout = new NativeMethods.HDLAYOUT();
                    NativeMethods.RECT rect = new NativeMethods.RECT();
                    GCHandle pRect = GCHandle.Alloc(rect, GCHandleType.Pinned);
                    GCHandle pWindowPos = GCHandle.Alloc(new NativeMethods.WINDOWPOS(), GCHandleType.Pinned);
                    layout.prc = pRect.AddrOfPinnedObject();
                    layout.pwpos = pWindowPos.AddrOfPinnedObject();
                    NativeMethods.SendMessage(hWnd, NativeMethods.HDM_LAYOUT, 0, ref layout);
                    NativeMethods.WINDOWPOS wp = (NativeMethods.WINDOWPOS)Marshal.PtrToStructure(layout.pwpos, typeof(NativeMethods.WINDOWPOS));
                    pWindowPos.Free();
                    pRect.Free();
                    myHeaderHeight = wp.cy;
                    return myHeaderHeight;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        ///     Control.OnFontChanged override. Flag the height to be recalculated
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            myHeaderHeight = -1;
        }
    }

}
