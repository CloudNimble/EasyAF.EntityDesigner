// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Validation;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.CustomSerializer;

namespace Microsoft.Data.Entity.Design.Diagrams
{
    public sealed partial class MicrosoftDataEntityDesignSerializationHelper
    {
        internal override EntityDesignerViewModel LoadModelAndDiagram(
            SerializationResult serializationResult, Partition modelPartition, string modelFileName, Partition diagramPartition,
            string diagramFileName, ISchemaResolver schemaResolver, ValidationController validationController,
            ISerializerLocator serializerLocator)
        {
            // The host shows whatever progress affordance it has -- a wait cursor in Visual Studio, nothing at the
            // command line -- around its own call into the load. See specs/layer-map.md.
            var evm = LoadModel(
                serializationResult, modelPartition, modelFileName, schemaResolver, validationController, serializerLocator);
            CreateDiagramHelper(diagramPartition, evm);

            return evm;
        }

        /// <summary>
        ///     Helper method to create and initialize a new EntityDesignerSurface.
        /// </summary>
        internal override EntityDesignerSurface CreateDiagramHelper(Partition diagramPartition, ModelElement modelRoot)
        {
            EntityDesignerViewModel evm = modelRoot as EntityDesignerViewModel;
            EntityDesignerSurface diagram = new EntityDesignerSurface(diagramPartition);
            diagram.ModelElement = evm;
            return diagram;
        }

        internal override EntityDesignerViewModel LoadModel(
            SerializationResult serializationResult, Partition partition, string fileName, ISchemaResolver schemaResolver,
            ValidationController validationController, ISerializerLocator serializerLocator)
        {
            EntityDesignerViewModel evm = null;

            // The host prepares its own document buffer and hands in the editing context that identifies it,
            // because the host is what owns the document. See specs/layer-map.md.
            var context = DesignerStoreProperties.GetEditingContext(partition.Store);

            SerializationContext serializationContext = new SerializationContext(GetDirectory(partition.Store), fileName, serializationResult);
            TransactionContext transactionContext = new TransactionContext();
            transactionContext.Add(SerializationContext.TransactionContextKey, serializationContext);

            using (var t = partition.Store.TransactionManager.BeginTransaction("Load Model from " + fileName, true, transactionContext))
            {
                evm =
                    ModelTranslatorContextItem.GetEntityModelTranslator(context).TranslateModelToDslModel(null, partition) as
                    EntityDesignerViewModel;

                if (evm == null)
                {
                    serializationResult.Failed = true;
                }
                else
                {
                    if (t.IsActive)
                    {
                        t.Commit();
                    }
                }
            }

            // Validate imported model
            if (!serializationResult.Failed
                && (validationController != null))
            {
                validationController.Validate(partition, ValidationCategories.Load);
            }
            return evm;
        }

        internal override MemoryStream InternalSaveModel(
            SerializationResult serializationResult, EntityDesignerViewModel modelRoot, string fileName, Encoding encoding,
            bool writeOptionalPropertiesWithDefaultValue)
        {
            MemoryStream stream = null;

            // The document's XML is already authoritative and lives in the host's buffer, so the host hands the
            // text over rather than the designer serializing anything. Pushing it in also settles what the old
            // doc data lookup had to work around by hand: during a Save As the passed file name is the new name,
            // but the text is always this document's. See specs/layer-map.md.
            var text = DesignerStoreProperties.GetDocumentText(modelRoot.Store);
            Debug.Assert(!string.IsNullOrEmpty(text), "The host did not supply any text to save.");

            if (!string.IsNullOrEmpty(text))
            {
                stream = FileUtils.StringToStream(text, encoding) as MemoryStream;
            }

            // if we don't have a stream, then we couldn't serialize for some reason
            if (stream == null)
            {
                serializationResult.Failed = true;
            }

            return stream;
        }

        internal override void SaveDiagram(
            SerializationResult serializationResult, EntityDesignerSurface diagram, string diagramFileName, Encoding encoding,
            bool writeOptionalPropertiesWithDefaultValue)
        {
            // don't save the .diagram file
            return;
        }

