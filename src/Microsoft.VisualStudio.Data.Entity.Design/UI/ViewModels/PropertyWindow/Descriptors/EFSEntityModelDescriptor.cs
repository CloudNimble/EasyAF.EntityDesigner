// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFSEntityModelDescriptor : EFAnnotatableElementDescriptor<StorageEntityModel>
    {
        internal override bool IsReadOnlyName()
        {
            return true;
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_Alias")]
        public string Alias
        {
            get { return TypedEFElement.Alias.Value; }
        }

        [LocCategory("PropertyWindow_Category_General")]
        [LocDisplayName("PropertyWindow_DisplayName_Namespace")]
        public string Namespace
        {
            get { return TypedEFElement.Namespace.Value; }
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "StorageEntityModel";
        }
    }
}
