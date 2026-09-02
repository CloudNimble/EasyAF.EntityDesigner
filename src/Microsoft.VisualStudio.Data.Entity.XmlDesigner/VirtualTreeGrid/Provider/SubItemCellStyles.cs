// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Specifies the style of cells in subitem columns of
    ///     a multicolumn branch. The cell style is retrieved
    ///     when the branch is first loaded and cannot be changed.
    /// </summary>
    [Flags]
    internal enum SubItemCellStyles
    {
        /// <summary>
        ///     Subitem cells are single-valued and non-expandable.
        /// </summary>
        Simple = 1,

        /// <summary>
        ///     Subitem cells can be the parent node of an expansion, but
        ///     cannot have siblings. The data for the parent node is provided
        ///     by the IBranch implementation for the row. The IsExpandable method
        ///     is used to determine if a given cell is expandable.
        /// </summary>
        Expandable = 2,

        /// <summary>
        ///     A subitem can be comprised of multiple top level cells defined
        ///     by a separate branch. The IBranch functions and settings for a given row
        ///     do not affect these nodes. The subitem branches are requested when
        ///     the tree is first loaded. The cell reverts to simple state if a subitem
        ///     root branch is not available. UNDONE: Tree methods to toggle between
        ///     simple/expandable state and complex state.
        /// </summary>
        Complex = 4,

        /// <summary>
        ///     Cells can be either complex or expandable. A subitem root list will
        ///     be requested for each row in the branch with an ObjectStyle.SubItemRootBranch
        ///     call to IBranch.GetObject. To keep the subitem collapsed,
        ///     simply return null from this request. To preexpand an expandable subitem,
        ///     set the ExpansionOptions.UseAsSubItemExpansion flag.
        /// </summary>
        Mixed = Expandable | Complex,
    }

}
