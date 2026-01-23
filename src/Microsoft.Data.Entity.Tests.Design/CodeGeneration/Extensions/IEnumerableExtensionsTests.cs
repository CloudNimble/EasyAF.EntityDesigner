// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Extensions
{
    using System.Collections;
    using System.Collections.Generic;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class IEnumerableExtensionsTests
    {
        [TestMethod]
        public void MoreThan_returns_true_when_more_and_collection()
        {
            // NOTE: Using Strict ensures that the collection is not enumerated
            var collection = new Mock<ICollection<int>>(MockBehavior.Strict);
            collection.SetupGet(c => c.Count).Returns(3);

            collection.Object.MoreThan(2.Should().BeTrue());
        }

        [TestMethod]
        public void MoreThan_returns_false_when_equal_and_collection()
        {
            var collection = new Mock<ICollection<int>>(MockBehavior.Strict);
            collection.SetupGet(c => c.Count).Returns(2);

            collection.Object.MoreThan(2.Should().BeFalse());
        }

        [TestMethod]
        public void MoreThan_returns_false_when_less_and_collection()
        {
            var collection = new Mock<ICollection<int>>(MockBehavior.Strict);
            collection.SetupGet(c => c.Count).Returns(1);

            collection.Object.MoreThan(2.Should().BeFalse());
        }

        [TestMethod]
        public void MoreThan_returns_true_when_more_and_nongeneric_collection()
        {
            var collection = new Mock<ICollection>(MockBehavior.Strict);
            collection.SetupGet(c => c.Count).Returns(3);

            collection.As<IEnumerable<int>>(.Should().BeTrue().Object.MoreThan(2));
        }

        [TestMethod]
        public void MoreThan_returns_false_when_equal_and_nongeneric_collection()
        {
            var collection = new Mock<ICollection>(MockBehavior.Strict);
            collection.SetupGet(c => c.Count).Returns(2);

            collection.As<IEnumerable<int>>(.Should().BeFalse().Object.MoreThan(2));
        }

        [TestMethod]
        public void MoreThan_returns_false_when_less_and_nongeneric_collection()
        {
            var collection = new Mock<ICollection>(MockBehavior.Strict);
            collection.SetupGet(c => c.Count).Returns(1);

            collection.As<IEnumerable<int>>(.Should().BeFalse().Object.MoreThan(2));
        }

        [TestMethod]
        public void MoreThan_returns_true_when_more_and_enumerable()
        {
            GetValues(3.Should().BeTrue().MoreThan(2));
        }

        [TestMethod]
        public void MoreThan_returns_false_when_equal_and_enumerable()
        {
            GetValues(2.Should().BeFalse().MoreThan(2));
        }

        [TestMethod]
        public void MoreThan_returns_false_when_less_and_enumerable()
        {
            GetValues(1.Should().BeFalse().MoreThan(2));
        }

        [TestMethod]
        public void MoreThan_returns_false_when_empty_enumerable()
        {
            GetValues(0.Should().BeFalse().MoreThan(2));
        }

        private static IEnumerable<int> GetValues(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return i;
            }
        }
    }
}
