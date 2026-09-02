// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Edmx;
using System;

namespace Microsoft.Data.Entity.Design.Diagrams.Rendering.Headless
{

    /// <summary>
    ///     Loads an EDMX file into a fully populated, laid out and routed <see cref="EntityDesignerSurface" /> without
    ///     a Visual Studio shell.
    /// </summary>
    /// <remarks>
    ///     This replaces <c>MicrosoftDataEntityDesignSerializationHelper.LoadModel</c>, which cannot be reused because
    ///     it resolves a doc data through <c>PackageManager.Package</c> and an editing context through
    ///     <c>DocumentFrameMgr</c>. The pieces it delegates to are shell free, so the sequence is reassembled here from
    ///     <c>VanillaXmlModelProvider</c>, <c>EFArtifactFactory</c> and
    ///     <c>EntityModelToDslModelTranslatorStrategy</c>.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     using (var loaded = EdmxDiagramLoader.Load(@"C:\Models\Northwind.edmx"))
    ///     {
    ///         var svg = new SvgExporter().GenerateSvg(loaded.Diagram);
    ///     }
    ///     </code>
    /// </example>
    internal sealed class LoadedDiagram : IDisposable
    {
        private readonly EntityDesignModelManager _modelManager;
        private readonly HeadlessDiagramStore _store;
        private bool _isDisposed;

        /// <summary>
        ///     Gets the artifact the diagram was built from.
        /// </summary>
        public EntityDesignArtifact Artifact { get; }

        /// <summary>
        ///     Gets the populated, routed diagram.
        /// </summary>
        public EntityDesignerSurface Diagram { get; }

        /// <summary>
        ///     Gets the name of the diagram that was rendered.
        /// </summary>
        public string DiagramName { get; }

        /// <summary>
        ///     Creates a result holding the diagram and the resources that must outlive it.
        /// </summary>
        internal LoadedDiagram(
            EntityDesignArtifact artifact, EntityDesignModelManager modelManager, HeadlessDiagramStore store,
            EntityDesignerSurface diagram, string diagramName)
        {
            Artifact = artifact;
            _modelManager = modelManager;
            _store = store;
            Diagram = diagram;
            DiagramName = diagramName;
        }

        /// <summary>
        ///     Releases the store and the model manager.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _store?.Dispose();
            _modelManager?.Dispose();
        }
    }

}
