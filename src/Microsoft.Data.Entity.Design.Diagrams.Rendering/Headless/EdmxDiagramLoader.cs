// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Entity.Design.Diagrams.CustomSerializer;
using Microsoft.Data.Entity.Design.Diagrams.Layout;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using EngineUtils = Microsoft.Data.Entity.Design.XmlEngine.Util.Utils;

namespace Microsoft.Data.Entity.Design.Diagrams.Rendering.Headless
{

    /// <summary>
    ///     Builds <see cref="EntityDesignerSurface" /> instances from EDMX files with no Visual Studio present.
    /// </summary>
    internal static class EdmxDiagramLoader
    {
        /// <summary>
        ///     Loads an EDMX file and returns a diagram whose shapes are positioned from the file's Diagrams section
        ///     and whose connectors have been routed.
        /// </summary>
        /// <param name="edmxFilePath">Full path of the .edmx file.</param>
        /// <param name="diagramName">
        ///     Name of the diagram to render. When null or empty the first diagram in the file is used.
        /// </param>
        /// <param name="layoutManager">
        ///     The layout engines to hand the surface, from the host's container. When null the surface will not
        ///     lay anything out, which only matters for a model whose shapes have no saved positions.
        /// </param>
        /// <returns>The loaded diagram. Dispose it to release the underlying store and model manager.</returns>
        /// <exception cref="FileNotFoundException"><paramref name="edmxFilePath" /> does not exist.</exception>
        /// <exception cref="InvalidOperationException">
        ///     The file could not be loaded as an Entity Designer artifact, is not designer safe, or does not contain
        ///     the requested diagram.
        /// </exception>
        public static LoadedDiagram Load(
            string edmxFilePath, string diagramName = null, LayoutEngineManager layoutManager = null)
        {
            if (string.IsNullOrWhiteSpace(edmxFilePath))
            {
                throw new ArgumentException("An EDMX file path is required.", nameof(edmxFilePath));
            }

            if (!File.Exists(edmxFilePath))
            {
                throw new FileNotFoundException("EDMX file not found.", edmxFilePath);
            }

            var modelManager = new EntityDesignModelManager(new RenderArtifactFactory(), new EFArtifactSetFactory());
            HeadlessDiagramStore store = null;

            try
            {
                var artifact = LoadArtifact(modelManager, edmxFilePath);
                var modelDiagram = ResolveDiagram(artifact, diagramName);

                var context = new EditingContext();
                context.SetEFArtifactService(new EFArtifactService(artifact));

                store = new HeadlessDiagramStore();

                // The translator creates the view model itself and keys it off the conceptual model, so the diagram
                // cannot be attached until afterwards.
                EntityDesignerViewModel viewModel = null;
                store.Transact(
                    "Translate model",
                    () => viewModel = new EntityModelToDslModelTranslatorStrategy(context)
                        .TranslateModelToDslModel(modelDiagram, store.ModelPartition) as EntityDesignerViewModel);

                if (viewModel is null)
                {
                    throw new InvalidOperationException(
                        $"The model in '{edmxFilePath}' could not be translated into a designer view model.");
                }

                var diagram = store.AttachDiagram(viewModel);

                // TranslateDiagram below asks for a layout for any shape the EDMX has no position for, so the
                // host's engines have to be in place before it runs.
                diagram.LayoutManager = layoutManager;

                // Fixup normally runs as elements are added, but the diagram did not exist yet at that point, so the
                // shapes and connectors are created explicitly here instead.
                store.Transact("Create shapes", () => CreateShapes(viewModel));

                // Applies PointX, PointY, Width, IsExpanded and FillColor from the EDMX, and routes the connectors.
                // It opens its own transaction.
                EntityModelToDslModelTranslatorStrategy.TranslateDiagram(diagram, modelDiagram);

                return new LoadedDiagram(
                    artifact, modelManager, store, diagram, modelDiagram.Name.Value ?? string.Empty);
            }
            catch
            {
                // Disposing a half built store can throw on its own - EntityDesignerViewModel.UnregisterEventDelegates
                // assumes it was fully initialised. Letting that surface would replace the real failure with a
                // NullReferenceException from the cleanup path.
                TryDispose(store);
                TryDispose(modelManager);

                throw;
            }
        }

