// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard
{
    using Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class ModelObjectItemWizardTests
    {
        [TestMethod]
        public void ShouldAddProjectItem_returns_true_for_ModelFirst()
        {
            (new ModelObjectItemWizard(
                    new ModelBuilderSettings { GenerationOption = ModelGenerationOption.EmptyModel })
                    .ShouldAddProjectItem("FakeProjectItemName"));
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_true_for_DatabaseFirst()
        {
            (new ModelObjectItemWizard(
                    new ModelBuilderSettings { GenerationOption = ModelGenerationOption.GenerateFromDatabase })
                    .ShouldAddProjectItem("FakeProjectItemName"));
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_false_for_EmptyModelCodeFirst()
        {
            Assert.False(
                new ModelObjectItemWizard(
                    new ModelBuilderSettings { GenerationOption = ModelGenerationOption.EmptyModelCodeFirst })
                    .ShouldAddProjectItem("FakeProjectItemName"));
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_false_for_CodeFirstFromDatabase()
        {
            Assert.False(
                new ModelObjectItemWizard(
                    new ModelBuilderSettings { GenerationOption = ModelGenerationOption.CodeFirstFromDatabase })
                    .ShouldAddProjectItem("FakeProjectItemName"));
        }
    }
}
