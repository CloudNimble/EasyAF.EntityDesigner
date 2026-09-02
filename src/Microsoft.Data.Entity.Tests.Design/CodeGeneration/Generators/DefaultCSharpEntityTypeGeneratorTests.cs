// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Generators
{
    [TestClass]
    public class DefaultCSharpEntityTypeGeneratorTests : GeneratorTestBase
    {
        private static string NormalizeCode(string code) =>
            Regex.Replace(code.Replace("\r\n", "\n"), @"[ \t]+\n", "\n");

        [TestMethod]
        public void Generate_returns_code()
        {
            DefaultCSharpEntityTypeGenerator generator = new DefaultCSharpEntityTypeGenerator();
            var result = NormalizeCode(generator.Generate(
                Model.ConceptualModel.Container.EntitySets.First(),
                Model,
                "WebApplication1.Models"));

            result.Should().Be(NormalizeCode(
                @"namespace WebApplication1.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Entity
    {
        public int Id { get; set; }
    }
}
"));
        }
    }
}
