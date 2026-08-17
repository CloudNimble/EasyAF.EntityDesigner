// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;

namespace Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb
{
    internal class AssociationSetEndDetails
    {
        public readonly AssociationSetEnd AssociationSetEnd;
        public readonly RelationshipMultiplicity Multiplicity;
        public readonly OperationAction DeleteBehavior;

        public AssociationSetEndDetails(
            AssociationSetEnd associationSetEnd, RelationshipMultiplicity multiplicity,
            OperationAction deleteBehavior)
        {
            AssociationSetEnd = associationSetEnd;
            Multiplicity = multiplicity;
            DeleteBehavior = deleteBehavior;
        }
    }
}
