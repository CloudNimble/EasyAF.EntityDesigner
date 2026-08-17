// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.NavigationProperties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.NavigationProperty
{
    [TestClass]
    public class CascadeDeleteConfigurationTests
    {
        [TestMethod]
        public void GetMethodChain_returns_chain_when_cascade()
        {
            CascadeDeleteConfiguration configuration = new CascadeDeleteConfiguration { DeleteBehavior = OperationAction.Cascade };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".WillCascadeOnDelete()");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain_when_none()
        {
            CascadeDeleteConfiguration configuration = new CascadeDeleteConfiguration { DeleteBehavior = OperationAction.None };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".WillCascadeOnDelete(false)");
        }
    }
}
