// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Designer;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class CreateDiagramCommand : Command
    {
        private readonly string _name;
        private readonly Diagrams _diagrams;
        private Diagram _created;

        internal CreateDiagramCommand(string name, Diagrams diagrams)
        {
            ValidateString(name);
            Debug.Assert(diagrams != null, "diagrams is null");

            _name = name;
            _diagrams = diagrams;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // check to see if this name is unique
            if (!ModelHelper.IsUniqueName(typeof(Diagram), _diagrams, _name, true, out string msg))
            {
                throw new InvalidOperationException(msg);
            }

            Diagram diagram = new Diagram(_diagrams, null);
            diagram.Id.Value = Guid.NewGuid().ToString("N");
            diagram.LocalName.Value = _name;
            _diagrams.AddDiagram(diagram);

            XmlModelHelper.NormalizeAndResolve(diagram);

            _created = diagram;
        }

        internal Diagram Diagram
        {
            get { return _created; }
        }

        /// <summary>
        ///     This helper function will create a Diagram using default name.
        ///     NOTE: If the cpc already has an active transaction, these changes will be in that transaction
        ///     and the caller of this helper method must commit it to see these changes commited
        ///     otherwise the diagram will never be created.
        /// </summary>
        /// <param name="cpc"></param>
        /// <returns>The new ComplexType</returns>
        internal static Diagram CreateDiagramWithDefaultName(CommandProcessorContext cpc)
        {
            Debug.Assert(cpc != null, "The passed in CommandProcessorContext is null.");
            if (cpc != null)
            {
                var service = cpc.EditingContext.GetEFArtifactService();

                if (service.Artifact is not EntityDesignArtifact entityDesignArtifact
                    || entityDesignArtifact.DesignerInfo == null
                    || entityDesignArtifact.DesignerInfo.Diagrams == null)
                {
                    throw new CannotLocateParentItemException();
                }

                var diagramName = ModelHelper.GetUniqueNameWithNumber(
                    typeof(Diagram), entityDesignArtifact.DesignerInfo.Diagrams, Resources.Model_DefaultDiagramName);

                // go create it
                CommandProcessor cp = new CommandProcessor(cpc);
                CreateDiagramCommand cmd = new CreateDiagramCommand(diagramName, entityDesignArtifact.DesignerInfo.Diagrams);
                cp.EnqueueCommand(cmd);
                cp.Invoke();
                return cmd.Diagram;
            }
            return null;
        }
    }
}
