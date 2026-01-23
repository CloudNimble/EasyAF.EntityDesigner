// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
    using Xunit.Extensions;

    [TestClass]
    public class TableConfigurationTests
    {
        [TestMethod]
        public void GetAttributeBody_returns_body()
        {
            var configuration = new TableConfiguration { Table = "Entities" };
            var code = new CSharpCodeHelper();

            Assert.Equal("Table(\"Entities\")", configuration.GetAttributeBody(code));
        }

        [TestMethod]
        public void GetMethodChain_returns_body()
        {
            var configuration = new TableConfiguration { Table = "Entities" };
            var code = new CSharpCodeHelper();

            Assert.Equal(".ToTable(\"Entities\")", configuration.GetMethodChain(code));
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
            var configuration = new TableConfiguration { Schema = schema, Table = table };

            Assert.Equal(expected, configuration.GetName());
        }
    }
}
