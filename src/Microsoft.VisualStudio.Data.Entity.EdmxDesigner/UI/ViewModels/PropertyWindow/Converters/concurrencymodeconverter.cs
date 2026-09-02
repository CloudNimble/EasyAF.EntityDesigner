// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{
    internal class ConcurrencyModeConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            AddMapping(ModelConstants.ConcurrencyModeNone, ModelConstants.ConcurrencyModeNone);
            AddMapping(ModelConstants.ConcurrencyModeFixed, ModelConstants.ConcurrencyModeFixed);
        }
    }
}
