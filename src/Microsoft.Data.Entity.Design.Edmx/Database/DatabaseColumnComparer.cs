// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Edmx.Database
{

    internal class DatabaseColumnComparer : IComparer<DatabaseColumn>
    {
        private readonly DatabaseObjectComparer _tableComparer = new DatabaseObjectComparer();

        public int Compare(DatabaseColumn x, DatabaseColumn y)
        {
            var compareTables = _tableComparer.Compare(x.Table, y.Table);
            if (compareTables != 0)
            {
                return compareTables;
            }
            else
            {
                return String.Compare(x.Column, y.Column, StringComparison.CurrentCulture);
            }
        }
    }

}