        internal override void SaveModelAndDiagram(
            SerializationResult serializationResult, EntityDesignerViewModel modelRoot, string modelFileName, EntityDesignerSurface diagram,
            string diagramFileName, Encoding encoding, bool writeOptionalPropertiesWithDefaultValue)
        {
            // Only save the model. Clearing the artifact's dirty flag and saving the subordinate .diagram
            // document are the host's to do once this returns, and it does them there. See specs/layer-map.md.
            base.SaveModel(serializationResult, modelRoot, modelFileName, encoding, writeOptionalPropertiesWithDefaultValue);
        }

        /// <summary>
        ///     This method removes all of the child elements of the EntityDesignerViewModel.
        /// </summary>
        internal static void ClearModel(EntityDesignerViewModel viewModel)
        {
            // The assumption is that if ModelXRef is null or we cannot get the diagram, DSL Model is empty
            if (viewModel.ModelXRef == null
                || viewModel.GetDiagram() == null)
            {
                return;
            }

            using (var t = viewModel.Store.TransactionManager.BeginTransaction("ClearModel", true))
            {
                // delete inheritance first
                foreach (var melem in viewModel.ModelXRef.ReferencedViewElements)
                {
                    if (melem is Inheritance)
                    {
                        melem.Delete();
                    }
                }

                // delete associations next
                foreach (var melem in viewModel.ModelXRef.ReferencedViewElements)
                {
                    if (melem is Association)
                    {
                        melem.Delete();
                    }
                }

                // delete entities last
                foreach (var melem in viewModel.ModelXRef.ReferencedViewElements)
                {
                    if (melem is EntityType)
                    {
                        melem.Delete();
                    }
                }

                // clear out the XRef
                viewModel.ModelXRef.Clear();

                if (t.IsActive)
                {
                    t.Commit();
                }
            }
        }

        /// <summary>
        ///     This method will remove all child shapes of the EntityDesignerSurface.
        /// </summary>
        internal static void ClearDiagram(EntityDesignerViewModel viewModel)
        {
            var diagram = viewModel.GetDiagram();
            if (diagram != null)
            {
                using (var t = viewModel.Store.TransactionManager.BeginTransaction("ClearDiagram", true))
                {
                    // make copy of AllElements so that we don't modify the collection while we are iterating over it.
                    // We are only interested to the model elements that belong to the viewModel; so look at the Partition's ElementDirectory
                    // instead of Store's ElementDirectory which is shared across diagram.
                    Debug.Assert(viewModel.Partition != null, "ViewModel's Partition should never be null.");
                    if (viewModel.Partition != null)
                    {
                        var elementDirectory = viewModel.Partition.ElementDirectory;
                        Debug.Assert(
                            elementDirectory != null, "ElementDirectory in partition for view model :" + diagram.Title + " is null.");
                        if (elementDirectory != null)
                        {
                            List<ModelElement> allElements = new List<ModelElement>(elementDirectory.AllElements.Count);
                            allElements.AddRange(elementDirectory.AllElements);

                            foreach (var melem in allElements)
                            {
                                // don't delete our diagram, but remove every other presentation element
                                if (melem is PresentationElement
                                    && (melem is EntityDesignerSurface) == false)
                                {
                                    melem.Delete();
                                }
                            }
                        }
                    }

                    if (t.IsActive)
                    {
                        t.Commit();
                    }
                }
            }
        }

