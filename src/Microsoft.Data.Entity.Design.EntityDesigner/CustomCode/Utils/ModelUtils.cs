// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EDMModelHelper = Microsoft.Data.Entity.Design.Model.ModelHelper;

namespace Microsoft.Data.Entity.Design.EntityDesigner.Utils
{
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Data.Entity.Design.Base.Context;
    using Microsoft.Data.Entity.Design.EntityDesigner.CustomSerializer;
    using Microsoft.Data.Entity.Design.EntityDesigner.View;
    using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
    using Microsoft.VisualStudio.Modeling;

    internal static class ModelUtils
    {
        /// <summary>
        ///     Returns true if the store is serializing, false otherwise
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        internal static bool IsSerializing(Store store)
        {
            var serializing = false;

            if ((store != null)
                && (store.TransactionManager != null)
                && (store.TransactionManager.CurrentTransaction != null))
            {
                serializing = store.TransactionManager.CurrentTransaction.IsSerializing;
            }

            return serializing;
        }

        /// <summary>
        ///     Returns the name of Current Active Tx on that Store, otherwise return null
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        internal static Transaction GetCurrentTx(Store store)
        {
            Transaction tx = null;

            if ((store != null)
                && (store.TransactionManager != null))
            {
                tx = store.TransactionManager.CurrentTransaction;
            }

            return tx;
        }

        /// <summary>
        ///     Exports the diagram as an image
        /// </summary>
        internal static void ExportDiagram(EntityDesignerDiagram diagram)
        {
            if (diagram == null)
            {
                return;
            }

            var childShapes = diagram.NestedChildShapes;
            Debug.Assert(childShapes != null && childShapes.Count > 0, "Diagram '" + diagram.Title + "' is empty");

            if (childShapes == null || childShapes.Count == 0)
            {
                return;
            }

            // Get model name from EDMX file name (without extension)
            var modelName = "EntityModel";
            try
            {
                var viewModel = diagram.GetModel();
                if (viewModel != null && viewModel.EditingContext != null)
                {
                    var artifactService = viewModel.EditingContext.GetEFArtifactService();
                    if (artifactService != null && artifactService.Artifact != null)
                    {
                        var filePath = artifactService.Artifact.Uri.LocalPath;
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            modelName = Path.GetFileNameWithoutExtension(filePath);
                        }
                    }
                }
            }
            catch
            {
                // Fall back to diagram title if we can't get the file name
                if (!string.IsNullOrEmpty(diagram.Title))
                {
                    modelName = diagram.Title;
                }
            }

            var dlg = new ExportDiagramDialog(modelName, diagram.DisplayNameAndType);
            if (dlg.ShowModal() != true)
            {
                return;
            }

            // Create export options and use ExportManager to handle the export
            var options = dlg.CreateExportOptions();
            var exportManager = new ExportManager();
            exportManager.Export(diagram, options);
        }

        internal static bool IsUniqueName(ModelElement elementToCheck, string proposedName, EditingContext context)
        {
            if (string.IsNullOrEmpty(proposedName)
                || elementToCheck == null)
            {
                return false;
            }

            var xref = ModelToDesignerModelXRef.GetModelToDesignerModelXRef(context);
            var modelItem = xref.GetExisting(elementToCheck);
            if (modelItem != null)
            {
                string msg;
                return EDMModelHelper.IsUniqueNameForExistingItem(modelItem, proposedName, true, out msg);
            }

            return true;
        }
    }
}
