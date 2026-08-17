// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class GetterSetterConverter : AccessConverter
    {
        protected override void PopulateMapping()
        {
            base.PopulateMapping();
            AddMapping(ModelConstants.CodeGenerationAccessProtected, ModelConstants.CodeGenerationAccessProtected);
            AddMapping(ModelConstants.CodeGenerationAccessPrivate, ModelConstants.CodeGenerationAccessPrivate);
        }
    }
}
