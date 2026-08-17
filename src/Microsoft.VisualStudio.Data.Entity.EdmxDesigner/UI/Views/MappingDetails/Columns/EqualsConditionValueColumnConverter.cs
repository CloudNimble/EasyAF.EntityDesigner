// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns
{

    // <summary>
    //     Used to override conversion for the case where the ValueColumn is representing
    //     the value for a MappingCondition with Operation '='
    // </summary>
    internal class EqualsConditionValueColumnConverter : ValueColumnConverter
    {
        // needs to return false so as to provide ordinary Textbox for editing
        // see TreeGridDesignerTreeControl.CreateTypeEditorHost()
        public override bool /* TypeConverter */ GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }
    }

}
