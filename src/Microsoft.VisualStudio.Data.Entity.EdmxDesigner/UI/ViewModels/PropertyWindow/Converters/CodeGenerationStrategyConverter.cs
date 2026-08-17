// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{
    internal class CodeGenerationStrategyConverter : DynamicListConverter<string, EFEntityModelDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(EFEntityModelDescriptor selectedObject)
        {
            AddMapping(EdmxDesignerResources.None, EdmxDesignerResources.CodeGenerationStrategy_T4);
            AddMapping(EdmxDesignerResources.Default, EdmxDesignerResources.CodeGenerationStrategy_LegacyObjectContext);
        }
    }
}