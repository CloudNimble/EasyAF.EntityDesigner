// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFComplexTypeDescriptor : EFAnnotatableElementDescriptor<ComplexType>
    {
        [LocDescription("PropertyWindow_Description_ComplexTypeName")]
        public override string Name
        {
            get { return base.Name; }
            set { base.Name = value; }
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "ComplexType";
        }

        [LocCategory("PropertyWindow_Category_CodeGeneration")]
        [LocDisplayName("PropertyWindow_DisplayName_Access")]
        [LocDescription("PropertyWindow_Description_Access")]
        [TypeConverter(typeof(AccessConverter))]
        public string TypeAccess
        {
            get { return TypedEFElement.TypeAccess.Value; }
            set
            {
                var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                UpdateDefaultableValueCommand<string> cmd = new UpdateDefaultableValueCommand<string>(TypedEFElement.TypeAccess, value);
                CommandProcessor.InvokeSingleCommand(cpc, cmd);
            }
        }

        public override object GetDescriptorDefaultValue(string propertyDescriptorMethodName)
        {
            if (propertyDescriptorMethodName.Equals("TypeAccess"))
            {
                return TypedEFElement.TypeAccess.DefaultValue;
            }
            return base.GetDescriptorDefaultValue(propertyDescriptorMethodName);
        }
    }
}
