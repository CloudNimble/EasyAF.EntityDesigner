// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.XmlEngine.UI.Controls;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class EnumUnderlyingTypeConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            foreach (var primType in ModelHelper.UnderlyingEnumTypes.Select(t => t.Name))
            {
                AddMapping(primType, primType);
            }
        }
    }
}
