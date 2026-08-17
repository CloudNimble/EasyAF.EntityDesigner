// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid
{

    /// <summary>
    ///     Borrowed from property browser.  Installs a mouse hook so we can close the drop down if the
    ///     user clicks the mouse while it's open
    /// </summary>
    internal class MouseHooker : IDisposable
    {
        private readonly Control _control;
        private readonly IMouseHookClient _client;

        internal int ThisProcessId = 0;
        private GCHandle _mouseHookRoot;
        private IntPtr _mouseHookHandle = IntPtr.Zero;

        private const bool HookDisable = false;

        private bool _processing;

        public MouseHooker(Control control, IMouseHookClient client)
        {
            _control = control;
            _client = client;
        }

        public virtual bool HookMouseDown
        {
            get { return _mouseHookHandle != IntPtr.Zero; }
            set
            {
                if (value && !HookDisable)
                {
                    HookMouse();
                }
                else
                {
                    UnhookMouse();
                }
            }
        }

        #region IDisposable

        ~MouseHooker()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnhookMouse();
            }
        }

        #endregion

        /// <devdoc>
        ///     Sets up the needed windows hooks to catch messages.
        /// </devdoc>
        /// <internalonly />
        private void HookMouse()
        {
            lock (this)
            {
                if (_mouseHookHandle != IntPtr.Zero)
                {
                    return;
                }

                if (ThisProcessId == 0)
                {
                    NativeMethods.GetWindowThreadProcessId(new HandleRef(_control, _control.Handle), out ThisProcessId);
                }

                NativeMethods.HookProc hook = new MouseHookObject(this).Callback;
                _mouseHookRoot = GCHandle.Alloc(hook);

                _mouseHookHandle = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_MOUSE,
                    hook,
                    NativeMethods.NullHandleRef,
                    NativeMethods.GetCurrentThreadId());
                Debug.Assert(_mouseHookHandle != IntPtr.Zero, "Failed to install mouse hook");
            }
        }

        /// <devdoc>
        ///     HookProc used for catch mouse messages.
        /// </devdoc>
        /// <internalonly />
        private IntPtr MouseHookProc(int nCode, IntPtr wparam, IntPtr lparam)
        {
            if (nCode == NativeMethods.HC_ACTION)
            {
                NativeMethods.MouseHookStruct mhs = (NativeMethods.MouseHookStruct)Marshal.PtrToStructure(lparam, typeof(NativeMethods.MouseHookStruct));
                if (mhs != null)
                {
                    switch ((int)wparam)
                    {
                        case NativeMethods.WM_LBUTTONDOWN:
                        case NativeMethods.WM_MBUTTONDOWN:
                        case NativeMethods.WM_RBUTTONDOWN:
                        case NativeMethods.WM_NCLBUTTONDOWN:
                        case NativeMethods.WM_NCMBUTTONDOWN:
                        case NativeMethods.WM_NCRBUTTONDOWN:
                        case NativeMethods.WM_MOUSEACTIVATE:
                            if (ProcessMouseDown(mhs.handle))
                            {
                                return (IntPtr)1;
                            }
                            break;
                    }
                }
            }

            return NativeMethods.CallNextHookEx(new HandleRef(this, _mouseHookHandle), nCode, wparam, lparam);
        }

        /// <devdoc>
        ///     Removes the windowshook that was installed.
        /// </devdoc>
        /// <internalonly />
        private void UnhookMouse()
        {
            lock (this)
            {
                if (_mouseHookHandle != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(new HandleRef(this, _mouseHookHandle));
                    _mouseHookRoot.Free();
                    _mouseHookHandle = IntPtr.Zero;
                }
            }
        }

        private static MouseButtons GetAsyncMouseState()
        {
            MouseButtons buttons = 0;

            // SECURITYNOTE : only let state of MouseButtons out...
            //
            if (NativeMethods.GetKeyState((int)Keys.LButton) < 0)
            {
                buttons |= MouseButtons.Left;
            }
            if (NativeMethods.GetKeyState((int)Keys.RButton) < 0)
            {
                buttons |= MouseButtons.Right;
            }
            if (NativeMethods.GetKeyState((int)Keys.MButton) < 0)
            {
                buttons |= MouseButtons.Middle;
            }
            if (NativeMethods.GetKeyState((int)Keys.XButton1) < 0)
            {
                buttons |= MouseButtons.XButton1;
            }
            if (NativeMethods.GetKeyState((int)Keys.XButton2) < 0)
            {
                buttons |= MouseButtons.XButton2;
            }
            return buttons;
        }

        //
        // Here is where we force validation on any clicks outside the control
        //
        private bool ProcessMouseDown(IntPtr hWnd)
        {
            // com+ 12678
            // if we put up the "invalid" message box, it appears this 
            // method is getting called re-entrantly when it shouldn't be.
            // this prevents us from recursing.
            //
            if (_processing)
            {
                return false;
            }

            var hWndAtPoint = hWnd;
            var handle = _control.Handle;
            Control ctrlAtPoint = Control.FromHandle(hWndAtPoint);

            // if it's us or one of our children, just process as normal
            //
            if (hWndAtPoint != handle
                && !_control.Contains(ctrlAtPoint)
                && !TypeEditorHost.HandleContains(_control.Handle, handle))
            {
                Debug.Assert(ThisProcessId != 0, "Didn't get our process id!");

                // make sure the window is in our process
                var hr = NativeMethods.GetWindowThreadProcessId(new HandleRef(null, hWndAtPoint), out int pid);

                // if this isn't our process, unhook the mouse.
                if (!NativeMethods.Succeeded(hr) || pid != ThisProcessId)
                {
                    HookMouseDown = false;
                    return false;
                }

                // if this a sibling control (e.g. the drop down or buttons), just forward the message and skip the commit
                var needCommit = ctrlAtPoint == null || !IsSiblingControl(_control, ctrlAtPoint);
                try
                {
                    _processing = true;
                    if (needCommit)
                    {
                        if (_client.OnClickHooked())
                        {
                            return true; // there was an error, so eat the mouse
                        }
                        else
                        {
                            // Returning false lets the message go to its destination.  Only
                            // return false if there is still a mouse button down.  That might not be the
                            // case if committing the entry opened a modal dialog.
                            var state = GetAsyncMouseState();

                            return (int)state == 0;
                        }
                    }
                }
                finally
                {
                    _processing = false;
                }

                // cancel our hook at this point
                HookMouseDown = false;
            }
            return false;
        }

        private static bool IsSiblingControl(Control c1, Control c2)
        {
            var parent1 = c1.Parent;
            var parent2 = c2.Parent;

            while (parent2 != null)
            {
                if (parent1 == parent2)
                {
                    return true;
                }

                parent2 = parent2.Parent;
            }
            return false;
        }

        /// <devdoc>
        ///     Forwards messageHook calls to ToolTip.messageHookProc
        /// </devdoc>
        /// <internalonly />
        private class MouseHookObject
        {
            internal readonly WeakReference reference;

            public MouseHookObject(MouseHooker parent)
            {
                reference = new WeakReference(parent, false);
            }

            public virtual IntPtr Callback(int nCode, IntPtr wparam, IntPtr lparam)
            {
                var ret = IntPtr.Zero;
                // try 
                // {
                MouseHooker control = (MouseHooker)reference.Target;
                if (control != null)
                {
                    ret = control.MouseHookProc(nCode, wparam, lparam);
                }
                // }
                // catch (Exception) 
                // {
                // ignore
                // }
                return ret;
            }
        }
    }

}
