// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class RelationshipDetailsCollectionTests
    {
        [TestMethod]
        public void Verify_RelationshipDetailsCollection_columns()
        {
            RelationshipDetailsCollection relationshipDetailsCollection = new RelationshipDetailsCollection();
            relationshipDetailsCollection.Columns.Count.Should().Be(12);
            VerifyColumn(relationshipDetailsCollection.PKCatalogColumn, "PkCatalog", typeof(string));
            VerifyColumn(relationshipDetailsCollection.PKSchemaColumn, "PkSchema", typeof(string));
            VerifyColumn(relationshipDetailsCollection.PKTableColumn, "PkTable", typeof(string));
            VerifyColumn(relationshipDetailsCollection.PKColumnColumn, "PkColumn", typeof(string));
            VerifyColumn(relationshipDetailsCollection.FKCatalogColumn, "FkCatalog", typeof(string));
            VerifyColumn(relationshipDetailsCollection.FKSchemaColumn, "FkSchema", typeof(string));
            VerifyColumn(relationshipDetailsCollection.FKColumnColumn, "FkColumn", typeof(string));
            VerifyColumn(relationshipDetailsCollection.OrdinalColumn, "Ordinal", typeof(int));
            VerifyColumn(relationshipDetailsCollection.RelationshipNameColumn, "RelationshipName", typeof(string));
            VerifyColumn(relationshipDetailsCollection.RelationshipIdColumn, "RelationshipId", typeof(string));
            VerifyColumn(relationshipDetailsCollection.RelationshipIsCascadeDeleteColumn, "IsCascadeDelete", typeof(bool));
        }

        private void VerifyColumn(DataColumn dataColumn, string name, Type type)
        {
            dataColumn.ColumnName.Should().Be(name);
            dataColumn.DataType.Should().BeSameAs(type);
        }
    }
}
