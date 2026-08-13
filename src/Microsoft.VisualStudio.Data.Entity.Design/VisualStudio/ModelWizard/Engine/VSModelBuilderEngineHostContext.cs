// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    internal class VSModelBuilderEngineHostContext : ModelBuilderEngineHostContext
    {
        private readonly ModelBuilderSettings _settings;

        internal VSModelBuilderEngineHostContext(ModelBuilderSettings settings)
        {
            _settings = settings;
        }

        internal override void LogMessage(string statusMessage)
        {
            VsUtils.LogOutputWindowPaneMessage(_settings.Project, statusMessage);
        }

        internal override void DispatchToModelGenerationExtensions()
        {
            var edmx = ((EdmxModelBuilderEngine)_settings.ModelBuilderEngine).Edmx;

            XDocument fromDBDocument = new XDocument(edmx);
            ModelGenerationExtensionDispatcher dispatcher =
                new ModelGenerationExtensionDispatcher(
                    _settings.WizardKind,
                    fromDBDocument,
                    edmx,
                    _settings.Project);

            dispatcher.Dispatch();
            _settings.HasExtensionChangedModel = dispatcher.HasCurrentChanged;
        }
    }
}
