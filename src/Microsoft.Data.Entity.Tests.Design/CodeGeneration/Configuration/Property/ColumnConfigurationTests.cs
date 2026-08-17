// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Configuration.Property
{
    [TestClass]
    public class ColumnConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body_when_name()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Name = "Id" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Column(\"Id\")");
        }

        [TestMethod]
        public void GetAttributeBody_returns_body_when_order()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Order = 0 };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Column(Order = 0)");
        }

        [TestMethod]
        public void GetAttributeBody_returns_body_when_type()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { TypeName = "int" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Column(TypeName = \"int\")");
        }

        [TestMethod]
        public void GetAttributeBody_returns_body_when_order_and_type()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Order = 0, TypeName = "int" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Column(Order = 0, TypeName = \"int\")");
        }

        [TestMethod]
        public void GetAttributeBody_returns_body_when_order_and_type_and_vb()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Order = 0, TypeName = "int" };
            VBCodeHelper code = new VBCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Column(Order:=0, TypeName:=\"int\")");
        }

        [TestMethod]
        public void GetAttributeBody_returns_body_when_all()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Name = "Id", Order = 0, TypeName = "int" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Column(\"Id\", Order = 0, TypeName = \"int\")");
        }

        [TestMethod]
        public void GetMethodChain_returns_body_when_name()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Name = "Id" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".HasColumnName(\"Id\")");
        }

        [TestMethod]
        public void GetMethodChain_returns_body_when_order()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Order = 0 };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".HasColumnOrder(0)");
        }

        [TestMethod]
        public void GetMethodChain_returns_body_when_type()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { TypeName = "int" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".HasColumnType(\"int\")");
        }

        [TestMethod]
        public void GetMethodChain_returns_body_when_all()
        {
            ColumnConfiguration configuration = new ColumnConfiguration { Name = "Id", Order = 0, TypeName = "int" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".HasColumnName(\"Id\").HasColumnOrder(0).HasColumnType(\"int\")");
        }
    }
}
