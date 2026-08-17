// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.Property
{
    [TestClass]
    public class NonUnicodeConfigurationTests
    {
        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            NonUnicodeConfiguration configuration = new NonUnicodeConfiguration();
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".IsUnicode(false)");
        }
    }
}
