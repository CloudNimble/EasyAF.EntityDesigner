// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal class ExplorerAssociationSet : EntityDesignExplorerEFElement
    {
        public ExplorerAssociationSet(EditingContext context, AssociationSet assocSet, ExplorerEFElement parent)
            : base(context, assocSet, parent)
        {
            // do nothing
        }

        protected override void LoadChildrenFromModel()
        {
            // do nothing
        }

        protected override void LoadWpfChildrenCollection()
        {
            // do nothing
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "AssociationSetPngIcon"; }
        }
    }
}
