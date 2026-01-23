// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    using System.Data.Entity.Core.Metadata.Edm;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class AssociationSetEndDetailsTests
    {
        [TestMethod]
        public void Can_set_get_association_set_end_details()
        {
            var entity = EntityType.Create("E", "ns", DataSpace.CSpace, new string[0], new EdmMember[0], null);
            var entitySet = EntitySet.Create("es1", null, null, null, entity, null);
            var endMember = AssociationEndMember.Create(
                "aem1", entity.GetReferenceType(), RelationshipMultiplicity.One, OperationAction.None, null);
            var associationType = AssociationType.Create("at1", "ns", false, DataSpace.CSpace, endMember, null, null, null);
            var assocationSet = AssociationSet.Create("as1", associationType, entitySet, null, null);

            var associationSetEnd = assocationSet.AssociationSetEnds[0];

            var associationSetEndDetails =
                new AssociationSetEndDetails(
                    associationSetEnd,
                    (RelationshipMultiplicity)(-42),
                    (OperationAction)(-100));

            associationSetEndDetails.AssociationSetEnd.Should().BeSameAs(associationSetEnd);
            Assert.Equal(-42, (int)associationSetEndDetails.Multiplicity);
            Assert.Equal(-100, (int)associationSetEndDetails.DeleteBehavior);
        }
    }
}
