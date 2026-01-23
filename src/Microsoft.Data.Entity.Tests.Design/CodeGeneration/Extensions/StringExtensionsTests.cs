// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Extensions
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void ContainsIgnoreCase_ignores_case()
        {
            var source = new[] { "AAA", "BBB" };

            source.ContainsIgnoreCase("aaa".Should().BeTrue());
            source.ContainsIgnoreCase("Aaa".Should().BeTrue());
            source.ContainsIgnoreCase("AAA".Should().BeTrue());
        }

        [TestMethod]
        public void ContainsIgnoreCase_returns_false_when_not_found()
        {
            new[] { "AAA", "BBB" }.ContainsIgnoreCase("CCC".Should().BeFalse());
        }
    }
}
