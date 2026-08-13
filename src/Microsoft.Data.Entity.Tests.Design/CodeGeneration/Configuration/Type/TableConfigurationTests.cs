// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Types;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class TableConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            TableConfiguration configuration = new TableConfiguration { Table = "Entities" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetAttributeBody(code).Should().Be("Table(\"Entities\")");
        }

        [TestMethod]
        public void GetMethodChain_returns_body()
        {
            TableConfiguration configuration = new TableConfiguration { Table = "Entities" };
            CSharpCodeHelper code = new CSharpCodeHelper();

            configuration.GetMethodChain(code).Should().Be(".ToTable(\"Entities\")");
        }

        [DataTestMethod]
        [DataRow(null, "One", "One")]
        [DataRow("One", "Two", "One.Two")]
        [DataRow(null, "One.Two", "[One.Two]")]
        [DataRow("One.Two", "Three", "[One.Two].Three")]
        [DataRow("One", "Two.Three", "One.[Two.Three]")]
        [DataRow("One.Two", "Three.Four", "[One.Two].[Three.Four]")]
        public void GetName_escapes_parts_when_dot(string schema, string table, string expected)
        {
            TableConfiguration configuration = new TableConfiguration { Schema = schema, Table = table };

            configuration.GetName().Should().Be(expected);
        }
    }
}
