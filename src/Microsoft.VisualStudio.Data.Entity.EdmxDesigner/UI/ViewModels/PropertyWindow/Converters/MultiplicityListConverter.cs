// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{
    internal class MultiplicityListConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            AddMapping(ModelConstants.Multiplicity_Many, EdmxDesignerResources.PropertyWindow_Value_MultiplicityMany);
            AddMapping(ModelConstants.Multiplicity_One, EdmxDesignerResources.PropertyWindow_Value_MultiplicityOne);
            AddMapping(ModelConstants.Multiplicity_ZeroOrOne, EdmxDesignerResources.PropertyWindow_Value_MultiplicityZeroOrOne);
        }
    }
}