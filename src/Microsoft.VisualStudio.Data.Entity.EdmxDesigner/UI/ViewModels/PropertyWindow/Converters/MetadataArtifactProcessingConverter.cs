// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
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
                        ConnectionDesignerInfo.MAP_CopyToOutputDirectory, EdmxDesignerResources.PropertyWindow_DisplayName_MAP_CopyToOutputDirectory);
                }
            }
            else
            {
                AddMapping(ConnectionDesignerInfo.MAP_CopyToOutputDirectory, EdmxDesignerResources.PropertyWindow_DisplayName_MAP_CopyToOutputDirectory);
            }
            AddMapping(ConnectionDesignerInfo.MAP_EmbedInOutputAssembly, EdmxDesignerResources.PropertyWindow_DisplayName_MAP_EmbedInOutputAssembly);
        }
    }
}