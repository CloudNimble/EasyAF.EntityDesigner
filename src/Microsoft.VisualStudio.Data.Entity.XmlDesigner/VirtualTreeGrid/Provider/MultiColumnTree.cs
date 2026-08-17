// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     The data object for a multi-column tree.
    /// </summary>
    internal class MultiColumnTree : VirtualTree, IMultiColumnTree
    {
        private readonly int myColumns;
        private ITree mySingleColumnTree;

        /// <summary>
        ///     Create a new multi column tree
        /// </summary>
        /// <param name="columns">The maximum number of columns supported by the tree</param>
        public MultiColumnTree(int columns)
        {
            EnableMultiColumn();
            myColumns = columns;
        }

        /// <summary>
        ///     Override to supply the number of columns
        /// </summary>
        /// <value>The total number of columns support by the multi column tree</value>
        protected override sealed int ColumnCount
        {
            get { return myColumns; }
        }

        /// <summary>
        ///     Return the single column view on the multi column tree.
        /// </summary>
        /// <value>An ITree instance representing the single-column view</value>
        protected override sealed ITree SingleColumnTree
        {
            get
            {
                mySingleColumnTree ??= CreateSingleColumnTree();
                return mySingleColumnTree;
            }
        }

        int IMultiColumnTree.ColumnCount
        {
            get { return myColumns; }
        }

        void IMultiColumnTree.UpdateCellStyle(IBranch branch, int row, int column, bool makeComplex)
        {
            // It is easier to code this in the base class, defer
            UpdateCellStyle(branch, row, column, makeComplex);
        }

        ITree IMultiColumnTree.SingleColumnTree
        {
            get { return SingleColumnTree; }
        }
    }

}
