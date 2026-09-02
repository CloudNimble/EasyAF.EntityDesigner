// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Editors;
using System.ComponentModel;
using System.Drawing.Design;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    [TypeConverter(typeof(ReferentialConstraintConverter))]
    [Editor(typeof(ReferentialConstraintEditor), typeof(UITypeEditor))]
    internal class ReferentialConstraintProperty
    {
        public ReferentialConstraintProperty()
        {
        }
    }

}
