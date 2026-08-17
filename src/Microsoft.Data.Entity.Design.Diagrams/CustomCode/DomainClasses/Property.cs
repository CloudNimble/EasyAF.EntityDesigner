// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.VisualStudio.Modeling.Diagrams;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Data.Entity.Design.Diagrams.ViewModel
{
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords")]
    internal partial class Property
    {
        protected override void OnDeleting()
        {
            base.OnDeleting();

            if (EntityType != null
                && EntityType.EntityDesignerViewModel != null
                && EntityType.EntityDesignerViewModel.Reloading == false)
            {
                var viewModel = EntityType.EntityDesignerViewModel;
                var tx = ModelUtils.GetCurrentTx(Store);
                Debug.Assert(tx != null);
                if (tx != null
                    && !tx.IsSerializing)
                {
                    // deleting the property would select the Diagram, select parent Entity instead
                    var diagram = viewModel.GetDiagram();
                    if (diagram != null
                        && diagram.ActiveDiagramView != null)
                    {
                        var shape = diagram.FindShape(EntityType);
                        if (shape != null)
                        {
                            diagram.ActiveDiagramView.Selection.Set(new DiagramItem(shape));
                        }
                    }

                    ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new PropertyDelete(this));
                }
            }
        }
    }
}
