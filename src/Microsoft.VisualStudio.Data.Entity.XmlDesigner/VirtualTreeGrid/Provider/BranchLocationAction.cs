// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Enumeration for values return by IBranch.LocateObject called with the
    ///     ObjectStyle.ExpandedBranch style.
    /// </summary>
    internal enum BranchLocationAction
    {
        /// <summary>
        ///     Discard the branch
        /// </summary>
        DiscardBranch = 0,

        /// <summary>
        ///     Keep the branch. The return values indicates the new index
        /// </summary>
        KeepBranch = 1,

        /// <summary>
        ///     Used during an insertLevels > 0 ITree.ShiftBranchLevels call to attach an existing expansion
        ///     at a level other than the insertLevels depth. Same as KeepBranch otherwise.
        /// </summary>
        KeepBranchAtThisLevel = 2,

        /// <summary>
        ///     Discard the current object, and retrieve a new branch using the returned index.
        /// </summary>
        RetrieveNewBranch = 3,
    }

}
