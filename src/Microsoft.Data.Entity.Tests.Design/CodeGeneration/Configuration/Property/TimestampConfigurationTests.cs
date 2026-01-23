// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class TimestampConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            var configuration = new TimestampConfiguration();
            var code = new CSharpCodeHelper();

            Assert.Equal("Timestamp", configuration.GetAttributeBody(code));
        }

        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            var configuration = new TimestampConfiguration();
            var code = new CSharpCodeHelper();

            Assert.Equal(".IsRowVersion()", configuration.GetMethodChain(code));
        }
    }
}
