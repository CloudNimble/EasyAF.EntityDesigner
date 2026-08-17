// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    [TestClass]
    public class StringBuilderExtensionsTests
    {
        [TestMethod]
        public void StringBuilder_AppendIfNotEmpty_appends_string_to_non_empty_StringBuilder()
        {
            new StringBuilder("a").AppendIfNotEmpty("b").ToString().Should().Be("ab");
        }

        [TestMethod]
        public void StringBuilder_AppendIfNotEmpty_does_not_append_string_to_empty_StringBuilder()
        {
            new StringBuilder().AppendIfNotEmpty("b").ToString().Should().Be(string.Empty);
        }
    }
}
