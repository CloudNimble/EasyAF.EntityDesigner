// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity.Core.Metadata.Edm;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.Property
{
    [TestClass]
    public class DatabaseGeneratedConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            DatabaseGeneratedConfiguration configuration = new DatabaseGeneratedConfiguration
                {
                    StoreGeneratedPattern = StoreGeneratedPattern.Computed
                };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("DatabaseGenerated(DatabaseGeneratedOption.Computed)");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain()
        {
            DatabaseGeneratedConfiguration configuration = new DatabaseGeneratedConfiguration
                {
                    StoreGeneratedPattern = StoreGeneratedPattern.Computed
                };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)");
        }
    }
}
