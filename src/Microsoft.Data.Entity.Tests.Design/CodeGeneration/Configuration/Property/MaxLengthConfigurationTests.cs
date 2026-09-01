// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.Property
{
    [TestClass]
    public class MaxLengthConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            MaxLengthConfiguration configuration = new MaxLengthConfiguration { MaxLength = 30 };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("MaxLength(30)");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            MaxLengthConfiguration configuration = new MaxLengthConfiguration { MaxLength = 30 };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".HasMaxLength(30)");
        }
    }
}
