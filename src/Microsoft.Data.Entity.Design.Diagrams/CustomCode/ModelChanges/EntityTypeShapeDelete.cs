// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal class EntityTypeShapeDelete : EntityTypeShapeModelChange
    {
        internal EntityTypeShapeDelete(EntityTypeShape entityShape)
            : base(entityShape)
        {
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = EntityTypeShape.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from entity type shape: " + EntityTypeShape.AccessibleName);
            if (viewModel != null)
            {
                if (viewModel.ModelXRef.GetExisting(EntityTypeShape) is Edmx.Designer.EntityTypeShape modelEntityShape)
                {
                    DeleteEFElementCommand.DeleteInTransaction(cpc, modelEntityShape);
                    viewModel.ModelXRef.Remove(modelEntityShape, EntityTypeShape);
                }
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 0; }
        }
    }
}
