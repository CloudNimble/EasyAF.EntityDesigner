// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    /// <summary>
    ///     The refName for these ends cannot be already normalized.  The Role attribute points to
    ///     an End of the Assocation from the AssociationSet, so it already has its scope set in stone.
    ///     The EntitySet attribute points to an EntitySet that must be in the current EntityContainer
    ///     and EntitySet names don't use the schema alias or namespace.
    /// </summary>
    internal static class AssociationSetEndEntitySetNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            // cast the parameter to what this really is
            AssociationSetEnd end = parent as AssociationSetEnd;
            Debug.Assert(end != null, "parent should be an AssociationSetEnd");

            // get the assoc set
            AssociationSet set = end.Parent as AssociationSet;
            Debug.Assert(set != null, "association set end parent should be an AssociationSet");

            // get the entity container name
            string entityContainerName = null;
            if (set.Parent is BaseEntityContainer ec)
            {
                Debug.Assert(ec != null, "AssociationSet parent should be a subclass of BaseEntityContainer");
                entityContainerName = ec.EntityContainerName;
            }

            // the normalized name for an EnitySet is 'EntityContainerName + # + EntitySetName'
            Symbol symbol = new Symbol(entityContainerName, refName);

            NormalizedName normalizedName = new NormalizedName(symbol, null, null, refName);
            return normalizedName;
        }
    }
}
