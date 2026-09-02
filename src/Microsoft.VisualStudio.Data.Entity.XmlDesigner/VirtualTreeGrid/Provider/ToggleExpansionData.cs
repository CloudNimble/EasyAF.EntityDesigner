// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Data returned by the ITree.ToggleExpansion method
    /// </summary>
    internal struct ToggleExpansionData
    {
        private readonly int myChange;
        private readonly bool myAllowRecursion;

        internal ToggleExpansionData(int change, bool allowRecursion)
        {
            myChange = change;
            myAllowRecursion = allowRecursion;
        }

        /// <summary>
        ///     The change in the number of items in the tree. A positive change is
        ///     an expansion, negative values occur if the item is collapsed.
        /// </summary>
        public int Change
        {
            get { return myChange; }
        }

        /// <summary>
        ///     Whether this item supports recursive expansion
        /// </summary>
        public bool AllowRecursion
        {
            get { return myAllowRecursion; }
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is ToggleExpansionData)
            {
                return Compare(this, (ToggleExpansionData)obj);
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
        public static bool operator ==(ToggleExpansionData operand1, ToggleExpansionData operand2)
        {
            return Compare(operand1, operand2);
        }

        /// <summary>
        ///     Compare two ToggleExpansionData structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>true if operands are equal</returns>
        public static bool Compare(ToggleExpansionData operand1, ToggleExpansionData operand2)
        {
            return operand1.myChange == operand2.myChange && operand1.myAllowRecursion == operand2.myAllowRecursion;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator !=(ToggleExpansionData operand1, ToggleExpansionData operand2)
        {
            return !Compare(operand1, operand2);
        }

        #endregion // Equals override and related functions
    }

}
