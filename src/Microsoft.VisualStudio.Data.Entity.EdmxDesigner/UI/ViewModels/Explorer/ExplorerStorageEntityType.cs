// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Explorer
{
    // ExplorerConceptualEntityType must be distinguished from 
    // ExplorerStorageEntityType in order to allow the XAML
    // to load different images for them
    internal class ExplorerStorageEntityType : ExplorerEntityType
    {
        public ExplorerStorageEntityType(EditingContext context, EntityType entityType, ExplorerEFElement parent)
            : base(context, entityType, parent)
        {
            // do nothing
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "TablePngIcon"; }
        }
    }
}
