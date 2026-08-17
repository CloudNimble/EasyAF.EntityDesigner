// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Edmx.Database;

namespace Microsoft.Data.Entity.Design.Edmx.UpdateFromDatabase
{

    internal class AssociationPropertyIdentityComparer : IComparer<AssociationPropertyIdentity>
    {
        private static readonly AssociationPropertyIdentityComparer _instance = new AssociationPropertyIdentityComparer();

        internal static AssociationPropertyIdentityComparer Instance
        {
            get { return _instance; }
        }

        private AssociationPropertyIdentityComparer()
        {
        }

        public int Compare(AssociationPropertyIdentity x, AssociationPropertyIdentity y)
        {
            var compVal = SortedListAllowDupes<DatabaseColumn>.CompareListContents(x.PrincipalColumns, y.PrincipalColumns);
            if (compVal == 0)
            {
                // left columns are equal, compare right columsn
                compVal = SortedListAllowDupes<DatabaseColumn>.CompareListContents(x.DependentColumns, y.DependentColumns);
            }
            return compVal;
        }
    }

}
