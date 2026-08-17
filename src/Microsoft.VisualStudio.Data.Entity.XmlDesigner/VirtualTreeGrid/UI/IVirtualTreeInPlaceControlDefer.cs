// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Windows.Forms;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     The portion of the IInPlaceControl interface that is
    ///     implemented on the VirtualTreeControl.InPlaceControlHelper class.
    /// </summary>
    internal interface IVirtualTreeInPlaceControlDefer
    {
        /// <summary>
        ///     The tree control that activates this object.
        /// </summary>
        VirtualTreeControl Parent { get; set; }

        /// <summary>
        ///     The control that implements this interface
        /// </summary>
        Control InPlaceControl { get; }

        /// <summary>
        ///     Flags governing control appearance/behavior
        /// </summary>
        VirtualTreeInPlaceControls Flags { get; set; }

        /// <summary>
        ///     The windows message (WM_LBUTTONDOWN) that launched the edit
        ///     control. Use 0 for for a timer, selection change, or explicit
        ///     launch. Used to support mouse behavior while a control is in-place
        ///     activating in response to a mouse click.
        /// </summary>
        int LaunchedByMessage { get; set; }

        /// <summary>
        ///     Value indicates whether the in-place edit control is currently dirty.
        ///     At commit time, only a dirty in-place edit control generates calls
        ///     to IBranch.CommitLabelEdit or a custom commit delegate.
        /// </summary>
        bool Dirty { get; set; }
    }

}
