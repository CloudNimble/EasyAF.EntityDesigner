// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Windows;
using System.Windows.Forms.Design;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    /// <summary>
    /// Displays a dialog that allows the user to choose to never see it again via a checkbox.
    /// </summary>
    internal partial class DismissableWarningDialog : DialogWindow
    {
        internal enum ButtonMode
        {
            OkCancel,
            YesNo
        }

        internal DismissableWarningDialog(string formattedTitle, string formattedMessage, ButtonMode buttonMode)
        {
            InitializeComponent();
            this.HasHelpButton = false;

            Title = formattedTitle;
            WarningLabel.Text = formattedMessage;

            // Default button mode is OkCancel
            if (buttonMode == ButtonMode.YesNo)
            {
                OkButton.Content = DialogsResource.YesButton_Text;
                CancelDialogButton.Content = DialogsResource.NoButton_Text;
            }
        }

        /// <summary>
        /// Gets whether the "don't show again" checkbox was checked.
        /// </summary>
        internal bool DontShowAgain => DontShowAgainCheckBox.IsChecked == true;

        /// <summary>
        /// Static method to instantiate a DismissableWarningDialog and persist the user setting to dismiss the dialog.
        /// Returns a boolean indicating whether the dialog was cancelled or not.
        /// </summary>
        /// <param name="formattedTitle">Dialog title</param>
        /// <param name="formattedMessage">Warning message</param>
        /// <param name="regKeyName">Registry key name for persisting the "don't show" setting</param>
        /// <param name="buttonMode">Either 'OKCancel' or 'YesNo'. If 'YesNo', 'Yes' will be associated with OK result</param>
        /// <returns>True if cancelled, false if OK/Yes was clicked</returns>
        internal static bool ShowWarningDialogAndSaveDismissOption(
            string formattedTitle, string formattedMessage, string regKeyName, ButtonMode buttonMode)
        {
            var cancelled = true;

            var service = Services.ServiceProvider.GetService(typeof(IUIService)) as IUIService;
            Debug.Assert(service != null, "service should not be null");
            if (service != null)
            {
                var dialog = new DismissableWarningDialog(formattedTitle, formattedMessage, buttonMode);
                var result = dialog.ShowModal();
                if (result == true)
                {
                    cancelled = false;
                    var showAgain = !dialog.DontShowAgain;
                    EdmUtils.SaveUserSetting(regKeyName, showAgain.ToString());
                }
            }

            return cancelled;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
