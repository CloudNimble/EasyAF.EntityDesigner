// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Data.Entity.Design.Edmx.Database
{
    /// <summary>
    ///     Represents the full name of a column on a table on a database
    ///     including the schema
    /// </summary>
    internal struct DatabaseColumn
    {
        internal DatabaseObject Table;
        internal string Column;

        public override bool Equals(object obj)
        {
            if (null == obj)
            {
                return false;
            }

            if (typeof(DatabaseColumn) != obj.GetType())
            {
                return false;
            }
            DatabaseColumn objAsDatabaseColumn = (DatabaseColumn)obj;

            return (Table.Equals(objAsDatabaseColumn.Table)
                    && Column == objAsDatabaseColumn.Column);
        }

        public override int GetHashCode()
        {
            var columnHashCode = (Column != null ? Column.GetHashCode() : 0);
            return Table.GetHashCode() ^ columnHashCode;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, EdmxResources.DatabaseColumnNameFormat, Table.ToString(), Column);
        }

        internal static DatabaseColumn CreateFromProperty(Property prop)
        {
            StorageEntitySet ses = prop.EntityType.EntitySet as StorageEntitySet;
            Debug.Assert(ses != null, "Property " + prop.ToPrettyString() + " does not have S-side EntitySet");
            DatabaseObject tableOrView = DatabaseObject.CreateFromEntitySet(ses);

            DatabaseColumn column = new DatabaseColumn();
            column.Table = tableOrView;

            Debug.Assert(prop.LocalName.Value != null, "Property " + prop.ToPrettyString() + " does not have Name");
            column.Column = prop.LocalName.Value;
            return column;
        }
    }
}
