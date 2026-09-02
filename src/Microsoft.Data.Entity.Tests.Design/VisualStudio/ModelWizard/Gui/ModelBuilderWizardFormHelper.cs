// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Gui;
using Moq;
using Moq.Protected;
using System;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Gui
{
    internal class ModelBuilderWizardFormHelper
    {
        public static ModelBuilderWizardForm CreateWizard(ModelGenerationOption generationOption = (ModelGenerationOption)(-1),
            Project project = null, string modelPath = null, IServiceProvider serviceProvider = null)
        {
            ModelBuilderSettings modelBuilderSettings =
                new ModelBuilderSettings
                {
                    Project = project ?? MockDTE.CreateProject(),
                    GenerationOption = generationOption,
                    ModelPath = modelPath
                };

            return CreateWizard(modelBuilderSettings, serviceProvider);
        }

        public static ModelBuilderWizardForm CreateWizard(ModelBuilderSettings modelBuilderSettings, IServiceProvider serviceProvider = null)
        {
            Mock<ModelBuilderWizardForm> mockWizard = new Mock<ModelBuilderWizardForm>(
                serviceProvider ?? Mock.Of<IServiceProvider>(),
                modelBuilderSettings,
                ModelBuilderWizardForm.WizardMode.PerformAllFunctionality)
            {
                CallBase = true
            };

            mockWizard.Protected().Setup("InitializeWizardPages");

            return mockWizard.Object;
        }
    }
}
