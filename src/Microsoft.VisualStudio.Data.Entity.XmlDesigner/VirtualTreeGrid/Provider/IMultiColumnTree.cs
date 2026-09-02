// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     An interface to implement in addition to ITree to enable multi column support
    /// </summary>
    internal interface IMultiColumnTree
    {
        /// <summary>
        ///     The number of columns supported by this tree
        /// </summary>
        /// <value>A positive column count</value>
        int ColumnCount { get; }

        /// <summary>
        ///     A mechanism for changing a cell from simple or expandable
        ///     to complex, or vice versa. This enables a potentially
        ///     complex cell to begin life as a simple cell, then switch later.
        ///     The makeComplex variable is interpreted according to the
        ///     cell style settings for the given branch.
        /// </summary>
        /// <param name="branch">The branch to modify</param>
        /// <param name="row">Target row</param>
        /// <param name="column">Target column</param>
        /// <param name="makeComplex">True to switch to a complex cell, false to switch to a simple cell</param>
        void UpdateCellStyle(IBranch branch, int row, int column, bool makeComplex);

        /// <summary>
        ///     A single column view on the first column of the multi-column tree.
        ///     Allows a multi-column tree to exist simultaneously in both multi and
        ///     single column states.
        /// </summary>
        /// <value>The single-column view on the tree</value>
        ITree SingleColumnTree { get; }
    }

}
