// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Options used in an IBranch.GetObject call for ObjectStyle.ExpandedBranch,
    ///     ObjectStyle.SubitemRootBranch, and ObjectStyle.SubitemBranch objects.
    /// </summary>
    [Flags]
    internal enum ExpansionOptions
    {
        /// <summary>
        ///     A user request for recursive expansion (* on the number keypad)
        ///     will expand one level instead of recursing. This must be set
        ///     for circular branch structures to be safe.
        /// </summary>
        BlockRecursion = 1,

        /// <summary>
        ///     If a tree object is returned in response to an ExpandedBranch call,
        ///     then incorporate all of the data from the tree. If this flag is not
        ///     set, then the returned tree can continue to be used.
        /// </summary>
        ConsumeTree = 2,

        /// <summary>
        ///     Set to turn the branch returned by an ObjectStyle.SubItemRootBranch
        ///     request into a ObjectStyle.SubItemExpansion request. This flag enables
        ///     branches returned during the initial load of a SubItemCellStyles.Mixed
        ///     column to result in subitem expansion instead of a root branch.
        /// </summary>
        UseAsSubItemExpansion = 4,
    }

}
