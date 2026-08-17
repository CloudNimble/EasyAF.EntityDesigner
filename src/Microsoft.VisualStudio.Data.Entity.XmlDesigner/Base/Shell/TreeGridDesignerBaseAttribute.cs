// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell
{

    /// <summary>
    ///     Base class for TreeGrid designer attributes.  Implements IComparable functionality
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal abstract class TreeGridDesignerBaseAttribute : Attribute, IComparable
    {
        private int _order;

        #region IComparable implementation

        /// <summary>
        ///     IComparable interface implementation.
        /// </summary>
        public int /* IComparable */ CompareTo(object obj)
        {
            return _order - ((TreeGridDesignerBaseAttribute)obj)._order;
        }

        /// <summary>
        ///     Determines equality based on the Order property.
        /// </summary>
        public override bool Equals(object obj)
        {
            TreeGridDesignerBaseAttribute attr = obj as TreeGridDesignerBaseAttribute;
            if (attr == null)
            {
                return false;
            }

            return _order == attr._order;
        }

        /// <summary>
        ///     Returns a unique hashcode for this object.
        /// </summary>
        public override int GetHashCode()
        {
            return _order.GetHashCode();
        }

        /// <summary>
        ///     Comparison based on the Order property.
        /// </summary>
        public static bool operator ==(TreeGridDesignerBaseAttribute attr1, TreeGridDesignerBaseAttribute attr2)
        {
            return attr1._order == attr2._order;
        }

        /// <summary>
        ///     Comparison based on the Order property.
        /// </summary>
        public static bool operator !=(TreeGridDesignerBaseAttribute attr1, TreeGridDesignerBaseAttribute attr2)
        {
            return attr1._order != attr2._order;
        }

        /// <summary>
        ///     Comparison based on the Order property.
        /// </summary>
        public static bool operator <=(TreeGridDesignerBaseAttribute attr1, TreeGridDesignerBaseAttribute attr2)
        {
            return attr1._order <= attr2._order;
        }

        /// <summary>
        ///     Comparison based on the Order property.
        /// </summary>
        public static bool operator >=(TreeGridDesignerBaseAttribute attr1, TreeGridDesignerBaseAttribute attr2)
        {
            return attr1._order >= attr2._order;
        }

        /// <summary>
        ///     Comparison based on the Order property.
        /// </summary>
        public static bool operator <(TreeGridDesignerBaseAttribute attr1, TreeGridDesignerBaseAttribute attr2)
        {
            return attr1._order < attr2._order;
        }

        /// <summary>
        ///     Comparison based on the Order property.
        /// </summary>
        public static bool operator >(TreeGridDesignerBaseAttribute attr1, TreeGridDesignerBaseAttribute attr2)
        {
            return attr1._order > attr2._order;
        }

        #endregion

        /// <summary>
        ///     Determines the order of rows or columns to be displayed.  Lower values are
        ///     displayed above or to the left of higher values.
        /// </summary>
        public int Order
        {
            get { return _order; }
            set { _order = value; }
        }
    }

}
