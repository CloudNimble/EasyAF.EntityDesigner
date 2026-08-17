// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Data.Entity.Design.Base.Shell
{
    internal interface ITreeGridDesignerToolWindowContainer : IDisposable
    {
        bool WatermarkVisible { get; set; }
        AccessibleObject AccessibilityObject { get; }
        IWin32Window Window { get; }
        Font Font { get; set; }
        IntPtr Handle { get; }
        object HostContext { get; set; }
        void SetWatermarkInfo(TreeGridDesignerWatermarkInfo watermarkInfo);
        void SetWatermarkThemedColors();
        void SetToolbarThemedColors();
    }
}
