// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors;
using System;
using System.Resources;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class LocCategoryAttribute : CommonLocCategoryAttribute
    {
        public LocCategoryAttribute(string category)
            : base(category)
        {
        }

        protected override ResourceManager ResourceManager
        {
            get { return EdmxDesignerResources.ResourceManager; }
        }
    }

}