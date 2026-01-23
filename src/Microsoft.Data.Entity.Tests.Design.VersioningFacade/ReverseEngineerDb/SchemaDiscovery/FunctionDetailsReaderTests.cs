// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class FunctionDetailsReaderTests
    {
        private readonly EntityClientMockFactory entityClientMockFactory
            = new EntityClientMockFactory();

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void CurrentRow_is_null_for_empty_reader()
        {
            using (var functionDetailsReader =
                new FunctionDetailsReader(
                    entityClientMockFactory.CreateMockEntityCommand(null).Object,
                    EntityFrameworkVersion.Version3))
            {
                functionDetailsReader.CurrentRow.Should().BeNull();
                functionDetailsReader.Read(.Should().BeFalse());
                functionDetailsReader.CurrentRow.Should().BeNull();
            }
        }

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void CurrentRow_exposes_underlying_reader_values()
        {
            var expectedValues = new object[12];
            expectedValues[0] = "catalog";

            using (var functionDetailsReader =
                new FunctionDetailsReader(
                    entityClientMockFactory.CreateMockEntityCommand(
                        new List<object[]> { expectedValues }).Object,
                    EntityFrameworkVersion.Version3))
            {
                functionDetailsReader.CurrentRow.Should().BeNull();
                functionDetailsReader.Read(.Should().BeTrue());
                functionDetailsReader.CurrentRow.Should().NotBeNull();
                functionDetailsReader.CurrentRow.Catalog.Should().Be("catalog");
                functionDetailsReader.Read(.Should().BeFalse());
                functionDetailsReader.CurrentRow.Should().BeNull();
            }
        }
    }
}
