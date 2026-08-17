// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;
using System.Globalization;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Explorer
{
    internal abstract class ExplorerEntityContainer : EntityDesignExplorerEFElement
    {
        // Ghost nodes are grouping nodes in the EDM Browser which 
        // do not correspond to any underlying element in the model
        protected ExplorerEntityContainerEntitySets _entitySetsGhostNode;
        protected ExplorerEntityContainerAssociationSets _assocSetsGhostNode;

        public ExplorerEntityContainer(
            EditingContext context,
            BaseEntityContainer entityContainer, ExplorerEFElement parent)
            : base(context, entityContainer, parent)
        {
            _entitySetsGhostNode = new ExplorerEntityContainerEntitySets(
                EdmxDesignerResources.EntitySetsGhostNodeName, context, this);
            _assocSetsGhostNode = new ExplorerEntityContainerAssociationSets(
                EdmxDesignerResources.AssociationSetsGhostNodeName, context, this);
        }

        #region Properties

        public override string Name
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    EdmxDesignerResources.EntityContainerNodeName, base.Name);
            }
        }

        #endregion

        public ExplorerEntityContainerEntitySets EntitySets
        {
            get { return _entitySetsGhostNode; }
        }

        public ExplorerEntityContainerAssociationSets AssociationSets
        {
            get { return _assocSetsGhostNode; }
        }

        protected override void LoadChildrenFromModel()
        {
            // do nothing
        }

        protected override void LoadWpfChildrenCollection()
        {
            // do nothing
        }
    }
}