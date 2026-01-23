// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class PrecisionDecimalConfigurationTests
    {
        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            var configuration = new PrecisionDecimalConfiguration { Precision = 8, Scale = 2 };
            var code = new CSharpCodeHelper();

            Assert.Equal(".HasPrecision(8, 2)", configuration.GetMethodChain(code));
        }
    }
}
