// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     The result of an ILevelShiftAdjuster.ValidateAdjustDepth call
    /// </summary>
    internal struct ValidateAdjustDepthResult
    {
        private readonly bool myContinue;
        private readonly int myDepthAdjustment;

        /// <summary>
        ///     Continue processing this branch without any depth adjustment
        /// </summary>
        public static readonly ValidateAdjustDepthResult ContinueProcessing = new ValidateAdjustDepthResult(true);

        /// <summary>
        ///     Stop processing this branch
        /// </summary>
        public static readonly ValidateAdjustDepthResult StopProcessing = new ValidateAdjustDepthResult(false);

        /// <summary>
        ///     Continue processing the branch but adjust the depth
        /// </summary>
        /// <param name="depthAdjustment">The number of levels to shift the branch</param>
        /// <returns>A new result structure</returns>
        public static ValidateAdjustDepthResult AdjustDepth(int depthAdjustment)
        {
            return new ValidateAdjustDepthResult(depthAdjustment);
        }

        private ValidateAdjustDepthResult(bool continueProcessing)
        {
            myContinue = continueProcessing;
            myDepthAdjustment = 0;
        }

        private ValidateAdjustDepthResult(int depthAdjustment)
        {
            myContinue = true;
            myDepthAdjustment = depthAdjustment;
        }

        /// <summary>
        ///     Continue processing the branch if true
        /// </summary>
        public bool Continue
        {
            get { return myContinue; }
        }

        /// <summary>
        ///     Adjust the branch depth by this value
        /// </summary>
        public int DepthAdjustment
        {
            get { return myDepthAdjustment; }
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is ValidateAdjustDepthResult)
            {
                return Compare(this, (ValidateAdjustDepthResult)obj);
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
        public static bool operator ==(ValidateAdjustDepthResult operand1, ValidateAdjustDepthResult operand2)
        {
            return Compare(operand1, operand2);
        }

        /// <summary>
        ///     Compare two ValidateAdjustDepthResult structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>true if operands are equal</returns>
        public static bool Compare(ValidateAdjustDepthResult operand1, ValidateAdjustDepthResult operand2)
        {
            return operand1.myContinue == operand2.myContinue && operand1.myDepthAdjustment == operand2.myDepthAdjustment;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator !=(ValidateAdjustDepthResult operand1, ValidateAdjustDepthResult operand2)
        {
            return !Compare(operand1, operand2);
        }

        #endregion // Equals override and related functions
    }

}
