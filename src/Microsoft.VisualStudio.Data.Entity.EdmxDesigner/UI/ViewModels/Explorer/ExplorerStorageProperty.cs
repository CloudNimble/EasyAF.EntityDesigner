// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Explorer
{
    internal class ExplorerStorageProperty : ExplorerProperty
    {
        public ExplorerStorageProperty(EditingContext context, Property property, ExplorerEFElement parent)
            : base(context, property, parent)
        {
            // do nothing
        }

        internal override string ExplorerImageResourceKeyName
        {
            get
            {
                if (IsKeyProperty)
                {
                    return "TableKeyColumnPngIcon";
                }
                else
                {
                    return "TableColumnPngIcon";
                }
            }
        }
    }
}
