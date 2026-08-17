// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    [TestClass]
    public class AssociationSetEndDetailsTests
    {
        [TestMethod]
        public void Can_set_get_association_set_end_details()
        {
            EntityType entity = EntityType.Create("E", "ns", DataSpace.CSpace, new string[0], new EdmMember[0], null);
            EntitySet entitySet = EntitySet.Create("es1", null, null, null, entity, null);
            AssociationEndMember endMember = AssociationEndMember.Create(
                "aem1", entity.GetReferenceType(), RelationshipMultiplicity.One, OperationAction.None, null);
            AssociationType associationType = AssociationType.Create("at1", "ns", false, DataSpace.CSpace, endMember, null, null, null);
            AssociationSet assocationSet = AssociationSet.Create("as1", associationType, entitySet, null, null);

            var associationSetEnd = assocationSet.AssociationSetEnds[0];

            AssociationSetEndDetails associationSetEndDetails =
                new AssociationSetEndDetails(
                    associationSetEnd,
                    (RelationshipMultiplicity)(-42),
                    (OperationAction)(-100));

            associationSetEndDetails.AssociationSetEnd.Should().BeSameAs(associationSetEnd);
            ((int)associationSetEndDetails.Multiplicity).Should().Be(-42);
            ((int)associationSetEndDetails.DeleteBehavior).Should().Be(-100);
        }
    }
}
