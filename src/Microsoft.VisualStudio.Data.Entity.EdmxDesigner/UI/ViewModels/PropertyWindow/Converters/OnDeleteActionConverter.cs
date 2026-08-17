// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{
    internal class OnDeleteActionConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            AddMapping(ModelConstants.OnDeleteAction_None, ModelConstants.OnDeleteAction_None);
            AddMapping(ModelConstants.OnDeleteAction_Cascade, ModelConstants.OnDeleteAction_Cascade);
        }
    }
}
