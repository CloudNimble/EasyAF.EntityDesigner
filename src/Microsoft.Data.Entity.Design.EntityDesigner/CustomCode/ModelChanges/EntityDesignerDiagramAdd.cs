// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.View;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Commands;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class EntityDesignerDiagramAdd : EntityDesignerDiagramModelChange
    {
        internal EntityDesignerDiagramAdd(EntityDesignerDiagram diagram)
            : base(diagram)
        {
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            StaticInvoke(cpc, Diagram);
        }

        internal static void StaticInvoke(CommandProcessorContext cpc, EntityDesignerDiagram diagram)
        {
            var viewModel = diagram.ModelElement;
            Debug.Assert(viewModel != null, "Why Diagram's Model Element is null?");

            if (viewModel != null)
            {
                var artifact = cpc.Artifact;
                Debug.Assert(artifact != null && artifact.DesignerInfo() != null && artifact.DesignerInfo().Diagrams != null);
                if (artifact != null
                    && artifact.DesignerInfo() != null
                    && artifact.DesignerInfo().Diagrams != null)
                {
                    var modelDiagram = CreateDiagramCommand.CreateDiagramWithDefaultName(cpc);
                    Debug.Assert(modelDiagram != null);
                    using (var t = diagram.Store.TransactionManager.BeginTransaction("Set Diagram Id", false))
                    {
                        diagram.DiagramId = modelDiagram.Id.Value;
                        t.Commit();
                    }
                    viewModel.ModelXRef.Add(modelDiagram, diagram, viewModel.EditingContext);
                }
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 140; }
        }
    }
}
