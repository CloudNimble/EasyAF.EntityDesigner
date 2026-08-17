// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.XmlEngine.UI.Controls;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class MultiplicityListConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            AddMapping(ModelConstants.Multiplicity_Many, Resources.PropertyWindow_Value_MultiplicityMany);
            AddMapping(ModelConstants.Multiplicity_One, Resources.PropertyWindow_Value_MultiplicityOne);
            AddMapping(ModelConstants.Multiplicity_ZeroOrOne, Resources.PropertyWindow_Value_MultiplicityZeroOrOne);
        }
    }
}