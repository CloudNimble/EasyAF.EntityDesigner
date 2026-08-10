// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    /// <summary>
    /// Displays a dialog that allows us to display debug information in modal textbox in debug builds
    /// </summary>
    internal partial class DebugViewerDialog : DialogWindow
    {
        internal enum ButtonMode
        {
            OkCancel,
            YesNo
        }

        private readonly Action<DebugViewerDialog> _onOkClick;

        internal string Message => MessageTextBox.Text;

        internal DebugViewerDialog(string formattedTitle, string formattedMessage, Action<DebugViewerDialog> onOkClick = null)
        {
            InitializeComponent();
            this.HasHelpButton = false;

            Title = formattedTitle;
            MessageTextBox.Text = formattedMessage;
            _onOkClick = onOkClick;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_onOkClick == null)
            {
                DialogResult = true;
            }
            else
            {
                _onOkClick(this);
            }
        }
    }
}
