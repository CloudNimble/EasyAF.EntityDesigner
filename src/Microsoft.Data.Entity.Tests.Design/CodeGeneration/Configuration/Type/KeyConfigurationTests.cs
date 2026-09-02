// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.Type
{
    [TestClass]
    public class KeyConfigurationTests
    {
        [TestMethod, Ignore("Different API Visiblity between official dll and locally built")]
        public void GetMethodChain_returns_chain_when_one_key_property()
        {
            //var configuration = new KeyConfiguration { KeyProperties = { new EdmProperty("Id") } };
            //var code = new CSharpCodeHelper();

            //configuration.GetMethodChain(code).Should().Be(".HasKey(e => e.Id)");
        }

        [TestMethod, Ignore("Different API Visiblity between official dll and locally built")]
        public void GetMethodChain_returns_chain_when_more_than_one_key_property()
        {
            //var configuration = new KeyConfiguration
            //    {
            //        KeyProperties = { new EdmProperty("Id1"), new EdmProperty("Id2") }
            //    };
            //var code = new CSharpCodeHelper();

            //configuration.GetMethodChain(code).Should().Be(".HasKey(e => new { e.Id1, e.Id2 })");
        }
    }
}
