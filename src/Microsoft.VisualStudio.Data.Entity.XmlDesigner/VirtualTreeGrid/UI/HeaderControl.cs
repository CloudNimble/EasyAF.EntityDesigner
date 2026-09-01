// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Portions of the VirtualTreeControl relating to displaying the column
    ///     header control. Because of the difficulties of showing the header as
    ///     a child control of the main control (very difficult to do because the
    ///     underlying listbox controls scrolling of the main area), we show it as
    ///     a sibling of the tree control.
    /// </summary>
    internal partial class VirtualTreeControl
    {
        private HeaderContainer myHeaderContainer;
        private ImageList myHeaderImageList;

        private void RepositionHeaderContainer()
        {
            Debug.Assert(!GetStateFlag(VTCStateFlags.WindowPositionChanging), "!GetStateFlag(VTCStateFlags.WindowPositionChanging)");
            if (myHeaderContainer == null)
            {
                return;
            }
            myHeaderContainer.Bounds = HeaderContainerPosition;
        }

        private Rectangle HeaderContainerPosition
        {
            get
            {
                var retVal = ClientRectangle;
                var offset = BorderOffset;
                var height = HeaderHeight;
                retVal.Height = height;
                if (HasVerticalScrollBar)
                {
                    var scrollWidth = SystemInformation.VerticalScrollBarWidth;
                    retVal.Width += scrollWidth;
                }
                retVal.Offset(offset, offset);
                retVal.Offset(Location);
                return retVal;
            }
        }

        /// <summary>
        ///     Control.OnParentChanged override. Updates the parent of the header container to match
        ///     the new parent
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (myHeaderContainer != null)
            {
                myHeaderContainer.Parent = null;
                var parentCtl = Parent;
                if (parentCtl != null)
                {
                    var controls = parentCtl.Controls;
                    myHeaderContainer.TabIndex = TabIndex;
                    controls.Add(myHeaderContainer);
                    controls.SetChildIndex(myHeaderContainer, controls.GetChildIndex(this));

                    if (!parentCtl.IsHandleCreated)
                    {
                        parentCtl.HandleCreated += OnParentHandleCreated;
                    }
                }
            }
        }

        /// <summary>
        ///     Listen for parent handle creation, to ensure we're in the right place in the Z-order.  Necessary because WinForms
        ///     calls that change Z-order prior to parent handle creation do not send WM_WINDOWPOSCHANGED (even if our handle
        ///     is created), so we do not receive notification.
        /// </summary>
        private void OnParentHandleCreated(object sender, EventArgs e)
        {
            Control parent = sender as Control;
            Debug.Assert(parent != null, "should have parent control");
            if (parent != null)
            {
                parent.HandleCreated -= OnParentHandleCreated;
                if (myHeaderContainer != null
                    && parent == Parent)
                {
                    myHeaderContainer.UpdateHeaderControlZOrder();
                }
            }
        }

        /// <summary>
        ///     Control.OnTabIndexChanged override. Updates the TabIndex of the header container.
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected override void OnTabIndexChanged(EventArgs e)
        {
            base.OnTabIndexChanged(e);
            myHeaderContainer?.TabIndex = TabIndex;
        }

        /// <summary>
        ///     Gives deriving classes a chance to modify the header control behavior
        /// </summary>
        /// <returns>A new header control</returns>
        protected virtual VirtualTreeHeaderControl CreateHeaderControl()
        {
            return new VirtualTreeHeaderControl(this);
        }

        /// <summary>
        ///     Retrieve the header control associated with this contrail.
        /// </summary>
        /// <returns>The associated header control, or null</returns>
        public VirtualTreeHeaderControl HeaderControl
        {
            get { return myHeaderContainer?.HeaderControl; }
        }

        /// <summary>
        ///     The image list used to show icons in the header control
        /// </summary>
        [DefaultValue(null)]
        public ImageList HeaderImageList
        {
            get { return myHeaderImageList; }
            set
            {
                myHeaderImageList = value;
                if (value != null
                    && myHeaderContainer != null
                    && myHeaderContainer.IsHandleCreated)
                {
                    NativeMethods.SendMessage(myHeaderContainer.HeaderControl.Handle, NativeMethods.HDM_SETIMAGELIST, 0, value.Handle);
                }
            }
        }

        private void UpdateHeaderControlStyle(int style, bool onOff, bool uiChange)
        {
            if (myHeaderContainer != null
                && myHeaderContainer.IsHandleCreated)
            {
                Control headerControl = myHeaderContainer.HeaderControl;
                if (headerControl.IsHandleCreated)
                {
                    var hWnd = headerControl.Handle;
                    var styles = (int)NativeMethods.GetWindowStyle(hWnd);
                    if (onOff)
                    {
                        styles |= style;
                    }
                    else
                    {
                        styles &= ~style;
                    }
                    NativeMethods.SetWindowLong(hWnd, NativeMethods.GWL_STYLE, styles);
                    if (uiChange)
                    {
                        NativeMethods.SetWindowPos(
                            hWnd, IntPtr.Zero, 0, 0, 0, 0,
                            NativeMethods.SetWindowPosFlags.SWP_NOMOVE | NativeMethods.SetWindowPosFlags.SWP_NOSIZE
                            | NativeMethods.SetWindowPosFlags.SWP_NOACTIVATE | NativeMethods.SetWindowPosFlags.SWP_NOZORDER);
                    }
                }
            }
        }

        /// <summary>
        ///     Enable column drag drop on the header control
        /// </summary>
        [DefaultValue(false)]
        public bool HeaderDragDrop
        {
            get { return GetStyleFlag(VTCStyleFlags.HeaderDragDrop); }
            set
            {
                if (value != GetStyleFlag(VTCStyleFlags.HeaderDragDrop))
                {
                    SetStyleFlag(VTCStyleFlags.HeaderDragDrop, value);
                    UpdateHeaderControlStyle(NativeMethods.HDS_DRAGDROP, value, false);
                }
            }
        }

        /// <summary>
        ///     Display the column headers with a button style instead of the default flat style
        /// </summary>
        [DefaultValue(false)]
        public bool HeaderButtons
        {
            get { return GetStyleFlag(VTCStyleFlags.HeaderButtons); }
            set
            {
                if (value != GetStyleFlag(VTCStyleFlags.HeaderButtons))
                {
                    SetStyleFlag(VTCStyleFlags.HeaderButtons, value);
                    UpdateHeaderControlStyle(NativeMethods.HDS_BUTTONS, value, true);
                }
            }
        }

        /// <summary>
        ///     Drag the full column header button while resizing a column instead of just
        ///     moving the splitter line.
        /// </summary>
        [DefaultValue(false)]
        public bool HeaderFullDrag
        {
            get { return GetStyleFlag(VTCStyleFlags.HeaderFullDrag); }
            set
            {
                if (value != GetStyleFlag(VTCStyleFlags.HeaderFullDrag))
                {
                    SetStyleFlag(VTCStyleFlags.HeaderFullDrag, value);
                    UpdateHeaderControlStyle(NativeMethods.HDS_FULLDRAG, value, false);
                }
            }
        }

        /// <summary>
        ///     Change the display of a column header when the mouse is moved over it.
        /// </summary>
        [DefaultValue(false)]
        public bool HeaderHotTrack
        {
            get { return GetStyleFlag(VTCStyleFlags.HeaderHotTrack); }
            set
            {
                if (value != GetStyleFlag(VTCStyleFlags.HeaderHotTrack))
                {
                    SetStyleFlag(VTCStyleFlags.HeaderHotTrack, value);
                    UpdateHeaderControlStyle(NativeMethods.HDS_HOTTRACK, value, true);
                }
            }
        }

        private class HeaderContainer : Control
        {
            private readonly VirtualTreeHeaderControl myHeader;

            public HeaderContainer(VirtualTreeControl associatedControl)
            {
                SetStyle(ControlStyles.ContainerControl, true);
                SetStyle(ControlStyles.Selectable, false);
                Size = new Size(0, 0);
                TabStop = false;
                Font = associatedControl.Font;
                myHeader = associatedControl.CreateHeaderControl();
                Controls.Add(myHeader);
            }

            /// <summary>
            ///     Returns the height required to display the header control with the current font.
            /// </summary>
            public int HeaderHeight
            {
                get { return (myHeader != null) ? myHeader.HeaderHeight : 0; }
            }

            /// <summary>
            ///     The header control in the container
            /// </summary>
            public VirtualTreeHeaderControl HeaderControl
            {
                get { return myHeader; }
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UpdateHeaderControlPosition(true);
            }

            /// <summary>
            ///     Control.SetBoundsCore override.  Ensures header container height is always the same as the underlying header.
            /// </summary>
            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified boundsSpecified)
            {
                if (0 != (boundsSpecified & BoundsSpecified.Height)
                    && myHeader != null)
                {
                    height = HeaderHeight;
                }

                base.SetBoundsCore(x, y, width, height, boundsSpecified);
            }

            public void UpdateHeaderControlPosition(bool updateColumnWidths)
            {
                if (myHeader != null)
                {
                    var associatedControl = myHeader.AssociatedControl;
                    if (associatedControl != null)
                    {
                        if (updateColumnWidths)
                        {
                            associatedControl.UpdateHeaderControlWidths(myHeader);
                        }
                        var rect = ClientRectangle;
                        var scrollPosition = associatedControl.myXPos;
                        if (scrollPosition != 0)
                        {
                            rect.X -= scrollPosition;
                            rect.Width += scrollPosition;
                        }
                        myHeader.Bounds = rect;
                    }
                }
            }

            public void UpdateHeaderControlZOrder()
            {
                // ensure we're always just after the associated tree control in the Z-order
                if (myHeader != null && IsHandleCreated)
                {
                    var parentCtl = Parent;
                    if (parentCtl != null)
                    {
                        var associatedControl = myHeader.AssociatedControl;
                        if (associatedControl != null
                            && associatedControl.IsHandleCreated
                            && parentCtl == associatedControl.Parent)
                        {
                            var treeControlIndex = parentCtl.Controls.GetChildIndex(associatedControl);
                            if (parentCtl.Controls.GetChildIndex(this) != treeControlIndex - 1)
                            {
                                // ensure we're always just before the tree control in the Z-order
                                parentCtl.Controls.SetChildIndex(this, treeControlIndex - 1 > 0 ? treeControlIndex - 1 : 0);
                            }
                        }
                    }
                }
            }

            /// <summary>
            ///     Control.WndProc override
            /// </summary>
            /// <param name="m">Message</param>
            [SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            protected override void WndProc(ref Message m)
            {
                try
                {
                    base.WndProc(ref m);
                }
                catch (Exception e)
                {
                    if (CriticalException.IsCriticalException(e))
                    {
                        throw;
                    }

                    DisplayException(myHeader.AssociatedControl.Site, e);
                }
            }
        }

        private void EnsureHeaderContainer()
        {
            if (myHeaderContainer == null)
            {
                if (myHeaderBounds.HasHeaders && DisplayColumnHeaders)
                {
                    myHeaderContainer = new HeaderContainer(this);

                    // forces evaluation of Handle
                    var handle = myHeaderContainer.Handle;
                }
            }
        }

        private void AttachHeaderContainer()
        {
            if (myHeaderContainer != null)
            {
                var parentCtl = Parent;
                if (parentCtl != null)
                {
                    // I would like this to be a child of the parent control without
                    // being in the controls collection of the parent. However, we 
                    // can't do this because it messes up the accessibility children
                    // of the parent (WinForms apparently doesn't like a Controls.Count
                    // different than the Windows child count). Lacking this support, we
                    // simply place the header control in the tab order before the tree
                    // control and turn off its tabstop property.
                    var containerHandle = myHeaderContainer.Handle; // forces evaluation of Handle
                    PopulateHeaderControl(myHeaderContainer.HeaderControl);
                    var controls = parentCtl.Controls;
                    if (!controls.Contains(myHeaderContainer))
                    {
                        myHeaderContainer.TabIndex = TabIndex;
                        controls.Add(myHeaderContainer);
                        // Change the child index to make sure the header control is in front of this one
                        // WinForms zorder is based first on TabIndex (equal in this case), then order in
                        // the Controls collection (earlier is on top).
                        controls.SetChildIndex(myHeaderContainer, controls.GetChildIndex(this));

                        if (!parentCtl.IsHandleCreated)
                        {
                            parentCtl.HandleCreated += OnParentHandleCreated;
                        }
                    }
                }
            }
        }
    }

}
