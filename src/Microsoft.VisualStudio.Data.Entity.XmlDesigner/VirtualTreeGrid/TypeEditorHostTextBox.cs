// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid
{

    /// <summary>
    ///     Edit control displayed in the TypeEditorHost.  Just a TextBox with some addtional
    ///     key message processing for opening the drop down.
    /// </summary>
    internal class TypeEditorHostTextBox : TextBox
    {
        private readonly TypeEditorHost _dropDownParent;

        /// <summary>
        ///     Edit control displayed in the TypeEditorHost.  Just a TextBox with some addtional
        ///     key message processing for opening the drop down.
        /// </summary>
        public TypeEditorHostTextBox(TypeEditorHost dropDownParent)
        {
            _dropDownParent = dropDownParent;

            BackColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey);
            ForeColor = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxTextColorKey);
        }

        /// <summary>
        ///     Key processing.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Z: // ensure that the edit control handles undo if the text box is dirty.
                    if (((IVirtualTreeInPlaceControlDefer)_dropDownParent).Dirty
                        && ((keyData & Keys.Control) != 0)
                        && ((keyData & Keys.Shift) == 0)
                        && ((keyData & Keys.Alt) == 0))
                    {
                        Undo();
                        return true;
                    }

                    break;

                case Keys.A: // ensure that the edit control handles select all.
                    if (((keyData & Keys.Control) != 0)
                        && ((keyData & Keys.Shift) == 0)
                        && ((keyData & Keys.Alt) == 0))
                    {
                        SelectAll();
                        return true;
                    }

                    break;

                case Keys.F4: // F4 opens the drop down
                    if ((keyData & (Keys.Shift | Keys.Control | Keys.Alt)) == 0)
                    {
                        _dropDownParent.OpenDropDown();
                        return true;
                    }

                    break;

                case Keys.Down: // Alt-Down opens the drop down
                    if (((keyData & Keys.Alt) != 0)
                        && ((keyData & Keys.Control) == 0))
                    {
                        _dropDownParent.OpenDropDown();
                        return true;
                    }

                    break;

                case Keys.Delete:
                    NativeMethods.SendMessage(Handle, msg.Msg, msg.WParam, msg.LParam);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        ///     Key processing.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // for some reason when the Alt key is pressed,
            // WM_KEYDOWN messages aren't going through the 
            // TranslateAccelerator/PreProcessMessage loop.
            // So we handle Alt-Down here.
            switch (e.KeyCode)
            {
                case Keys.Down: // Alt-Down opens the drop down
                    if (((e.KeyData & Keys.Alt) != 0)
                        && ((e.KeyData & Keys.Control) == 0))
                    {
                        _dropDownParent.OpenDropDown();
                        e.Handled = true;
                        return;
                    }

                    break;
            }

            base.OnKeyDown(e);
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

                VirtualTreeControl.DisplayException(_dropDownParent.Site, e);
            }
        }
    }

}
