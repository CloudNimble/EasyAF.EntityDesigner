// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.Data.Entity.Design.EntityDesigner.View;
using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
using Microsoft.Data.Entity.Design.Model;

namespace Microsoft.Data.Entity.Design.Package
{
    /// <summary>
    ///     Drives the interactive diagram export: prompts for a format and destination, then hands the work to the
    ///     renderer.
    /// </summary>
    /// <remarks>
    ///     This lives in the package rather than beside the exporters because it is the only part of exporting that
    ///     needs a user. Microsoft.Data.Entity.Design.Renderer deliberately has no UI so that a command line tool can
    ///     use it, which means the dialog and this orchestration belong on the Visual Studio side of the line.
    /// </remarks>
    internal static class DiagramExportHelper
    {
        /// <summary>
        ///     Prompts the user for export settings and writes the diagram to the file they choose.
        /// </summary>
        /// <param name="diagram">The diagram to export. Nothing happens if it is null or empty.</param>
        internal static void ExportDiagram(EntityDesignerDiagram diagram)
        {
            if (diagram is null)
            {
                return;
            }

            var childShapes = diagram.NestedChildShapes;
            Debug.Assert(childShapes is not null && childShapes.Count > 0, "Diagram '" + diagram.Title + "' is empty");

            if (childShapes is null || childShapes.Count == 0)
            {
                return;
            }

            var dialog = new ExportDiagramDialog(GetModelName(diagram), diagram.DisplayNameAndType);
            if (dialog.ShowModal() != true)
            {
                return;
            }

            new ExportManager(new ShellRasterExporter()).Export(diagram, dialog.CreateExportOptions());
        }

        /// <summary>
        ///     Gets the name to seed the export file name with, preferring the EDMX file name over the diagram title.
        /// </summary>
        private static string GetModelName(EntityDesignerDiagram diagram)
        {
            try
            {
                var artifact = diagram.GetModel()?.EditingContext?.GetEFArtifactService()?.Artifact;
                var filePath = artifact?.Uri.LocalPath;

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    return Path.GetFileNameWithoutExtension(filePath);
                }
            }
            catch
            {
                // Falls through to the diagram title below. The model name only seeds a file name, so failing to
                // resolve the artifact must not stop the user exporting.
            }

            return string.IsNullOrWhiteSpace(diagram.Title) ? "EntityModel" : diagram.Title;
        }
    }
}
