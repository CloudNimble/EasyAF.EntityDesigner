// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     A structure specifying the types of information to return from IBranch.GetDisplayData
    /// </summary>
    internal struct VirtualTreeDisplayDataMasks
    {
        private VirtualTreeDisplayMasks myMask;
        private VirtualTreeDisplayStates myStateMask;

        /// <summary>
        ///     Create a new structure with the given display and state masks
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="stateMask"></param>
        public VirtualTreeDisplayDataMasks(VirtualTreeDisplayMasks mask, VirtualTreeDisplayStates stateMask)
        {
            myMask = mask;
            myStateMask = stateMask;
        }

        /// <summary>
        ///     The fields of the VirtualTreeDisplayData to populate
        /// </summary>
        public VirtualTreeDisplayMasks Mask
        {
            get { return myMask; }
            set { myMask = value; }
        }

        /// <summary>
        ///     The state settings to populate. Refines the VirtualTreeDisplayMasks.State setting.
        /// </summary>
        public VirtualTreeDisplayStates StateMask
        {
            get { return myStateMask; }
            set { myStateMask = value; }
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is VirtualTreeDisplayDataMasks)
            {
                return Compare(this, (VirtualTreeDisplayDataMasks)obj);
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
        public static bool operator ==(VirtualTreeDisplayDataMasks operand1, VirtualTreeDisplayDataMasks operand2)
        {
            return Compare(operand1, operand2);
        }

        /// <summary>
        ///     Compare two VirtualTreeDisplayDataMasks structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>true if operands are equal</returns>
        public static bool Compare(VirtualTreeDisplayDataMasks operand1, VirtualTreeDisplayDataMasks operand2)
        {
            return operand1.myMask == operand2.myMask && operand1.myStateMask == operand2.myStateMask;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator !=(VirtualTreeDisplayDataMasks operand1, VirtualTreeDisplayDataMasks operand2)
        {
            return !Compare(operand1, operand2);
        }

        #endregion // Equals override and related functions
    }

}
