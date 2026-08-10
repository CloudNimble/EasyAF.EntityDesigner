// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFEntitySetDescriptor : EFAnnotatableElementDescriptor<EntitySet>
    {
        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_EntityType")]
        public string EntityType
        {
            get { return TypedEFElement.EntityType.RefName; }
        }

        [LocCategory("PropertyWindow_Category_CodeGeneration")]
        [LocDisplayName("PropertyWindow_DisplayName_Getter")]
        [LocDescription("PropertyWindow_Description_Getter")]
        [TypeConverter(typeof(GetterSetterConverter))]
        public string GetterAccess
        {
            get
            {
                if (TypedEFElement is ConceptualEntitySet conc)
                {
                    return conc.GetterAccess.Value;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (TypedEFElement is ConceptualEntitySet conc)
                {
                    var cpc = PropertyWindowViewModelHelper.GetCommandProcessorContext();
                    UpdateDefaultableValueCommand<string> cmd = new UpdateDefaultableValueCommand<string>(conc.GetterAccess, value);
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        public bool IsBrowsableGetterAccess()
        {
            // only show this item if this is a conceptual entity set
            return TypedEFElement.EntityModel.IsCSDL;
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "EntitySet";
        }

        public override object GetDescriptorDefaultValue(string propertyDescriptorMethodName)
        {
            if (propertyDescriptorMethodName.Equals("GetterAccess"))
            {
                if (TypedEFElement is ConceptualEntitySet conc)
                {
                    return conc.GetterAccess.DefaultValue;
                }
                else
                {
                    return string.Empty;
                }
            }
            return base.GetDescriptorDefaultValue(propertyDescriptorMethodName);
        }
    }
}
