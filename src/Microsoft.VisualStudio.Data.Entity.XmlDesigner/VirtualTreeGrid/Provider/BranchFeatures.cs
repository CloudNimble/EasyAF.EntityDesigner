// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Features support by a branch.
    /// </summary>
    [Flags]
    internal enum BranchFeatures
    {
        /// <summary>
        ///     Branch is used to display a non-expandable static branch
        /// </summary>
        None = 0,

        /// <summary>
        ///     IBranch.IsExpandable and IBranch.GetObject(ObjectStyle.ExpandedBranch) will
        ///     be called for each item.
        /// </summary>
        Expansions = 0x0001,

        /// <summary>
        ///     Support LocateObject(ObjectStyle.ExpandedBranch). If this is not set, then
        ///     a Realign modification will close all child branches.
        /// </summary>
        BranchRelocation = 0x0002,

        /// <summary>
        ///     Support insertion and deletion. If these are not set, then at any attempt
        ///     to insert or delete items in the branch will fail
        /// </summary>
        InsertsAndDeletes = 0x0004,

        /// <summary>
        ///     UpdateCounter should be called during a global realign to determine if a branch
        ///     needs to be realigned.
        /// </summary>
        DelayedUpdates = 0x0008,

        /// <summary>
        ///     Realign may be called for this list. If this is not set, then the branch is static
        ///     and realignment of child elements is not attempted during a global realign.
        /// </summary>
        Realigns = 0x0010,

        /// <summary>
        ///     Support the IBranch.ToggleState method
        /// </summary>
        StateChanges = 0x0020,

        /// <summary>
        ///     Support selection tracking. GetObject(ObjectStyle.TrackableObject) can be called.
        /// </summary>
        PositionTracking = 0x040,

        /// <summary>
        ///     The indexed position of an item in the branch does not change
        /// </summary>
        DefaultPositionTracking = 0x0080,

        /// <summary>
        ///     Toss this branch and all children when the expansion is closed
        /// </summary>
        OnCollapseCloseAndDiscard = 0x0100,

        /// <summary>
        ///     Discard children when the branch is closed
        /// </summary>
        OnCollapseCloseChildren = 0x0200,

        /// <summary>
        ///     Don't do any discarding, just unexpand the node
        /// </summary>
        OnCollapseDoNothing = 0x0,

        /// <summary>
        ///     The number of columns in a multi column branch depends on the row
        /// </summary>
        JaggedColumns = 0x0400,

        /// <summary>
        ///     Label edits can be activated by explicit command
        /// </summary>
        ExplicitLabelEdits = 0x0800,

        /// <summary>
        ///     Label edits can be activated automatically after a delay when an item is selected with the mouse
        /// </summary>
        DelayedLabelEdits = 0x1000,

        /// <summary>
        ///     Label edits can be activated immediately when an item is selected with the mouse
        /// </summary>
        ImmediateMouseLabelEdits = 0x2000,

        /// <summary>
        ///     Label edits can be activated immediately when an item is selected, regardless of
        ///     the selection style (mouse, keyboard, or selection change via code). This flag
        ///     implies ImmediateMouseLabelEdits.
        /// </summary>
        ImmediateSelectionLabelEdits = 0x4000,
        // Update VirtualTree.BranchFeaturesToActivationStyleMask, VirtualTree.BranchFeaturesToActivationStyleShift when
        // these values change. The order should be kept the same.

        /// <summary>
        ///     Call IMultiColumnBranch.ColumnStyles when the branch is initially created to
        ///     look for columns with the SubItemCellStyles.Complex column style.
        /// </summary>
        ComplexColumns = 0x8000,

        /// <summary>
        ///     This flag means that the branch will never fire the DisplayDataChanged event.
        /// </summary>
        DisplayDataFixed = 0x10000, //(see bug 58253)

        // Update VirtualTreeConstant.FirstUserBranchFeature if values are added/removed here
    };

}
