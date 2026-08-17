// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    [TestClass]
    public class ModelGeneratorUtilsTests
    {
        [TestClass]
        public class CreateValidEcmaNameTests
        {
            [TestMethod]
            public void CreateValidEcmaName_does_not_change_valid_ECMA_name()
            {
                ModelGeneratorUtils.CreateValidEcmaName("foo", 'a').Should().Be("foo");
            }

            [TestMethod]
            public void CreateValidEcmaName_replaces_invalid_ECMA_chars_with_underscore()
            {
                ModelGeneratorUtils.CreateValidEcmaName("f#o", 'a').Should().Be("f_o");
            }

            [TestMethod]
            public void CreateValidEcmaName_can_handle_empty_name()
            {
                ModelGeneratorUtils.CreateValidEcmaName(string.Empty, 'a').Should().Be("a");
            }

            [TestMethod]
            public void CreateValidEcmaName_prepends_name_with_ECMA_char_if_the_first_char_was_replaced_with_underscore()
            {
                ModelGeneratorUtils.CreateValidEcmaName("@foo", 'a').Should().Be("a_foo");
            }
        }
    }
}
