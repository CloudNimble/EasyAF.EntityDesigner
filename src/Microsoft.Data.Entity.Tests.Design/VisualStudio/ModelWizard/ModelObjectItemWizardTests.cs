// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard
{
    [TestClass]
    public class ModelObjectItemWizardTests
    {
        [TestMethod]
        public void ShouldAddProjectItem_returns_true_for_ModelFirst()
        {
            new ModelObjectItemWizard(
                new ModelBuilderSettings { GenerationOption = ModelGenerationOption.EmptyModel })
                .ShouldAddProjectItem("FakeProjectItemName").Should().BeTrue();
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_true_for_DatabaseFirst()
        {
            new ModelObjectItemWizard(
                new ModelBuilderSettings { GenerationOption = ModelGenerationOption.GenerateFromDatabase })
                .ShouldAddProjectItem("FakeProjectItemName").Should().BeTrue();
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_false_for_EmptyModelCodeFirst()
        {
            new ModelObjectItemWizard(
                new ModelBuilderSettings { GenerationOption = ModelGenerationOption.EmptyModelCodeFirst })
                .ShouldAddProjectItem("FakeProjectItemName").Should().BeFalse();
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_false_for_CodeFirstFromDatabase()
        {
            new ModelObjectItemWizard(
                new ModelBuilderSettings { GenerationOption = ModelGenerationOption.CodeFirstFromDatabase })
                .ShouldAddProjectItem("FakeProjectItemName").Should().BeFalse();
        }
    }
}
