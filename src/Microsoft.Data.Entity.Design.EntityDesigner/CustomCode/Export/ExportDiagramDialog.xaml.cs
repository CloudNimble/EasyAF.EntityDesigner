// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.PlatformUI;
    using EntityDesignerResources = Microsoft.Data.Entity.Design.EntityDesigner.Properties.Resources;

    /// <summary>
    /// WPF dialog for exporting diagrams as images with additional options.
    /// </summary>
    internal partial class ExportDiagramDialog : DialogWindow
    {
        private readonly string _modelName;
        private readonly bool _diagramShowsTypes;

        /// <summary>
        /// Gets the full path to the selected file.
        /// </summary>
        public string FileName
        {
            get
            {
                var extension = GetSelectedExtension();
                var fileName = _fileNameTextBox.Text.Trim();
                if (!fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    fileName += extension;
                }
                return Path.Combine(_directoryTextBox.Text.Trim(), fileName);
            }
        }

        /// <summary>
        /// Gets whether transparent background is selected.
        /// </summary>
        public bool TransparentBackground => _transparentBackgroundCheckBox.IsChecked == true;

        /// <summary>
        /// Gets whether to show data types in the export.
        /// </summary>
        public bool ShowTypes => _showTypesCheckBox.IsChecked == true;

        /// <summary>
        /// Gets the selected file extension (e.g., ".svg", ".png").
        /// </summary>
        public string SelectedExtension => GetSelectedExtension();

        /// <summary>
        /// Gets whether the diagram currently shows types (Scalar Property Format).
        /// </summary>
        public bool DiagramShowsTypes => _diagramShowsTypes;

        /// <summary>
        /// Creates a DiagramExportOptions object based on the current dialog settings.
        /// </summary>
        public DiagramExportOptions CreateExportOptions()
        {
            return new DiagramExportOptions
            {
                FilePath = FileName,
                Format = ExportManager.GetFormatFromExtension(SelectedExtension),
                TransparentBackground = TransparentBackground,
                ShowTypes = ShowTypes,
                DiagramShowsTypes = DiagramShowsTypes
            };
        }

        /// <summary>
        /// Creates a new ExportDiagramDialog.
        /// </summary>
        /// <param name="modelName">The name of the model being exported.</param>
        /// <param name="diagramShowsTypes">Whether the diagram currently shows types (Scalar Property Format).</param>
        public ExportDiagramDialog(string modelName, bool diagramShowsTypes)
        {
            _modelName = modelName ?? "EntityModel";
            _diagramShowsTypes = diagramShowsTypes;

            InitializeComponent();
            this.HasHelpButton = false;

            PopulateFormats();
            SetDefaults();
        }

        private void PopulateFormats()
        {
            // Add formats in alphabetical order
            _formatComboBox.Items.Add(new FormatItem("BMP", ".bmp", EntityDesignerResources.ImageFormatBmp));
            _formatComboBox.Items.Add(new FormatItem("GIF", ".gif", EntityDesignerResources.ImageFormatGif));
            _formatComboBox.Items.Add(new FormatItem("JPEG", ".jpg", EntityDesignerResources.ImageFormatJpeg));
            _formatComboBox.Items.Add(new FormatItem("Mermaid", ".mmd", EntityDesignerResources.ImageFormatMermaid));
            _formatComboBox.Items.Add(new FormatItem("PNG", ".png", EntityDesignerResources.ImageFormatPng));
            _formatComboBox.Items.Add(new FormatItem("SVG", ".svg", EntityDesignerResources.ImageFormatSvg));
            _formatComboBox.Items.Add(new FormatItem("TIFF", ".tif", EntityDesignerResources.ImageFormatTiff));
        }

        private void SetDefaults()
        {
            // Default directory to My Documents
            _directoryTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Default filename: modelname-YYYY-MM-DD
            var dateStr = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            _fileNameTextBox.Text = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", _modelName, dateStr);

            // Default to SVG (index 5 in alphabetical order: BMP, GIF, JPEG, Mermaid, PNG, SVG, TIFF)
            _formatComboBox.SelectedIndex = 5; // SVG

            // Default to transparent background for maximum flexibility
            _transparentBackgroundCheckBox.IsChecked = true;

            // Default show types to diagram's current setting
            _showTypesCheckBox.IsChecked = _diagramShowsTypes;

            // Update transparency checkbox state based on selected format
            UpdateTransparencyCheckboxState();
        }

        private string GetSelectedExtension()
        {
            if (_formatComboBox.SelectedItem is FormatItem item)
            {
                return item.Extension;
            }
            return ".svg";
        }

        /// <summary>
        /// Determines whether the given file extension supports transparency.
        /// </summary>
        /// <param name="extension">The file extension (e.g., ".png").</param>
        /// <returns>True if the format supports transparency; otherwise, false.</returns>
        private static bool SupportsTransparency(string extension)
        {
            switch (extension?.ToLowerInvariant())
            {
                case ".png":
                case ".svg":
                case ".gif":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handles the format ComboBox selection change event.
        /// </summary>
        private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTransparencyCheckboxState();
        }

        /// <summary>
        /// Updates the enabled state of the transparent background checkbox
        /// based on whether the selected format supports transparency.
        /// </summary>
        private void UpdateTransparencyCheckboxState()
        {
            if (_transparentBackgroundCheckBox == null) return;

            var supportsTransparency = SupportsTransparency(GetSelectedExtension());
            _transparentBackgroundCheckBox.IsEnabled = supportsTransparency;

            // Uncheck when disabled to avoid user confusion
            if (!supportsTransparency)
            {
                _transparentBackgroundCheckBox.IsChecked = false;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = ShowFolderPicker(_directoryTextBox.Text);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _directoryTextBox.Text = selectedPath;
            }
        }

        /// <summary>
        /// Shows the modern Windows Shell folder picker dialog.
        /// Falls back to FolderBrowserDialog on failure.
        /// </summary>
        private string ShowFolderPicker(string initialPath)
        {
            try
            {
                var dialog = (IFileOpenDialog)new FileOpenDialog();
                try
                {
                    // Configure for folder picking
                    dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_NOCHANGEDIR);
                    dialog.SetTitle(EntityDesignerResources.ExportImage_SelectLocation);

                    // Set initial folder if valid
                    if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                    {
                        IShellItem initialFolder;
                        if (SHCreateItemFromParsingName(initialPath, IntPtr.Zero, typeof(IShellItem).GUID, out initialFolder) == 0)
                        {
                            dialog.SetFolder(initialFolder);
                        }
                    }

                    // Get the window handle for the WPF dialog
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

                    // Show dialog
                    var hr = dialog.Show(hwnd);
                    if (hr == 0) // S_OK
                    {
                        IShellItem result;
                        dialog.GetResult(out result);
                        if (result != null)
                        {
                            string path;
                            result.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out path);
                            return path;
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(dialog);
                }
            }
            catch
            {
                // Fall back to basic folder browser dialog on any COM failure
                using (var fallbackDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    fallbackDialog.Description = EntityDesignerResources.ExportImage_SelectLocation;
                    fallbackDialog.SelectedPath = initialPath;
                    fallbackDialog.ShowNewFolderButton = true;

                    // Get the window handle for the WPF dialog
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    var win32Window = new Win32Window(hwnd);

                    if (fallbackDialog.ShowDialog(win32Window) == System.Windows.Forms.DialogResult.OK)
                    {
                        return fallbackDialog.SelectedPath;
                    }
                }
            }

            return null;
        }

        #region Windows Shell COM Interop

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IShellItem ppv);

        [ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }

        [ComImport, Guid("D57C7288-D4AD-4768-BE02-9D969532D960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr hwndOwner);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid([MarshalAs(UnmanagedType.LPStruct)] Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [Flags]
        private enum FOS : uint
        {
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040,
            FOS_NOCHANGEDIR = 0x00000008,
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000,
        }

        /// <summary>
        /// Helper class to wrap an IntPtr as IWin32Window for WinForms dialogs.
        /// </summary>
        private class Win32Window : System.Windows.Forms.IWin32Window
        {
            public IntPtr Handle { get; }

            public Win32Window(IntPtr handle)
            {
                Handle = handle;
            }
        }

        #endregion

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(_directoryTextBox.Text))
            {
                MessageBox.Show(this, EntityDesignerResources.ExportImage_SelectLocationError, Title,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(_directoryTextBox.Text))
            {
                MessageBox.Show(this, EntityDesignerResources.ExportImage_LocationNotExistError, Title,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_fileNameTextBox.Text))
            {
                MessageBox.Show(this, EntityDesignerResources.ExportImage_EnterFileNameError, Title,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for invalid filename characters
            var fileName = _fileNameTextBox.Text.Trim();
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show(this, EntityDesignerResources.ExportImage_InvalidFileNameError, Title,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if file exists
            var fullPath = FileName;
            if (File.Exists(fullPath))
            {
                var result = MessageBox.Show(
                    this,
                    string.Format(CultureInfo.CurrentCulture, EntityDesignerResources.MsgBox_ConfirmFileOverwrite, fullPath)
                        .Replace(@"\n", "\n"),
                    EntityDesignerResources.MsgBox_Title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // All validation passed - close dialog with OK
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        /// <summary>
        /// Helper class to store format information in ComboBox.
        /// </summary>
        private sealed class FormatItem
        {
            public string Name { get; }
            public string Extension { get; }
            public string Description { get; }

            public FormatItem(string name, string extension, string description)
            {
                Name = name;
                Extension = extension;
                Description = description;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
