// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     An enumeration of actions that can be taken indirectly by a branch
    ///     against all trees (and other listeners) containing that branch
    /// </summary>
    internal enum BranchModificationAction
    {
        /// <summary>
        ///     Call the ITree.DisplayDataChanged method
        /// </summary>
        DisplayDataChanged,

        /// <summary>
        ///     Call the ITree.Realign method
        /// </summary>
        Realign,

        /// <summary>
        ///     Call the ITree.InsertItems method
        /// </summary>
        InsertItems,

        /// <summary>
        ///     Call the ITree.DeleteItems method
        /// </summary>
        DeleteItems,

        /// <summary>
        ///     Call the ITree.MoveItems method
        /// </summary>
        MoveItem,

        /// <summary>
        ///     Call the ITree.ShiftBranchLevels method
        /// </summary>
        ShiftBranchLevels,

        /// <summary>
        ///     Set the ITree.Redraw property
        /// </summary>
        Redraw,

        /// <summary>
        ///     Set the ITree.DelayRedraw property
        /// </summary>
        DelayRedraw,

        /// <summary>
        ///     Set the ITree.ListShuffle property
        /// </summary>
        ListShuffle,

        /// <summary>
        ///     Set the ITree.DelayListShuffle property
        /// </summary>
        DelayListShuffle,

        /// <summary>
        ///     Call the IMultiColumnTree.UpdateCellStyle method
        /// </summary>
        UpdateCellStyle,

        /// <summary>
        ///     Call the ITree.RemoveBranch method
        /// </summary>
        RemoveBranch,
    }

}
