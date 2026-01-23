// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class StringBuilderExtensionsTests
    {
        [TestMethod]
        public void StringBuilder_AppendIfNotEmpty_appends_string_to_non_empty_StringBuilder()
        {
            Assert.Equal("ab", new StringBuilder("a").AppendIfNotEmpty("b").ToString());
        }

        [TestMethod]
        public void StringBuilder_AppendIfNotEmpty_does_not_append_string_to_empty_StringBuilder()
        {
            Assert.Equal(string.Empty, new StringBuilder().AppendIfNotEmpty("b").ToString());
        }
    }
}
