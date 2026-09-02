// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{
    internal class StoreGeneratedPatternConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            AddMapping(ModelConstants.StoreGeneratedPattern_None, ModelConstants.StoreGeneratedPattern_None);
            AddMapping(ModelConstants.StoreGeneratedPattern_Identity, ModelConstants.StoreGeneratedPattern_Identity);
            AddMapping(ModelConstants.StoreGeneratedPattern_Computed, ModelConstants.StoreGeneratedPattern_Computed);
        }
    }
}
