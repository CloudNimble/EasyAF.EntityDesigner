// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Edmx.Database
{

    internal class DatabaseObjectComparer : IComparer<DatabaseObject>
    {
        public int Compare(DatabaseObject x, DatabaseObject y)
        {
            var compareSchemas = String.Compare(x.Schema, y.Schema, StringComparison.CurrentCulture);
            if (compareSchemas != 0)
            {
                return compareSchemas;
            }
            else
            {
                return String.Compare(x.Name, y.Name, StringComparison.CurrentCulture);
            }
        }
    }

}
