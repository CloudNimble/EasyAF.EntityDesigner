// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Data returned by the ITree.GetExpandedBranch function
    /// </summary>
    internal struct ExpandedBranchData
    {
        private readonly IBranch myBranch;
        private readonly int myLevel;

        /// <summary>
        ///     The branch anchored at the requested location
        /// </summary>
        /// <value></value>
        public IBranch Branch
        {
            get { return myBranch; }
        }

        /// <summary>
        ///     The level of the requested location
        /// </summary>
        /// <value></value>
        public int Level
        {
            get { return myLevel; }
        }

        internal ExpandedBranchData(IBranch branch, int level)
        {
            myBranch = branch;
            myLevel = level;
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is ExpandedBranchData)
            {
                return Compare(this, (ExpandedBranchData)obj);
            }
            return false;
        }

        /// <summary>
        ///     GetHashCode override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // We're forced to override this with the Equals override.
            return base.GetHashCode();
        }

        /// <summary>
        ///     Equals operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator ==(ExpandedBranchData operand1, ExpandedBranchData operand2)
        {
            return Compare(operand1, operand2);
        }

        /// <summary>
        ///     Compare two ExpandedBranchData structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>true if operands are equal</returns>
        public static bool Compare(ExpandedBranchData operand1, ExpandedBranchData operand2)
        {
            return operand1.myBranch == operand2.myBranch && operand1.myLevel == operand2.myLevel;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator !=(ExpandedBranchData operand1, ExpandedBranchData operand2)
        {
            return !Compare(operand1, operand2);
        }

        #endregion // Equals override and related functions
    }

}
