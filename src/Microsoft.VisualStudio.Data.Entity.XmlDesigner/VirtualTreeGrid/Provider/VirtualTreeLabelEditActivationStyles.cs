// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Activation options for label editing. Combinations are used
    ///     to enable different support levels, and the current activation
    ///     style is sent to IBranch.BeginLabelEdit.
    /// </summary>
    [Flags]
    internal enum VirtualTreeLabelEditActivationStyles
    {
        /// <summary>
        ///     Label editing is not supported
        /// </summary>
        None = 0,

        /// <summary>
        ///     Label editing occurs in response to explicit user commands
        /// </summary>
        Explicit = 1,

        /// <summary>
        ///     Label editing occurs automatically in response to a timer firing after a mouse activation
        /// </summary>
        Delayed = 2,

        /// <summary>
        ///     Label editing occurs immediately when the item is activated with the mouse.
        /// </summary>
        ImmediateMouse = 4,

        /// <summary>
        ///     Label editing occurs immediately when the item is selected. Implies support for ImmediateMouse.
        ///     Specify both BranchFeatures.ImmediateSelectionLabelEdits and BranchFeatures.ImmediateMouseLabelEdits to get
        ///     an ImmediateMouse activation style for mouse-triggered selection, or just ImmediateSelection if the distinction
        ///     is irrelevant to your IBranch.BeginLabelEdit implementation.
        /// </summary>
        ImmediateSelection = 8,
        // Note that changes here need to be reflected in VirtualTreeControl.VTCStyleFlags and BranchFeatures
    }

}
