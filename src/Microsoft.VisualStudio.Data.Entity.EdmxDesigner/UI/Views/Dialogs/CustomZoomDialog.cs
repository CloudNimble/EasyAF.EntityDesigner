// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Windows.Forms;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.Dialogs
{
    internal partial class CustomZoomDialog : Form
    {
        public int ZoomPercent
        {
            get { return (int)numericUpDownZoom.Value; }

            set { numericUpDownZoom.Value = value; }
        }

        public CustomZoomDialog()
        {
            InitializeComponent();

            // Set the default font to VS shell font.
            var vsFont = VSHelpers.GetVSFont(PackageManager.Package);
            if (vsFont != null)
            {
                Font = vsFont;
            }
        }
    }
}
