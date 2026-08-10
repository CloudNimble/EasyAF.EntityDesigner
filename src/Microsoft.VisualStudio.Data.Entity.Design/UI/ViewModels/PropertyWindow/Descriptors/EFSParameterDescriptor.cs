// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFSParameterDescriptor : EFAnnotatableElementDescriptor<Parameter>
    {
        internal override bool IsReadOnlyName()
        {
            return true;
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_Type")]
        public string Type
        {
            get { return TypedEFElement.Type.Value; }
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_Mode")]
        public string Mode
        {
            get { return TypedEFElement.Mode.Value; }
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "Parameter";
        }
    }
}
