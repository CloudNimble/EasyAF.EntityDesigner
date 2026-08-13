// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Properties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class FixedLengthConfigurationTests
    {
        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            FixedLengthConfiguration configuration = new FixedLengthConfiguration();
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".IsFixedLength()");
        }
    }
}
