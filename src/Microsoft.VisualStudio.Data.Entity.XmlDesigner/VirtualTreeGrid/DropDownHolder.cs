// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid
{

    /// <summary>
    ///     Borrowed from the property browser.  Control to contain a child control which can be used
    ///     to do editing
    /// </summary>
    internal class DropDownHolder : Form, IMouseHookClient
    {
        internal Control CurrentControl = null;
        internal const int Border = 1;
        private readonly TypeEditorHost _dropDownParent;

        // we use this to hook mouse downs, etc. to know when to close the dropdown.
        private readonly MouseHooker _mouseHooker;

        // all the resizing goo...
        private bool _resizable = true; // true if we're showing the resize widget.
        private bool _resizing; // true if we're in the middle of a resize operation.
        private bool _resizeUp; // true if the dropdown is above the grid row, which means the resize widget is at the top.
        private Point _dragStart = Point.Empty; // the point at which the drag started to compute the delta
        private Rectangle _dragBaseRect = Rectangle.Empty; // the original bounds of our control.
        private int _currentMoveType = MoveTypeNone; // what type of move are we processing? left, bottom, or both?

        // the width of the vertical resize area at the bottom
        private static readonly int _resizeBorderSize = SystemInformation.HorizontalScrollBarHeight / 2;

        // the minimum size for the control
        private static readonly Size _minDropDownSize = 
            new Size(SystemInformation.VerticalScrollBarWidth * 4, SystemInformation.HorizontalScrollBarHeight * 4);

        // our cached size grip glyph.  Control paint only does right bottom glyphs, so we cache a mirrored one.
        // See GetSizeGripGlyph
        private Bitmap _sizeGripGlyph;

        internal const int DropDownHolderBorder = 1;
        private const int MoveTypeNone = 0x0;
        private const int MoveTypeBottom = 0x1;
        private const int MoveTypeLeft = 0x2;
        private const int MoveTypeTop = 0x4;

        internal DropDownHolder(TypeEditorHost dropDownParent)
        {
            ShowInTaskbar = false;
            ControlBox = false;
            MinimizeBox = false;
            MaximizeBox = false;
            Text = String.Empty;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual; // set this to avoid being moved when our handle is created.
            _mouseHooker = new MouseHooker(this, this);
            Visible = false;
            BackColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey);
            _dropDownParent = dropDownParent;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                cp.Style |= NativeMethods.WS_POPUP | NativeMethods.WS_BORDER;
                if (OSFeature.IsPresent(SystemParameter.DropShadow))
                {
                    cp.ClassStyle |= NativeMethods.CS_DROPSHADOW;
                }

                if (_dropDownParent != null)
                {
                    cp.Parent = _dropDownParent.Handle;
                }
                return cp;
            }
        }

        public virtual bool HookMouseDown
        {
            get { return _mouseHooker.HookMouseDown; }
            set { _mouseHooker.HookMouseDown = value; }
        }

        /// <devdoc>
        ///     This gets set to true if there isn't enough space below the currently selected
        ///     row for the drop down, so it appears above the row.  In this case, we make the resize
        ///     grip appear at the top left.
        /// </devdoc>
        public bool ResizeUp
        {
            get { return _resizeUp; }
            set
            {
                if (_resizeUp != value)
                {
                    // clear the glyph so we regenerate it.
                    //
                    _sizeGripGlyph = null;
                    _resizeUp = value;
                    if (_resizable)
                    {
                        DockPadding.Bottom = 0;
                        DockPadding.Top = 0;
                        if (value)
                        {
                            DockPadding.Top = SystemInformation.HorizontalScrollBarHeight;
                        }
                        else
                        {
                            DockPadding.Bottom = SystemInformation.HorizontalScrollBarHeight;
                        }
                    }
                }
            }
        }

        protected override void DestroyHandle()
        {
            _mouseHooker.Dispose();
            base.DestroyHandle();
        }

        public void DoModalLoop()
        {
            // Push a modal loop.  This kind of stinks, but I think it is a
            // better user model than returning from DropDownControl immediately.
            //  
            while (Visible)
            {
                Application.DoEvents();
                int result = NativeMethods.MsgWaitForMultipleObjects(0, Array.Empty<IntPtr>(), true, 250, NativeMethods.QS_ALLINPUT);
                Debug.Assert(result != NativeMethods.WAIT_FAILED, "result != NativeMethods.WAIT_FAILED");
            }
        }

        public virtual Control Component
        {
            get { return CurrentControl; }
        }

        /// <devdoc>
        ///     Get a glyph for sizing the lower left hand grip.  The code in ControlPaint only does lower-right glyphs
        ///     so we do some GDI+ magic to take that glyph and mirror it.  That way we can still share the code (in case it changes for theming, etc),
        ///     not have any special cases, and possibly solve world hunger.
        /// </devdoc>
        private Bitmap GetSizeGripGlyph(Graphics g)
        {
            if (_sizeGripGlyph != null)
            {
                return _sizeGripGlyph;
            }

            var scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
            var scrollBarHeight = SystemInformation.HorizontalScrollBarHeight;

            // create our drawing surface based on the current graphics context.
            //
            _sizeGripGlyph = new Bitmap(scrollBarWidth, scrollBarHeight, g);
            using (Graphics glyphGraphics = Graphics.FromImage(_sizeGripGlyph))
            {
                // mirror the image around the x-axis to get a gripper handle that works
                // for the lower left.
                using (Matrix m = new Matrix())
                {

                    // basically, mirroring is just scaling by -1 on the X-axis.  So any point that's like (10, 10) goes to (-10, 10). 
                    // that mirrors it, but also moves everything to the negative axis, so we just bump the whole thing over by it's width.
                    // 
                    // the +1 is because things at (0,0) stay at (0,0) since [0 * -1 = 0] so we want to get them over to the other side too.
                    //
                    // resizeUp causes the image to also be mirrored vertically so the grip can be used as a top-left grip instead of bottom-left.
                    //
                    m.Translate(scrollBarWidth + 1, (_resizeUp ? scrollBarHeight + 1 : 0));
                    m.Scale(-1, (ResizeUp ? -1 : 1));
                    glyphGraphics.Transform = m;
                    ControlPaint.DrawSizeGrip(glyphGraphics, BackColor, 0, 0, scrollBarWidth, scrollBarHeight);
                    glyphGraphics.ResetTransform();
                }
            }

            _sizeGripGlyph.MakeTransparent(BackColor);
            return _sizeGripGlyph;
        }

        public virtual bool GetUsed()
        {
            return (CurrentControl != null);
        }

        public virtual void FocusComponent()
        {
            if (CurrentControl != null && Visible)
            {
                CurrentControl.Focus();
            }
        }

        bool IMouseHookClient.OnClickHooked()
        {
            _dropDownParent.CloseDropDown(true);
            return false;
        }

        private void OnCurrentControlResize(object o, EventArgs e)
        {
            if (CurrentControl != null
                && !_resizing
                && !CurrentControl.Disposing)
            {
                var oldWidth = Width;
                Size newSize = new Size(2 * DropDownHolderBorder + CurrentControl.Width, 2 * DropDownHolderBorder + CurrentControl.Height);

                if (_resizable)
                {
                    newSize.Height += SystemInformation.HorizontalScrollBarHeight;
                }

                try
                {
                    _resizing = true;
                    SuspendLayout();
                    Size = newSize;
                }
                finally
                {
                    _resizing = false;
                    ResumeLayout(false);
                }
                Left -= (Width - oldWidth);
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            try
            {
                _resizing = true;
                base.OnLayout(levent);
            }
            finally
            {
                _resizing = false;
            }
        }

        /// <devdoc>
        ///     Just figure out what kind of sizing we would do at a given drag location.
        /// </devdoc>
        private int MoveTypeFromPoint(int x, int y)
        {
            var scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
            var scrollBarHeight = SystemInformation.HorizontalScrollBarHeight;
            Rectangle bRect = new Rectangle(0, Height - scrollBarHeight, scrollBarWidth, scrollBarHeight);
            Rectangle tRect = new Rectangle(0, 0, scrollBarWidth, scrollBarHeight);

            if (!ResizeUp
                && bRect.Contains(x, y))
            {
                return MoveTypeLeft | MoveTypeBottom;
            }
            else if (ResizeUp && tRect.Contains(x, y))
            {
                return MoveTypeLeft | MoveTypeTop;
            }
            else if (!ResizeUp
                     && Math.Abs(Height - y) < _resizeBorderSize)
            {
                return MoveTypeBottom;
            }
            else if (ResizeUp && Math.Abs(y) < _resizeBorderSize)
            {
                return MoveTypeTop;
            }

            return MoveTypeNone;
        }

        /// <devdoc>
        ///     Decide if we're going to be sizing at the given point, and if so, Capture and safe our current state.
        /// </devdoc>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _currentMoveType = MoveTypeFromPoint(e.X, e.Y);
                if (_currentMoveType != MoveTypeNone)
                {
                    _dragStart = PointToScreen(new Point(e.X, e.Y));
                    _dragBaseRect = Bounds;
                    Capture = true;
                }
                else
                {
                    _dropDownParent.CloseDropDown(true);
                }
            }

            base.OnMouseDown(e);
        }

        /// <devdoc>
        ///     Either set the cursor or do a move, depending on what our currentMoveType is/
        /// </devdoc>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (! _resizable)
            {
                // don't do any resizing ops
            }
            else
                // not moving so just set the cursor.
                //
                if (_currentMoveType == MoveTypeNone)
                {
                    var cursorMoveType = MoveTypeFromPoint(e.X, e.Y);

                    switch (cursorMoveType)
                    {
                        case (MoveTypeLeft | MoveTypeBottom):
                            Cursor = Cursors.SizeNESW;
                            break;

                        case MoveTypeBottom:
                        case MoveTypeTop:
                            Cursor = Cursors.SizeNS;
                            break;

                        case MoveTypeTop | MoveTypeLeft:
                            Cursor = Cursors.SizeNWSE;
                            break;

                        default:
                            Cursor = null;
                            break;
                    }
                }
                else
                {
                    var dragPoint = PointToScreen(new Point(e.X, e.Y));
                    var newBounds = Bounds;

                    // we're in a move operation, so do the resize.
                    //
                    if ((_currentMoveType & MoveTypeBottom) == MoveTypeBottom)
                    {
                        newBounds.Height = Math.Max(_minDropDownSize.Height, _dragBaseRect.Height + (dragPoint.Y - _dragStart.Y));
                    }

                    // for left and top moves, we actually have to resize and move the form simultaneously.
                    // do to that, we compute the xdelta, and apply that to the base rectangle if it's not going to
                    // make the form smaller than the minimum.
                    //
                    if ((_currentMoveType & MoveTypeTop) == MoveTypeTop)
                    {
                        var delta = dragPoint.Y - _dragStart.Y;

                        if ((_dragBaseRect.Height - delta) > _minDropDownSize.Height)
                        {
                            newBounds.Y = _dragBaseRect.Top + delta;
                            newBounds.Height = _dragBaseRect.Height - delta;
                        }
                    }

                    if ((_currentMoveType & MoveTypeLeft) == MoveTypeLeft)
                    {
                        var delta = dragPoint.X - _dragStart.X;

                        if ((_dragBaseRect.Width - delta) > _minDropDownSize.Width)
                        {
                            newBounds.X = _dragBaseRect.Left + delta;
                            newBounds.Width = _dragBaseRect.Width - delta;
                        }
                    }

                    if (newBounds != Bounds)
                    {
                        try
                        {
                            _resizing = true;
                            Bounds = newBounds;
                        }
                        finally
                        {
                            _resizing = false;
                        }
                    }

                    // Redraw!
                    //
                    var scrollBarHeight = SystemInformation.HorizontalScrollBarHeight;

                    Invalidate(new Rectangle(0, Height - scrollBarHeight, Width, scrollBarHeight));
                }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            // just clear the cursor back to the default.
            //
            Cursor = null;
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                // reset the world.
                //
                _currentMoveType = MoveTypeNone;
                _dragStart = Point.Empty;
                _dragBaseRect = Rectangle.Empty;
                Capture = false;
            }
        }

        /// <devdoc>
        ///     Just paint and draw our glyph.
        /// </devdoc>
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            if (_resizable)
            {
                var scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                var scrollBarHeight = SystemInformation.HorizontalScrollBarHeight;
                Rectangle lRect = new Rectangle(0, ResizeUp ? 0 : Height - scrollBarHeight, scrollBarWidth, scrollBarHeight);

                pe.Graphics.DrawImage(GetSizeGripGlyph(pe.Graphics), lRect);
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            try
            {
                if ((keyData & (Keys.Shift | Keys.Control | Keys.Alt)) == 0)
                {
                    var accept = false;
                    var doClose = false;
                    switch (keyData & Keys.KeyCode)
                    {
                        case Keys.Escape:
                            doClose = true;
                            //accept = false; // default value
                            break;
                        case Keys.Enter:
                            doClose = accept = true;
                            break;
                    }
                    if (doClose)
                    {
                        _dropDownParent.CloseDropDown(accept);
                        return true;
                    }
                }
                return base.ProcessDialogKey(keyData);
            }
            catch (Exception e)
            {
                if (CriticalException.IsCriticalException(e))
                {
                    throw;
                }

                VirtualTreeControl.DisplayException(_dropDownParent.Site, e);
                return false;
            }
        }

        public void SetComponent(Control ctl, bool resizable)
        {
            _resizable = resizable;

            // clear any existing control we have
            //
            if (CurrentControl != null)
            {
                CurrentControl.Resize -= OnCurrentControlResize;
                Controls.Remove(CurrentControl);
                CurrentControl = null;
            }

            // now set up the new control, top to bottom
            //
            if (ctl != null)
            {
                Size sz = new Size(2 * DropDownHolderBorder + ctl.Width, 2 * DropDownHolderBorder + ctl.Height);

                // set the size stuff.
                //
                try
                {
                    SuspendLayout();
                    // if we're resizable, add the space for the widget. Make sure
                    // this happens with layout off or you get the side effect of shrinking
                    // the contained control.
                    if (resizable)
                    {
                        var hscrollHeight = SystemInformation.HorizontalScrollBarHeight;
                        sz.Height += hscrollHeight + hscrollHeight;
                            // UNDONE: This is bizarre. But if we don't double the height adjustment, we lose that much each time the control is shown

                        // we use dockpadding to save space to draw the widget.
                        //
                        if (ResizeUp)
                        {
                            DockPadding.Top = hscrollHeight;
                        }
                        else
                        {
                            DockPadding.Bottom = hscrollHeight;
                        }
                    }
                    Size = sz;
                    ctl.Dock = DockStyle.Fill;
                    ctl.Visible = true;
                    Controls.Add(ctl);
                }
                finally
                {
                    ResumeLayout(true);
                }
                CurrentControl = ctl;

                // hook the resize event.
                //
                CurrentControl.Resize += OnCurrentControlResize;
            }

            Enabled = CurrentControl != null;
        }

        /// <summary>
        ///     Control.WndProc override
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == NativeMethods.WM_ACTIVATE)
                {
                    // SetState(STATE_MODAL, true);
                    var activatedControl = FromHandle(m.LParam);
                    if (Visible
                        && NativeMethods.UnsignedLOWORD(m.WParam) == NativeMethods.WA_INACTIVE
                        && (activatedControl == null || (!Contains(activatedControl) && !TypeEditorHost.HandleContains(Handle, m.LParam))))
                    {
                        // Notification occurs when the drop-down holder
                        // is de-activated via a click outside the drop-down area.
                        // call CloseDropDown(true) to commit any changes.
                        _dropDownParent.CloseDropDown(true);
                        return;
                    }
                }
                else if (m.Msg == NativeMethods.WM_CLOSE)
                {
                    // don't let an ALT-F4 get you down
                    //
                    if (Visible)
                    {
                        _dropDownParent.CloseDropDown(false);
                    }
                    return;
                }

                base.WndProc(ref m);
            }
            catch (Exception e)
            {
                if (CriticalException.IsCriticalException(e))
                {
                    throw;
                }

                VirtualTreeControl.DisplayException(_dropDownParent.Site, e);
            }
        }
    }

}
