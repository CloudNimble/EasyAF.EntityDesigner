// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class FunctionDetailsReaderTests
    {
        private readonly EntityClientMockFactory entityClientMockFactory
            = new EntityClientMockFactory();

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void CurrentRow_is_null_for_empty_reader()
        {
            using (FunctionDetailsReader functionDetailsReader =
                new FunctionDetailsReader(
                    entityClientMockFactory.CreateMockEntityCommand(null).Object,
                    EntityFrameworkVersion.Version3))
            {
                functionDetailsReader.CurrentRow.Should().BeNull();
                functionDetailsReader.Read().Should().BeFalse();
                functionDetailsReader.CurrentRow.Should().BeNull();
            }
        }

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void CurrentRow_exposes_underlying_reader_values()
        {
            var expectedValues = new object[12];
            expectedValues[0] = "catalog";

            using (FunctionDetailsReader functionDetailsReader =
                new FunctionDetailsReader(
                    entityClientMockFactory.CreateMockEntityCommand(
                        [expectedValues]).Object,
                    EntityFrameworkVersion.Version3))
            {
                functionDetailsReader.CurrentRow.Should().BeNull();
                functionDetailsReader.Read().Should().BeTrue();
                functionDetailsReader.CurrentRow.Should().NotBeNull();
                functionDetailsReader.CurrentRow.Catalog.Should().Be("catalog");
                functionDetailsReader.Read().Should().BeFalse();
                functionDetailsReader.CurrentRow.Should().BeNull();
            }
        }
    }
}
