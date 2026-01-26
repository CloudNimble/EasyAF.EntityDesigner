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
    public class VBCodeFirstEmptyModelGeneratorTests
    {
        private static string NormalizeCode(string code) =>
            Regex.Replace(code.Replace("\r\n", "\n"), @"[ \t]+\n", "\n");

        [TestMethod]
        public void VBCodeFirstEmptyModelGeneratorTests_generates_code()
        {
            var generatedCode = NormalizeCode(new VBCodeFirstEmptyModelGenerator()
                .Generate(null, "ConsoleApplication.Data", "MyContext", "MyContextConnString"));

            var ctorComment = NormalizeCode(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.CodeFirstCodeFile_CtorComment_VB,
                    "MyContext",
                    "ConsoleApplication.Data"));

            var expected = NormalizeCode(@"Imports System
Imports System.Data.Entity
Imports System.Linq

Public Class MyContext
    Inherits DbContext

    " + ctorComment + @"
    Public Sub New()
        MyBase.New(""name=MyContextConnString"")
    End Sub

    " + NormalizeCode(Resources.CodeFirstCodeFile_DbSetComment_VB) + @"
    ' Public Overridable Property MyEntities() As DbSet(Of MyEntity)

End Class

'Public Class MyEntity
'    Public Property Id() As Int32
'    Public Property Name() As String
'End Class
");

            generatedCode.Should().Be(expected);
        }
    }
}
