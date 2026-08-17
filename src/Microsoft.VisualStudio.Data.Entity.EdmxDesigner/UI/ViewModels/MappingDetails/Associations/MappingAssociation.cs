// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Branches;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Associations
{
    [TreeGridDesignerRootBranch(typeof(AssociationBranch))]
    [TreeGridDesignerColumn(typeof(PropertyColumn), Order = 1)]
    [TreeGridDesignerColumn(typeof(OperatorColumn), Order = 2)]
    [TreeGridDesignerColumn(typeof(ColumnNameColumn), Order = 3)]
    internal class MappingAssociation : MappingAssociationMappingRoot
    {
        private MappingAssociationSet _assocSet;

        public MappingAssociation(EditingContext context, Association assoc, MappingEFElement parent)
            : base(context, assoc, parent)
        {
            Debug.Assert(assoc != null, "MappingAssociation cannot accept a null Association");
            Debug.Assert(
                assoc.AssociationSet != null,
                "MappingAssociation cannot accept an Association " + assoc.ToPrettyString() + " with a null AssociationSet");
        }

        internal Association Association
        {
            get { return ModelItem as Association; }
        }

        protected override void LoadChildrenCollection()
        {
            _assocSet = ModelToMappingModelXRef.GetNewOrExisting(_context, Association.AssociationSet, this) as MappingAssociationSet;
            _children.Add(_assocSet);
        }

        protected override void OnChildDeleted(MappingEFElement melem)
        {
            if (_assocSet == melem)
            {
                _assocSet = null;
            }
        }
    }
}
