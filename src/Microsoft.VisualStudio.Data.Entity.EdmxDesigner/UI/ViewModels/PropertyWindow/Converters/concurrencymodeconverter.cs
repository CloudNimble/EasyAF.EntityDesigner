// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.XmlEngine.UI.Controls;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
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
