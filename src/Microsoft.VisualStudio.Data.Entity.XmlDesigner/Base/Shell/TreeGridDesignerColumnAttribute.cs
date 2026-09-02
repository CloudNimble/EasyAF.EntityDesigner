// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell
{

    /// <summary>
    ///     Attribute which may be placed on a selectable object to specify a
    ///     column that should be displayed in the TreeGridDesigner
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class TreeGridDesignerColumnAttribute : TreeGridDesignerBaseAttribute
    {
        private readonly Type _columnType;

        /// <summary>
        ///     Construct an empty OperationDesignerColumnsAttribute
        /// </summary>
        internal TreeGridDesignerColumnAttribute()
            : this(null)
        {
        }

        /// <summary>
        ///     Construct an OperationDesignerColumnAttribute with the given column type.
        /// </summary>
        /// <param name="columnType">Type of column to display</param>
        internal TreeGridDesignerColumnAttribute(Type columnType)
        {
            _columnType = columnType;
            InitialPercentage = TreeGridDesignerColumnDescriptor.CalculatePercentage;
        }

        /// <summary>
        ///     Type of column to be created.
        /// </summary>
        internal Type ColumnType
        {
            get { return _columnType; }
        }

        /// <summary>
        ///     Initial width of this column, as a percentage of the total width.  A value of
        ///     The default value of ColumnDescriptor.CalculatePercentage indicates that the percentage should be calculated by the tree control.
        ///     Otherwise, the value should be in the range (0, 1).
        /// </summary>
        public float InitialPercentage { get; set; }
    }

}
