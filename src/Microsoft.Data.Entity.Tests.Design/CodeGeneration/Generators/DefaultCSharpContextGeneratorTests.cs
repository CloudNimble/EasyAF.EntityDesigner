// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Generators
{
    [TestClass]
    public class DefaultCSharpContextGeneratorTests : GeneratorTestBase
    {
        private static string NormalizeCode(string code) =>
            Regex.Replace(code.Replace("\r\n", "\n"), @"[ \t]+\n", "\n");

        [TestMethod]
        public void Generate_returns_code()
        {
            DefaultCSharpContextGenerator generator = new DefaultCSharpContextGenerator();
            var result = NormalizeCode(generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString"));

            result.Should().Be(NormalizeCode(
                @"namespace WebApplication1.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class MyContext : DbContext
    {
        public MyContext()
            : base(""name=MyContextConnString"")
        {
        }

        public virtual DbSet<Entity> Entities { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}
"));
        }
    }
}
