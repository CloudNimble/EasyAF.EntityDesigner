// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Modeling;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{

    [RuleOn(typeof(EntityTypeShape), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class EntityTypeShape_AddRule : AddRule
    {
        public override void ElementAdded(ElementAddedEventArgs e)
        {
            EntityTypeShape entityShape = e.ModelElement as EntityTypeShape;
            Debug.Assert(entityShape != null, "entityShape != null");

            var tx = ModelUtils.GetCurrentTx(entityShape.Store);
            Debug.Assert(tx != null, "tx != null");
            if (tx != null
                && !tx.IsSerializing)
            {
                ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new EntityTypeShapeAdd(entityShape));
            }
        }
    }

}
