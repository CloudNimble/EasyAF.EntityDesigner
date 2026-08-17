// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Drawing;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     The default class to use an inplace label edit.
    /// </summary>
    internal class VirtualTreeInPlaceEditControl : TextBox, IVirtualTreeInPlaceControl
    {
        #region Boilerplate InPlaceControl code

        private readonly VirtualTreeInPlaceControlHelper myInPlaceHelper;

        VirtualTreeControl IVirtualTreeInPlaceControlDefer.Parent
        {
            get { return VirtualTreeInPlaceControlDeferParent; }
            set { VirtualTreeInPlaceControlDeferParent = value; }
        }

        /// <summary>
        ///     The tree control that activates this object.
        /// </summary>
        protected VirtualTreeControl VirtualTreeInPlaceControlDeferParent
        {
            get { return myInPlaceHelper.Parent; }
            set { myInPlaceHelper.Parent = value; }
        }

        Control IVirtualTreeInPlaceControlDefer.InPlaceControl
        {
            get { return InPlaceControl; }
        }

        /// <summary>
        ///     The control that implements this interface
        /// </summary>
        protected Control InPlaceControl
        {
            get { return myInPlaceHelper.InPlaceControl; }
        }

        VirtualTreeInPlaceControls IVirtualTreeInPlaceControlDefer.Flags
        {
            get { return Flags; }
            set { Flags = value; }
        }

        /// <summary>
        ///     Flags governing control appearance/behavior
        /// </summary>
        protected VirtualTreeInPlaceControls Flags
        {
            get { return myInPlaceHelper.Flags; }
            set { myInPlaceHelper.Flags = value; }
        }

        /// <summary>
        ///     Windows message used to launch the control. Defers to helper.
        /// </summary>
        int IVirtualTreeInPlaceControlDefer.LaunchedByMessage
        {
            get { return LaunchedByMessage; }
            set { LaunchedByMessage = value; }
        }

        /// <summary>
        ///     The windows message (WM_LBUTTONDOWN) that launched the edit
        ///     control. Use 0 for for a timer, selection change, or explicit
        ///     launch. Used to support mouse behavior while a control is in-place
        ///     activating in response to a mouse click.
        /// </summary>
        protected int LaunchedByMessage
        {
            get { return myInPlaceHelper.LaunchedByMessage; }
            set { myInPlaceHelper.LaunchedByMessage = value; }
        }

        bool IVirtualTreeInPlaceControlDefer.Dirty
        {
            get { return Dirty; }
            set { Dirty = value; }
        }

        /// <summary>
        ///     Dirty state of the in-place control.  Defers to helper.
        /// </summary>
        protected bool Dirty
        {
            get { return myInPlaceHelper.Dirty; }
            set { myInPlaceHelper.Dirty = value; }
        }

        /// <summary>
        ///     Notify the inplace helper that a key is down if
        ///     the event was not handled.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                e.Handled = myInPlaceHelper.OnKeyDown(e);
            }
        }

        /// <summary>
        ///     Notify the inplace helper that a key was pressed if the
        ///     key was not handled.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (!e.Handled)
            {
                e.Handled = myInPlaceHelper.OnKeyPress(e);
            }
        }

        /// <summary>
        ///     Notify the inplace helper that the text changed
        /// </summary>
        /// <param name="e"></param>
        protected override void OnTextChanged(EventArgs e)
        {
            myInPlaceHelper.OnTextChanged();
            base.OnTextChanged(e);
        }

        /// <summary>
        ///     Notify the inplace helper that focus was lost
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            myInPlaceHelper.OnLostFocus();
            base.OnLostFocus(e);
        }

        #endregion // Boilerplace InPlaceControl code

        #region Custom InPlaceControl callbacks

        void IVirtualTreeInPlaceControl.SelectAllText()
        {
            SelectAllText();
        }

        /// <summary>
        ///     Contains control-specific code to select all text in the window.
        /// </summary>
        protected void SelectAllText()
        {
            // Note that myEdit.SelectAll() is not quite the same as the following code
            var hwndEdit = Handle;
            NativeMethods.SendMessage(hwndEdit, NativeMethods.EM_SETSEL, -1, -1); // move to the end
            NativeMethods.SendMessage(hwndEdit, NativeMethods.EM_SETSEL, 0, -1); // select all text
        }

        int IVirtualTreeInPlaceControl.SelectionStart
        {
            get { return SelectionStart; }
            set { SelectionStart = value; }
        }

        int IVirtualTreeInPlaceControl.MaxTextLength
        {
            get { return MaxTextLength; }
            set { MaxTextLength = value; }
        }

        /// <summary>
        ///     Contains control-specific code to set the MaxTextLength property.
        /// </summary>
        protected int MaxTextLength
        {
            get { return MaxLength; }
            set { MaxLength = value; }
        }

        Rectangle IVirtualTreeInPlaceControl.FormattingRectangle
        {
            get { return FormattingRectangle; }
        }

        /// <summary>
        ///     Get the formatting rectangle of the text. See EM_GETRECT for the definition
        ///     of a formatting rectangle;
        /// </summary>
        protected Rectangle FormattingRectangle
        {
            get
            {
                // EM_GETRECT already takes EM_GETMARGINS into account, so don't use both.
                var hwndEdit = Handle;
                NativeMethods.SendMessage(hwndEdit, NativeMethods.EM_GETRECT, 0, out NativeMethods.RECT rcFormat);
                return new Rectangle(rcFormat.left, rcFormat.top, rcFormat.width, rcFormat.height);
            }
        }

        int IVirtualTreeInPlaceControl.ExtraEditWidth
        {
            get { return ExtraEditWidth; }
        }

        /// <summary>
        ///     Implementation of IVirtualTreeInPlaceControl.ExtraEditWidth
        /// </summary>
        protected static int ExtraEditWidth
        {
            get { return VirtualTreeInPlaceControlHelper.DefaultExtraEditWidth; }
        }

        #endregion // Custom InPlaceControl callbacks

        #region Required WndProc override

        /// <summary>
        ///     WndProc override
        /// </summary>
        /// <param name="m"></param>
        [SecuritySafeCritical]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            try
            {
                switch (m.Msg)
                {
                        // The tree grid will dispose this control on a MouseWheel
                        // event, but the base control class does not handle the case
                        // of the disappearing control correctly. The DefWndProc call
                        // here will forward the call back up the parent chain. Uncomment
                        // the following code to get to the OnMouseWheel override and/or
                        // fire the event, or handle MOUSEWHEEL code in line at this point.
                    case NativeMethods.WM_MOUSEWHEEL:
                        DefWndProc(ref m);
                        //if (!IsDisposed)
                        //{
                        //	Point p = new Point ((int)(short)m.LParam, (int)m.LParam >> 16);
                        //	p = PointToClient(p);
                        //	OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, p.X, p.Y, (int)m.WParam >> 16));
                        //}
                        break;
                    default:
                        base.WndProc(ref m);
                        break;
                }
            }
            catch (Exception e)
            {
                if (CriticalException.IsCriticalException(e))
                {
                    throw;
                }

                myInPlaceHelper.DisplayException(e);
            }
        }

        #endregion // Required WndProc override

        /// <summary>
        ///     Create a new inplace edit control
        /// </summary>
        public VirtualTreeInPlaceEditControl()
        {
            myInPlaceHelper = VirtualTreeControl.CreateInPlaceControlHelper(this);
            BorderStyle = BorderStyle.FixedSingle;
            TextAlign = HorizontalAlignment.Left;
        }

        /// <summary>
        ///     Changes default CreateParams. Turns off ES_AUTOVSCROLL and turns on ES_AUTOHSCROLL.
        /// </summary>
        protected override CreateParams CreateParams
        {
            [SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                var cp = base.CreateParams;
                cp.Style |= NativeMethods.ES_AUTOHSCROLL;
                cp.Style &= ~NativeMethods.ES_AUTOVSCROLL;
                return cp;
            }
        }
    }

}
