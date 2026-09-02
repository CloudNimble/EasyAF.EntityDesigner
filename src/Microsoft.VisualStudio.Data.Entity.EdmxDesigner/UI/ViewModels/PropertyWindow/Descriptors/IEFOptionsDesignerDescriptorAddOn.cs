// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    internal interface IEFOptionsDesignerDescriptorAddOn
    {
        bool ValidateOnBuild { get; set; }
        string DDLGenerationTemplate { get; set; }
        bool PluralizeNewObjects { get; set; }
        string DatabaseSchemaName { get; set; }
        bool ProcessDependentTemplatesOnSave { get; set; }
        string CodeGenerationStrategy { get; set; }
    }

}
