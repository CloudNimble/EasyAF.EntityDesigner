// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns
{

    // <summary>
    //     Used to override conversion for the case where the ParameterColumn is representing
    //     a MappingResultBinding
    // </summary>
    internal class NoDropDownParameterColumnConverter : ParameterColumnConverter
    {
        // needs to return false so as to provide ordinary Textbox for editing
        // (instead of drop-down) see TreeGridDesignerTreeControl.CreateTypeEditorHost()
        public override bool /* TypeConverter */ GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }
    }

}
