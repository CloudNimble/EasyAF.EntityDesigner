// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.Property
{
    [TestClass]
    public class TimestampConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            TimestampConfiguration configuration = new TimestampConfiguration();
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Timestamp");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            TimestampConfiguration configuration = new TimestampConfiguration();
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".IsRowVersion()");
        }
    }
}
