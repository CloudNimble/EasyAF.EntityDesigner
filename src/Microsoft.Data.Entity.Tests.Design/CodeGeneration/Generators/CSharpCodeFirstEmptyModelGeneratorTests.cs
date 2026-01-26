// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Generators
{
    using System.Globalization;
    using System.Text.RegularExpressions;
    using FluentAssertions;
    using Microsoft.Data.Entity.Design.CodeGeneration;
    using Microsoft.Data.Entity.Design.CodeGeneration.Generators;
    using Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Properties;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CSharpCodeFirstEmptyModelGeneratorTests
    {
        private static string NormalizeCode(string code) =>
            Regex.Replace(code.Replace("\r\n", "\n"), @"[ \t]+\n", "\n");

        [TestMethod]
        public void CSharpCodeFirstEmptyModelGenerator_generates_code()
        {
            var generatedCode = NormalizeCode(new CSharpCodeFirstEmptyModelGenerator()
                .Generate(null, "ConsoleApplication.Data", "MyContext", "MyContextConnString"));

            var ctorComment = NormalizeCode(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.CodeFirstCodeFile_CtorComment_CS,
                    "MyContext",
                    "ConsoleApplication.Data"));

            var expected = NormalizeCode(@"namespace ConsoleApplication.Data
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class MyContext : DbContext
    {
        " + ctorComment + @"
        public MyContext()
            : base(""name=MyContextConnString"")
        {
        }

        " + NormalizeCode(Resources.CodeFirstCodeFile_DbSetComment_CS) + @"

        // public virtual DbSet<MyEntity> MyEntities { get; set; }
    }

    //public class MyEntity
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //}
}");

            generatedCode.Should().Be(expected);
        }
    }
}