        /// <summary>
        ///     Disposes <paramref name="disposable" />, swallowing anything it throws.
        /// </summary>
        /// <param name="disposable">The object to dispose. May be null.</param>
        /// <remarks>
        ///     Only for use on a failure path, where an exception from cleanup would hide the failure being reported.
        /// </remarks>
        private static void TryDispose(IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch
            {
                // Intentionally ignored - the caller is already throwing.
            }
        }

        /// <summary>
        ///     Asks diagram fixup to create a shape or connector for every element in the view model.
        /// </summary>
        /// <param name="viewModel">The translated view model.</param>
        /// <remarks>
        ///     Entities are fixed up before relationships so that a connector always finds both of its end shapes.
        /// </remarks>
        private static void CreateShapes(EntityDesignerViewModel viewModel)
        {
            foreach (var entityType in viewModel.EntityTypes)
            {
                Diagram.FixUpDiagram(viewModel, entityType);
            }

            foreach (var link in GetRelationships(viewModel))
            {
                Diagram.FixUpDiagram(viewModel, link);
            }
        }

        /// <summary>
        ///     Gets the association and inheritance links between the entities in the view model.
        /// </summary>
        /// <remarks>
        ///     Read from the partition rather than walked from the entities: the links are what fixup needs, and the
        ///     element directory returns each one exactly once.
        /// </remarks>
        private static IEnumerable<ModelElement> GetRelationships(EntityDesignerViewModel viewModel)
        {
            var directory = viewModel.Partition.ElementDirectory;

            return directory.FindElements<Association>()
                .Cast<ModelElement>()
                .Concat(directory.FindElements<Inheritance>());
        }

        /// <summary>
        ///     Loads the EDMX into an artifact and confirms the designer can render it.
        /// </summary>
        private static EntityDesignArtifact LoadArtifact(EntityDesignModelManager modelManager, string edmxFilePath)
        {
            var uri = EngineUtils.FileName2Uri(edmxFilePath);

            if (!(modelManager.GetNewOrExistingArtifact(uri, new VanillaXmlModelProvider()) is EntityDesignArtifact artifact))
            {
                throw new InvalidOperationException($"'{edmxFilePath}' could not be loaded as an Entity Designer model.");
            }

            // Deliberately not DetermineIfArtifactIsDesignerSafe - that requires the model's ADO.NET provider to be
            // registered on this machine, which rendering does not need. See the remarks on the method below.
            if (!artifact.DetermineIfArtifactIsRenderSafe())
            {
                throw new InvalidOperationException(
                    $"'{edmxFilePath}' cannot be rendered: it has no conceptual model, or its XML namespaces do not "
                    + "match its schema version.");
            }

            return artifact;
        }

        /// <summary>
        ///     Finds the requested diagram in the artifact, or the first one when no name is supplied.
        /// </summary>
        private static Edmx.Designer.Diagram ResolveDiagram(EntityDesignArtifact artifact, string diagramName)
        {
            // When the diagrams have been moved into a .edmx.diagram file, the EDMX keeps an empty Diagrams node and
            // the real content lives on the diagram artifact.
            var diagrams = artifact.DiagramArtifact?.DesignerInfo()?.Diagrams ?? artifact.DesignerInfo()?.Diagrams;

            if (diagrams is null
                || diagrams.FirstDiagram is null)
            {
                throw new InvalidOperationException("The model does not contain any diagrams.");
            }

            if (string.IsNullOrWhiteSpace(diagramName))
            {
                return diagrams.FirstDiagram;
            }

            var match = diagrams.Items.FirstOrDefault(
                d => string.Equals(d.Name.Value, diagramName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                var available = string.Join(", ", diagrams.Items.Select(d => d.Name.Value));

                throw new InvalidOperationException(
                    $"The model does not contain a diagram named '{diagramName}'. Available diagrams: {available}");
            }

            return match;
        }
    }

}
