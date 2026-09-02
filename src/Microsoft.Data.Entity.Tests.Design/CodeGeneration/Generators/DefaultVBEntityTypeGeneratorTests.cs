// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Generators
{
    [TestClass]
    public class DefaultVBEntityTypeGeneratorTests : GeneratorTestBase
    {
        private static string NormalizeCode(string code) =>
            Regex.Replace(code.Replace("\r\n", "\n"), @"[ \t]+\n", "\n");

        [TestMethod]
        public void Generate_returns_code()
        {
            DefaultVBEntityTypeGenerator generator = new DefaultVBEntityTypeGenerator();
            var result = NormalizeCode(generator.Generate(
                Model.ConceptualModel.Container.EntitySets.First(),
                Model,
                "WebApplication1.Models"));

            result.Should().Be(NormalizeCode(
                @"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Data.Entity.Spatial

Partial Public Class Entity
    Public Property Id As Integer
End Class
"));
        }
    }
}
