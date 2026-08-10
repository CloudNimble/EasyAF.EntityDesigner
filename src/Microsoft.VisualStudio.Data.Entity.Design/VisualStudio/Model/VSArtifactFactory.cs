// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Tools.XmlDesignerBase.Model;

namespace Microsoft.Data.Entity.Design.VisualStudio.Model
{
    internal class VSArtifactFactory : IEFArtifactFactory
    {
        // <summary>
        //     The factory that creates VSArtifact and the corresponding DiagramArtifact if diagram file is available.
        // </summary>
        public IList<EFArtifact> Create(ModelManager modelManager, Uri uri, XmlModelProvider xmlModelProvider)
        {
            VSArtifact artifact = new VSArtifact(modelManager, uri, xmlModelProvider);

            List<EFArtifact> artifacts = new List<EFArtifact> { artifact };

            var diagramArtifact = GetDiagramArtifactIfAvailable(modelManager, uri, xmlModelProvider);
            if (diagramArtifact != null)
            {
                artifact.DiagramArtifact = diagramArtifact;
                artifacts.Add(diagramArtifact);
            }

            return artifacts;
        }

        private static DiagramArtifact GetDiagramArtifactIfAvailable(
            ModelManager modelManager, Uri modelUri, XmlModelProvider xmlModelProvider)
        {
            var diagramFileName = modelUri.OriginalString + EntityDesignArtifact.ExtensionDiagram;
            return File.Exists(diagramFileName)
                       ? new VSDiagramArtifact(modelManager, new Uri(diagramFileName), xmlModelProvider)
                       : null;
        }
    }
}
