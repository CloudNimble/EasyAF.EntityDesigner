// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Display state flags. Items &lt;=0xF000 correspond to TVIS_* flags in the system SDK
    /// </summary>
    [Flags]
    internal enum VirtualTreeDisplayStates
    {
        /// <summary>
        ///     The item is selected. This is set only in the VirtualTreeDisplayDataMasks.Mask property
        ///     sent while an item is being drawn. Setting it in the VirtualTreeDisplayData.State property
        ///     has no effect. This state should not be used to determine if an icon or state icon is
        ///     visible or to set the Bold state because it is not sent during IBranch.GetDisplayData
        ///     calls that occur while determining item width, which could lead to inconsistencies between
        ///     the display and hit testing (ToolTip interaction, VirtualTreeControl.HitInfo, etc).
        /// </summary>
        Selected = 0x000002,

        /// <summary>
        ///     The item is cut (NYI)
        /// </summary>
        Cut = 0x000004,

        /// <summary>
        ///     The item is drawn with a drop highlight (NYI)
        /// </summary>
        DropHighlighted = 0x000008,

        /// <summary>
        ///     Display the item bold
        /// </summary>
        Bold = 0x000010,

        /// <summary>
        ///     The force selection fields are set
        /// </summary>
        ForceSelect = 0x000020,

        /// <summary>
        ///     Display the item gray. Can be set indirectly with the
        /// </summary>
        GrayText = 0x000040,

        /// <summary>
        ///     The item is expanded. This is set only in the VirtualTreeDisplayDataMasks.Mask property
        ///     sent while an item is being drawn. Setting it in the VirtualTreeDisplayData.State property
        ///     has no effect. This state should not be used to determine if an icon or state icon is
        ///     visible or to set the Bold state because it is not sent during IBranch.GetDisplayData
        ///     calls that occur while determining item width, which could lead to inconsistencies between
        ///     the display and hit testing (ToolTip interaction, VirtualTreeControl.HitInfo, etc).
        /// </summary>
        Expanded = 0x000080,

        /// <summary>
        ///     Text aligned opposite (far) from the glyph.  This will cause text to be aligned to the right
        ///     if the RightToLeft property on the TreeControl is false, or to the left if RightToLeft is true.
        /// </summary>
        TextAlignFar = 0x000100,
        // If the ReverseTree property is implemented, we would want the following flag, to allow control
        // over text align behavior when this property was set.
        //
        // TextAlignIgnoreReverseTree = 0x000200
        //TextTypeMask    = 0xF00000,
    }

}
