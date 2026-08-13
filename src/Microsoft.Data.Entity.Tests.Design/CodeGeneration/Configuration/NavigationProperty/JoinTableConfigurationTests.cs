// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.NavigationProperties;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class JoinTableConfigurationTests
    {
        [TestMethod]
        public void GetMethodChain_returns_chain_when_table()
        {
            JoinTableConfiguration configuration = new JoinTableConfiguration { Table = "Subscriptions" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".Map(m => m.ToTable(\"Subscriptions\"))");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain_when_table_and_schema()
        {
            JoinTableConfiguration configuration = new JoinTableConfiguration { Table = "Subscriptions", Schema = "Sales" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".Map(m => m.ToTable(\"Subscriptions\", \"Sales\"))");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain_when_one_left_key()
        {
            JoinTableConfiguration configuration = new JoinTableConfiguration { LeftKeys = { "CustomerId" } };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".Map(m => m.MapLeftKey(\"CustomerId\"))");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain_when_more_than_one_left_key()
        {
            JoinTableConfiguration configuration = new JoinTableConfiguration { LeftKeys = { "CustomerId1", "CustomerId2" } };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".Map(m => m.MapLeftKey(new[] { \"CustomerId1\", \"CustomerId2\" }))");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain_when_one_right_key()
        {
            JoinTableConfiguration configuration = new JoinTableConfiguration { RightKeys = { "ServiceId" } };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".Map(m => m.MapRightKey(\"ServiceId\"))");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain_when_more_than_one_right_key()
        {
            JoinTableConfiguration configuration = new JoinTableConfiguration { RightKeys = { "ServiceId1", "ServiceId2" } };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".Map(m => m.MapRightKey(new[] { \"ServiceId1\", \"ServiceId2\" }))");
        }

        [TestMethod]
        public void GetMethodChain_returns_chain_when_all()
        {
            JoinTableConfiguration configuration = new JoinTableConfiguration
                {
                    Table = "Subscriptions",
                    LeftKeys = { "CustomerId" },
                    RightKeys = { "ServiceId" }
                };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".Map(m => m.ToTable(\"Subscriptions\").MapLeftKey(\"CustomerId\").MapRightKey(\"ServiceId\"))");
        }
    }
}
