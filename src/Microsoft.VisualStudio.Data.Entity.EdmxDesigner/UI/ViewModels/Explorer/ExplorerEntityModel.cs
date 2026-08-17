// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Explorer
{
    internal abstract class ExplorerEntityModel : EntityDesignExplorerEFElement
    {
        // Ghost nodes are grouping nodes in the EDM Browser which 
        // do not correspond to any underlying element in the model
        protected ExplorerTypes _typesGhostNode;
        protected ExplorerAssociations _assocsGhostNode;

        protected TypedChildList<ExplorerEntityContainer> _entityContainers =
            new TypedChildList<ExplorerEntityContainer>();

        protected ExplorerEntityModel(EditingContext context, BaseEntityModel entityModel, ExplorerEFElement parent)
            : base(context, entityModel, parent)
        {
        }

        internal override void OnModelPropertyChanged(string modelPropName)
        {
            // override OnModelPropertyChanged to respond to Namespace
            // name changes as well
            if (BaseEntityModel.AttributeNamespace == modelPropName)
            {
                OnPropertyChanged("Name");
            }

            base.OnModelPropertyChanged(modelPropName);
        }

        public ExplorerTypes Types
        {
            get { return _typesGhostNode; }
        }

        public ExplorerAssociations Associations
        {
            get { return _assocsGhostNode; }
        }

        public IList<ExplorerEntityContainer> EntityContainers
        {
            get { return _entityContainers.ChildList; }
        }
    }
}
