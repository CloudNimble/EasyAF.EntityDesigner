// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class MaxLengthStringConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            var configuration = new MaxLengthStringConfiguration { MaxLength = 30 };
            var code = new CSharpCodeHelper();

            Assert.Equal("StringLength(30)", configuration.GetAttributeBody(code));
        }
    }
}