        /// <summary>
        ///     This method loads the DSL view model with the items in the artifact's C-Model.
        /// </summary>
        internal void ReloadModel(EntityDesignerViewModel viewModel)
        {
            var diagram = viewModel.GetDiagram();
            if (diagram == null)
            {
                // empty DSL diagram
                return;
            }

            // get our artifact
            EntityDesignArtifact artifact = viewModel.EditingContext?.GetEFArtifactService()?.Artifact as EntityDesignArtifact;
            Debug.Assert(artifact != null);

            SerializationResult serializationResult = new SerializationResult();

            SerializationContext serializationContext = new SerializationContext(GetDirectory(viewModel.Store), artifact.Uri.LocalPath, serializationResult);
            TransactionContext transactionContext = new TransactionContext();
            transactionContext.Add(SerializationContext.TransactionContextKey, serializationContext);

            var workaroundFixSerializationTransactionValue = false;
            if (viewModel.Store.PropertyBag.ContainsKey("WorkaroundFixSerializationTransaction"))
            {
                workaroundFixSerializationTransactionValue = (bool)viewModel.Store.PropertyBag["WorkaroundFixSerializationTransaction"];
            }

            try
            {
                // To fix performance issue during reload, we turn-off layout during "serialization".
                viewModel.Store.PropertyBag["WorkaroundFixSerializationTransaction"] = true;

                using (var t = viewModel.Store.TransactionManager.BeginTransaction("ReloadModel", true, transactionContext))
                {
                    if (artifact.ConceptualModel() == null)
                    {
                        return;
                    }

                    Edmx.Designer.Diagram diagramModel = null;

                    // If DiagramId is not string empty, try to get the diagram from the artifact. 
                    // There is a situation where we could not find the diagram given an ID (for example: EDMX Model's Diagram that is created by VS before SQL 11; 
                    // In that case, we assign temporary ID to the diagram and a new ID will be generated every time the model is reloaded.)
                    // We could safely choose the first diagram since multiple diagrams feature didn't exist in VS prior to SQL11 release.
                    if (!string.IsNullOrEmpty(diagram.DiagramId))
                    {
                        diagramModel = artifact.DesignerInfo.Diagrams.GetDiagram(diagram.DiagramId);
                    }

                    diagramModel ??= artifact.DesignerInfo.Diagrams.FirstDiagram;

                    if (diagramModel != null)
                    {
                        // Re-establish the xref between Escher conceptual model and DSL root model.
                        // and between Escher Diagram model and DSL diagram model.

                        Debug.Assert(viewModel.ModelXRef != null, "Why ModelXRef is null?");
                        if (viewModel.ModelXRef != null)
                        {
                            viewModel.ModelXRef.Add(artifact.ConceptualModel(), viewModel, viewModel.EditingContext);
                            viewModel.ModelXRef.Add(diagramModel, diagram, viewModel.EditingContext);
                            ModelTranslatorContextItem.GetEntityModelTranslator(viewModel.EditingContext)
                                .TranslateModelToDslModel(diagramModel, viewModel.Partition);
                        }
                    }

                    if (t.IsActive)
                    {
                        t.Commit();
                    }
                }
            }
            finally
            {
                viewModel.Store.PropertyBag["WorkaroundFixSerializationTransaction"] = workaroundFixSerializationTransactionValue;
            }
        }

        /// <summary>
        ///     This method reads the .diagram file and makes sure that shapes exist for every item
        ///     in the .diagram file.
        /// </summary>
        internal static void ReloadDiagram(EntityDesignerViewModel viewModel)
        {
            var diagram = viewModel.GetDiagram();
            if (diagram == null)
            {
                // Empty DSL diagram
                return;
            }

            diagram.ResetWatermark(diagram.ActiveDiagramView);

            // get our artifact
            var artifact = viewModel.EditingContext?.GetEFArtifactService()?.Artifact;
            Debug.Assert(artifact != null);
            if (!artifact.IsDesignerSafe)
            {
                return;
            }


            if (viewModel.ModelXRef.GetExisting(diagram) is Edmx.Designer.Diagram diagramModel)
            {
                // ensure that we still have all of the parts we need to re-translate from the Model
                EntityModelToDslModelTranslatorStrategy.TranslateDiagram(diagram, diagramModel);
            }
            else
            {
                // this path will usually only happen if an UMFDB extension has deleted the diagram node
                // since we don't have a diagram anymore, lay it all out and create a new one
                if (diagram.ModelElement.EntityTypes.Count < EntityDesignerSurface.IMPLICIT_AUTO_LAYOUT_CEILING)
                {
                    diagram.AutoLayoutDiagram();
                }
                EntityModelToDslModelTranslatorStrategy.CreateDefaultDiagram(viewModel.EditingContext, diagram);
            }

            // remove "Select All" selection
            diagram.ActiveDiagramView?.Selection.Set(new DiagramItem(diagram));
        }
    }
}
