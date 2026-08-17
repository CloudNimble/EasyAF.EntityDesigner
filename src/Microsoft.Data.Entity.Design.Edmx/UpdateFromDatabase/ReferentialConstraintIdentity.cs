// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.Entity.Design.Edmx.Database;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Common.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.UpdateFromDatabase
{
    internal class ReferentialConstraintIdentity
    {
        //
        // contains a list consisting of <"Principal" -> "Dependent">, 
        // where
        //   "Principal" is the list of DatabaseColumns to which a c-side property included on the PRINCIPAL end of the RC is mapped
        //   "Dependent" is the list of DatabaseColumns to which a c-side property included on the DEPENDENT end of the RC is mapped
        //
        private SortedListAllowDupes<AssociationPropertyIdentity> _propertyIdentities =
            new SortedListAllowDupes<AssociationPropertyIdentity>(AssociationPropertyIdentityComparer.Instance);

        private ReferentialConstraintIdentity()
        {
        }

        internal SortedListAllowDupes<AssociationPropertyIdentity> PropertyIdentities
        {
            get { return _propertyIdentities; }
        }

        internal static ReferentialConstraintIdentity CreateReferentialConstraintIdentity(ReferentialConstraint referentialConstraint)
        {
            if (referentialConstraint == null)
            {
                Debug.Fail("null referential constraint");
            }
            else
            {
                ReferentialConstraintIdentity rcid = new ReferentialConstraintIdentity();
                rcid._propertyIdentities = AssociationPropertyIdentity.CreateIdentitiesFromReferentialConstraint(referentialConstraint);
                return rcid;
            }
            return null;
        }

        internal string TraceString()
        {
            StringBuilder sb = new StringBuilder("[" + typeof(ReferentialConstraintIdentity).Name);
            sb.Append(
                " " + EFToolsTraceUtils.FormatNamedEnumerable(
                    "propertyIdentities", _propertyIdentities,
                    delegate(AssociationPropertyIdentity assocPropId) { return assocPropId.TraceString(); }));

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            foreach (var pmi in _propertyIdentities)
            {
                hashCode ^= pmi.GetHashCode();
            }
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is ReferentialConstraintIdentity that)
            {
                return (ReferentialConstraintIdentityComparer.Instance.Compare(this, that) == 0);
            }
            return false;
        }

        /// <summary>
        ///     See whether the ReferentialConstraintIdentity otherRci contains a "covering" for this
        ///     i.e. for each AssociationPropertyIdentity in this._propertyIdentities see whether
        ///     otherRci._propertyIdentities contains a principal->dependent mapping which has
        ///     at least the same principal DatabaseColumns (but possibly more) _and_ at least
        ///     the same dependent DatabaseColumns (but possibly more).
        ///     This allows for treating these ReferentialConstraintIdentity's as identical for the purposes
        ///     of Update Model even if a given C-side property has been mapped to more than 1 S-side property.
        /// </summary>
        internal bool IsCoveredBy(ReferentialConstraintIdentity otherRci)
        {
            foreach (var thisApi in _propertyIdentities)
            {
                var foundCoveringApi = false;
                if (null != otherRci)
                {
                    foreach (var otherApi in otherRci._propertyIdentities)
                    {
                        if (thisApi.IsCoveredBy(otherApi))
                        {
                            // have found an AssociationPropertyIdentity in other._propertyIdentities which covers thisApi
                            foundCoveringApi = true;
                            break;
                        }
                    }
                }

                if (false == foundCoveringApi)
                {
                    // no covering AssociationPropertyIdentity was found for thisApi in 
                    // other._propertyIdentities
                    return false;
                }
            }

            // all AssociationPropertyIdentity's in this._propertyIdentities were covered by
            // AssociationPropertyIdentity's in otherRci._propertyIdentities
            return true;
        }

        /// <summary>
        ///     Return a list of all tables that are referenced by the dependent properties in the ref constraint
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DatabaseObject> GetDependentTables()
        {
            HashSet<DatabaseObject> tables = new HashSet<DatabaseObject>();
            foreach (var pmi in _propertyIdentities)
            {
                foreach (var dc in pmi.DependentColumns)
                {
                    tables.Add(dc.Table);
                }
            }
            return tables;
        }
    }

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
