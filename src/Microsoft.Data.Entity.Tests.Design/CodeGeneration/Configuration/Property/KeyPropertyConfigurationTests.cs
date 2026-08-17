// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.Property
{
    [TestClass]
    public class KeyPropertyConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            KeyPropertyConfiguration configuration = new KeyPropertyConfiguration();
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Key");
        }
    }
}
