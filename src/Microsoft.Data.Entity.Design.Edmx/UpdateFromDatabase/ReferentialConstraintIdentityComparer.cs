// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Edmx.UpdateFromDatabase
{

    internal class ReferentialConstraintIdentityComparer : IComparer<ReferentialConstraintIdentity>
    {
        private static readonly ReferentialConstraintIdentityComparer _instance = new ReferentialConstraintIdentityComparer();

        internal static ReferentialConstraintIdentityComparer Instance
        {
            get { return _instance; }
        }

        private ReferentialConstraintIdentityComparer()
        {
        }

        public int Compare(ReferentialConstraintIdentity x, ReferentialConstraintIdentity y)
        {
            return SortedListAllowDupes<AssociationPropertyIdentity>.CompareListContents(x.PropertyIdentities, y.PropertyIdentities);
        }
    }

}
