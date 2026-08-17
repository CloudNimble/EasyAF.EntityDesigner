// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.Data.Entity.Design.XmlEngine.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.Design.Ide;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class MetadataArtifactProcessingConverter : DynamicListConverter<string, EFEntityModelDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(EFEntityModelDescriptor selectedObject)
        {
            var documentPath = selectedObject.EditingContext.GetEFArtifactService().Artifact.Uri.LocalPath;
            var project = VSHelpers.GetProjectForDocument(documentPath, PackageManager.Package);
            if (project != null)
            {
                var appType = VsUtils.GetApplicationType(PackageManager.Package, project);
                if (appType != VisualStudioProjectSystem.Website)
                {
                    AddMapping(
                        ConnectionDesignerInfo.MAP_CopyToOutputDirectory, Resources.PropertyWindow_DisplayName_MAP_CopyToOutputDirectory);
                }
            }
            else
            {
                AddMapping(ConnectionDesignerInfo.MAP_CopyToOutputDirectory, Resources.PropertyWindow_DisplayName_MAP_CopyToOutputDirectory);
            }
            AddMapping(ConnectionDesignerInfo.MAP_EmbedInOutputAssembly, Resources.PropertyWindow_DisplayName_MAP_EmbedInOutputAssembly);
        }
    }
}