// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Tools.XmlDesignerBase.Model;

namespace Microsoft.Data.Entity.Design.Renderer.Headless
{
    /// <summary>
    ///     Creates an <see cref="EntityDesignArtifact" /> along with its sibling <see cref="DiagramArtifact" /> when
    ///     the model keeps its diagrams in a separate .edmx.diagram file.
    /// </summary>
    /// <remarks>
    ///     Neither of the existing factories fits a renderer. <c>EFArtifactFactory</c> never creates a diagram
    ///     artifact, so a model whose diagrams have been moved out appears to have none at all.
    ///     <c>VSArtifactFactory</c> does create one, but as <c>VSArtifact</c> and <c>VSDiagramArtifact</c>, which
    ///     require a running shell. This produces the shell free types and links them the same way.
    /// </remarks>
    internal sealed class RenderArtifactFactory : IEFArtifactFactory
    {
        /// <summary>
        ///     Creates the artifact for <paramref name="uri" />, plus its diagram artifact when one exists on disk.
        /// </summary>
        /// <param name="modelManager">The model manager loading the artifact.</param>
        /// <param name="uri">The URI of the EDMX file.</param>
        /// <param name="xmlModelProvider">The provider supplying the XML.</param>
        /// <returns>The artifacts that were created. The EDMX artifact is always first.</returns>
        public IList<EFArtifact> Create(ModelManager modelManager, Uri uri, XmlModelProvider xmlModelProvider)
        {
            var artifact = new EntityDesignArtifact(modelManager, uri, xmlModelProvider);
            var artifacts = new List<EFArtifact> { artifact };

            var diagramFileName = uri.OriginalString + EntityDesignArtifact.ExtensionDiagram;

            if (File.Exists(diagramFileName))
            {
                var diagramArtifact = new DiagramArtifact(modelManager, new Uri(diagramFileName), xmlModelProvider);
                artifact.DiagramArtifact = diagramArtifact;
                artifacts.Add(diagramArtifact);
            }

            return artifacts;
        }
    }
}
