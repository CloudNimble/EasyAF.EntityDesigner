// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFSEntityContainerDescriptor : EFAnnotatableElementDescriptor<StorageEntityContainer>
    {
        internal override bool IsReadOnlyName()
        {
            return true;
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "StorageEntityContainer";
        }
    }
}
