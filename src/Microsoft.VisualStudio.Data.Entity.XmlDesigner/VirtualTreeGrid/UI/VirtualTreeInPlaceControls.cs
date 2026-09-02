// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Flags that control how the tree control interacts with an
    ///     in-place activated control.
    /// </summary>
    [Flags]
    internal enum VirtualTreeInPlaceControls
    {
        /// <summary>
        ///     Set this flag to specify that the control should be sized based
        ///     on the label text. By default, the control is sized based on the
        ///     size of the activating cell.
        /// </summary>
        SizeToText = 1,

        /// <summary>
        ///     Set this flag to specify that the VirtualTreeControl should call Dispose()
        ///     on the in place control when it is no longer in use. If it is not set, then
        ///     it is up to the branch to dispose the control. Clearing this flag allows the
        ///     branch to reuse a single control instance.
        /// </summary>
        DisposeControl = 2,

        /// <summary>
        ///     By default, the text behind an in-place activated control is not drawn when the
        ///     control is painted. This enables the control to be smaller than the text without
        ///     having strange drawing happening in the background. Setting this flag forces the
        ///     text to draw, enabling transparent controls. For example, this enables a dropdown
        ///     control to show a button without providing an edit field.
        /// </summary>
        DrawItemText = 4,

        /// <summary>
        ///     If the in-place control does not need keystrokes itself, then it can set this flag
        ///     to force the OnKeyDown and OnKeyPress callbacks in the inplace helper to forward
        ///     the keystrokes to the tree control.
        /// </summary>
        ForwardKeyEvents = 8,
    }

}
