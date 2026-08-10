// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal abstract class ExplorerAssociation : EntityDesignExplorerEFElement
    {
        public ExplorerAssociation(EditingContext context, Association assoc, ExplorerEFElement parent)
            : base(context, assoc, parent)
        {
            // do nothing
        }

        protected override void InsertChild(EFElement efElementToInsert)
        {
            if (efElementToInsert is AssociationEnd assocEnd)
            {
                // the ViewModel does not keep track of AssociationEnd elements 
                // but it is not an error - so just return
                return;
            }

            if (efElementToInsert is ReferentialConstraint refConstraint)
            {
                // the ViewModel does not keep track of ReferentialConstraint elements 
                // but it is not an error - so just return
                return;
            }

            base.InsertChild(efElementToInsert);
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
