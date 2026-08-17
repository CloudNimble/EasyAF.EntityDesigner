// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.XmlEngine.Common;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class CodeIdentifierUtilsTests
    {
        [TestMethod]
        public void IsValidIdentifier_returns_true_for_valid_identifier()
        {
            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).IsValidIdentifier("@abc").Should().BeTrue();
            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).IsValidIdentifier("_abc").Should().BeTrue();
            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.VisualBasic).IsValidIdentifier("abc1").Should().BeTrue();
        }

        [TestMethod]
        public void IsValidIdentifier_returns_false_for_invalid_identifier()
        {
            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).IsValidIdentifier("class").Should().BeFalse();
            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).IsValidIdentifier("abc.def").Should().BeFalse();
            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.VisualBasic).IsValidIdentifier("a bc").Should().BeFalse();
            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.VisualBasic).IsValidIdentifier("3abc").Should().BeFalse();
        }

        [TestMethod]
        public void IsValidIdentifier_returns_false_if_identifier_not_valid_for_CSharp_and_VB_for_Website_project()
        {
            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.VisualBasic).IsValidIdentifier("@abc").Should().BeFalse();
        }

        [TestMethod]
        public void CreateValidIdentifier_returns_identifier_if_already_valid()
        {
            const string identifier = "testId";

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier(identifier).Should().BeSameAs(identifier);

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier(identifier).Should().BeSameAs(identifier);

            new CodeIdentifierUtils(VisualStudioProjectSystem.WindowsApplication, LangEnum.CSharp).CreateValidIdentifier(identifier).Should().BeSameAs(identifier);

            new CodeIdentifierUtils(VisualStudioProjectSystem.WindowsApplication, LangEnum.VisualBasic).CreateValidIdentifier(identifier).Should().BeSameAs(identifier);

            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.CSharp).CreateValidIdentifier(identifier).Should().BeSameAs(identifier);

            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.VisualBasic).CreateValidIdentifier(identifier).Should().BeSameAs(identifier);
        }

        [TestMethod]
        public void CreateValidIdentifier_creates_valid_identifiers()
        {
            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("a b").Should().Be("ab");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("a b").Should().Be("ab");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("a.b").Should().Be("ab");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("a.b").Should().Be("ab");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("for").Should().Be("_for");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("For").Should().Be("_For");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("'model'").Should().Be("model");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("'model'").Should().Be("model");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("@class").Should().Be("@class");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("@class").Should().Be("_class");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("123").Should().Be("_123");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("123").Should().Be("_123");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("_").Should().Be("_");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("_").Should().Be("_");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.CSharp).CreateValidIdentifier("...").Should().Be("_");

            new CodeIdentifierUtils(VisualStudioProjectSystem.WebApplication, LangEnum.VisualBasic).CreateValidIdentifier("...").Should().Be("_");



            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.CSharp).CreateValidIdentifier("@class").Should().Be("_class");

            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.VisualBasic).CreateValidIdentifier("@Dim").Should().Be("_Dim");

            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.CSharp).CreateValidIdentifier("Dim").Should().Be("_Dim");

            new CodeIdentifierUtils(VisualStudioProjectSystem.Website, LangEnum.VisualBasic).CreateValidIdentifier("Dim").Should().Be("_Dim");
        }
    }
}
