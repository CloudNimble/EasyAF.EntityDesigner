// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    internal class EFConnectionDesignerInfoDescriptor : ElementDescriptor<ConnectionDesignerInfo>, IEFConnectionDesignerDescriptorAddOn
    {
        private string _metadataArtifactProcessingDefault;

        public string MetadataArtifactProcessing
        {
            get
            {
                _metadataArtifactProcessingDefault ??= ConnectionManager.GetMetadataArtifactProcessingDefault();

                var val = _metadataArtifactProcessingDefault;
                if (TypedEFElement != null
                    && TypedEFElement.MetadataArtifactProcessingProperty != null
                    && TypedEFElement.MetadataArtifactProcessingProperty.ValueAttr != null)
                {
                    val = TypedEFElement.MetadataArtifactProcessingProperty.ValueAttr.Value;
                }
                return val;
            }
            set
            {
                var cmd = ModelHelper.CreateSetDesignerPropertyCommandInsideDesignerInfo(
                    TypedEFElement, ConnectionDesignerInfo.AttributeMetadataArtifactProcessing, value);
                if (cmd != null)
                {
                    var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        public override object GetDescriptorDefaultValue(string propertyDescriptorMethodName)
        {
            if (propertyDescriptorMethodName.Equals("MetadataArtifactProcessing"))
            {
                return ConnectionManager.GetMetadataArtifactProcessingDefault();
            }
            return base.GetDescriptorDefaultValue(propertyDescriptorMethodName);
        }
    }

}
