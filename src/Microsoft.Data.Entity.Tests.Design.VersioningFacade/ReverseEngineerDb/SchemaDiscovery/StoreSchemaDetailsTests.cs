// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class StoreSchemaDetailsTests
    {
        [TestMethod]
        public void StoreSchemaDetails_initialized_correctly()
        {
            var tableDetails = Enumerable.Empty<TableDetailsRow>();
            var viewDetails = Enumerable.Empty<TableDetailsRow>();
            var relationshipDetails = Enumerable.Empty<RelationshipDetailsRow>();
            var functionDetails = Enumerable.Empty<FunctionDetailsRowView>();
            var tvfReturnTypeDetails = Enumerable.Empty<TableDetailsRow>();

            var storeSchemaDetails = new StoreSchemaDetails(
                tableDetails, viewDetails, relationshipDetails, functionDetails, tvfReturnTypeDetails);

            storeSchemaDetails.TableDetails.Should().BeSameAs(tableDetails);
            storeSchemaDetails.ViewDetails.Should().BeSameAs(viewDetails);
            storeSchemaDetails.RelationshipDetails.Should().BeSameAs(relationshipDetails);
            storeSchemaDetails.FunctionDetails.Should().BeSameAs(functionDetails);
            storeSchemaDetails.TVFReturnTypeDetails.Should().BeSameAs(tvfReturnTypeDetails);
        }
    }
}
