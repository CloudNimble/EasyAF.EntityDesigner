// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Explorer
{
    // ExplorerConceptualEntityType must be distinguished from 
    // ExplorerStorageEntityType in order to allow the XAML
    // to load different images for them
    internal class ExplorerConceptualEntityType : ExplorerEntityType
    {
        public ExplorerConceptualEntityType(EditingContext context, EntityType entityType, ExplorerEFElement parent)
            : base(context, entityType, parent)
        {
            // do nothing
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "EntityTypePngIcon"; }
        }

        // the name of Conceptual Entity Types are editable inline in the Explorer
        public override bool IsEditableInline
        {
            get { return true; }
        }

        internal override void OnModelPropertyChanged(string modelPropName)
        {
            base.OnModelPropertyChanged(modelPropName);

            ModelToExplorerModelXRef xref = ModelToExplorerModelXRef.GetModelToBrowserModelXRef(_context);

            if (modelPropName == EFNameableItem.AttributeName)
            {
                // This code below makes sure that if ExplorerConceptualEntityType's name is changed we need to ensure the corresponding ExplorerEntityTypeShape's name is also updated.
                // TODO: review the code below see if we can create a more generic code in ExplorerViewModelHelper's ProcessModelChangesCommitted.
                EntityType entityType = ModelItem as EntityType;
                foreach (var ets in entityType.GetAntiDependenciesOfType<EntityTypeShape>())
                {
                    var exploreEFElement = xref.GetExisting(ets);
                    exploreEFElement?.OnModelPropertyChanged(modelPropName);
                }
            }
        }
    }
}
