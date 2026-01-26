// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    using System.Text.RegularExpressions;
    using FluentAssertions;
    using Microsoft.Data.Entity.Design.CodeGeneration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DefaultVBContextGeneratorTests : GeneratorTestBase
    {
        private static string NormalizeCode(string code) =>
            Regex.Replace(code.Replace("\r\n", "\n"), @"[ \t]+\n", "\n");

        [TestMethod]
        public void Generate_returns_code()
        {
            var generator = new DefaultVBContextGenerator();
            var result = NormalizeCode(generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString"));

            result.Should().Be(NormalizeCode(
                @"Imports System
Imports System.Data.Entity
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Linq

Partial Public Class MyContext
    Inherits DbContext

    Public Sub New()
        MyBase.New(""name=MyContextConnString"")
    End Sub

    Public Overridable Property Entities As DbSet(Of Entity)

    Protected Overrides Sub OnModelCreating(ByVal modelBuilder As DbModelBuilder)
    End Sub
End Class
"));
        }
    }
}
