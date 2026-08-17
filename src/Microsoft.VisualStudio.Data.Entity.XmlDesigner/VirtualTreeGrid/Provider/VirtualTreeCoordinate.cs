// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Structure representing the global position in a tree.
    /// </summary>
    internal struct VirtualTreeCoordinate
    {
        private int myRow;
        private int myColumn;

        /// <summary>
        ///     Create a new coordinate
        /// </summary>
        /// <param name="row">Coordinate row</param>
        /// <param name="column">Coordinate column</param>
        public VirtualTreeCoordinate(int row, int column)
        {
            myRow = row;
            myColumn = column;
        }

        /// <summary>
        ///     A value representing an invalid coordinate
        /// </summary>
        public static readonly VirtualTreeCoordinate Invalid = new VirtualTreeCoordinate(VirtualTreeConstant.NullIndex, 0);

        /// <summary>
        ///     Test if this is a valid coordinate
        /// </summary>
        /// <value>true if structure does not represent a valid coordinate</value>
        public bool IsValid
        {
            get { return myRow != VirtualTreeConstant.NullIndex; }
        }

        /// <summary>
        ///     The coordinate row
        /// </summary>
        /// <value>Nonnegative value for a valid coordinate</value>
        public int Row
        {
            get { return myRow; }
            set { myRow = value; }
        }

        /// <summary>
        ///     The coordinate column
        /// </summary>
        /// <value>Nonnegative value</value>
        public int Column
        {
            get { return myColumn; }
            set { myColumn = value; }
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is LocateObjectData)
            {
                return Compare(this, (VirtualTreeCoordinate)obj);
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
        public static bool operator ==(VirtualTreeCoordinate operand1, VirtualTreeCoordinate operand2)
        {
            return Compare(operand1, operand2);
        }

        /// <summary>
        ///     Compare two VirtualTreeCoordinate structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>true if operands are equal</returns>
        public static bool Compare(VirtualTreeCoordinate operand1, VirtualTreeCoordinate operand2)
        {
            return operand1.myColumn == operand2.myColumn && operand1.myRow == operand2.myRow;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns></returns>
        public static bool operator !=(VirtualTreeCoordinate operand1, VirtualTreeCoordinate operand2)
        {
            return !Compare(operand1, operand2);
        }

        #endregion // Equals override and related functions
    }

}
