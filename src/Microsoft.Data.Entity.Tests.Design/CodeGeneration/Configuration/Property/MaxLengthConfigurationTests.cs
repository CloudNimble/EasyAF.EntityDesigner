// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class MaxLengthConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            var configuration = new MaxLengthConfiguration { MaxLength = 30 };
            var code = new CSharpCodeHelper();

            Assert.Equal("MaxLength(30)", configuration.GetAttributeBody(code));
        }

        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            var configuration = new MaxLengthConfiguration { MaxLength = 30 };
            var code = new CSharpCodeHelper();

            Assert.Equal(".HasMaxLength(30)", configuration.GetMethodChain(code));
        }
    }
}
