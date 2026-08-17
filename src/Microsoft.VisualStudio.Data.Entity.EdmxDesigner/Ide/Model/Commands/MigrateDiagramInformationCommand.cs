// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Command = Microsoft.Data.Entity.Design.XmlEngine.Model.Commands.Command;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Model.Commands
{
    // <summary>
    //     Migrate diagrams node from EDMX file to a separate file.
    // </summary>
    internal class MigrateDiagramInformationCommand : Command
    {
        private readonly EntityDesignArtifact _artifact;

        // Intentionally set this to private because we only want this command to be instantiated from static method and not to be combined with other command.
        private MigrateDiagramInformationCommand(EntityDesignArtifact artifact)
        {
            _artifact = artifact;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // First check if the diagram file exists, if yes quit immediately.
            var diagramFilePath = _artifact.Uri.LocalPath + EntityDesignArtifact.ExtensionDiagram;
            if (!File.Exists(diagramFilePath)
                && (_artifact != null))
            {
                // The diagram file should have same structure as the EDMX file but only contains diagrams node.
                // To accomplish that the method will the do the following:
                // - Load the EDMX file to XDocument.
                // - Store Diagrams root node.
                // - Remove all nodes under root node.
                // - Create and add Designer node under root node.
                // - Re-Add Diagrams node under Designer node.
                XDocument document = XDocument.Parse(_artifact.XDocument.ToString(), LoadOptions.PreserveWhitespace);
                var rootDiagramsNode =
                    document.Descendants(XName.Get("Diagrams", SchemaManager.GetEDMXNamespaceName(_artifact.SchemaVersion)))
                        .FirstOrDefault();
                Debug.Assert(rootDiagramsNode != null, "Diagrams node does not exist in the EDMX file.");

                if (rootDiagramsNode != null)
                {
                    // Remove any nodes under root.
                    document.Root.RemoveNodes();
                    // Create an empty Designer node.
                    XElement element2 = new XElement(XName.Get("Designer", SchemaManager.GetEDMXNamespaceName(_artifact.SchemaVersion)));
                    element2.Add(rootDiagramsNode);
                    document.Root.Add(element2);
                    // Save the diagram file to disk.
                    document.Save(diagramFilePath, SaveOptions.None);

                    // Remove all diagram nodes under Diagrams node.
                    // Note that we want to still keep the Diagrams node for backward compatibility purpose. (The EDMX file could still be opened in the older Visual Studio).
                    _artifact.XDocument.Descendants(XName.Get("Diagrams", SchemaManager.GetEDMXNamespaceName(_artifact.SchemaVersion)))
                        .First()
                        .RemoveNodes();

                    // The code below is to ensure that Diagrams artifact in instantiated and initialized properly.
                    DiagramArtifact efArtifact = new VSDiagramArtifact(
                        _artifact.ModelManager, new Uri(diagramFilePath), _artifact.XmlModelProvider);
                    _artifact.DiagramArtifact = efArtifact;
                    _artifact.ModelManager.RegisterArtifact(efArtifact, _artifact.ArtifactSet);
                }
            }
        }

        internal static void DoMigrate(CommandProcessorContext cpc, EntityDesignArtifact artifact)
        {
            VSXmlModelProvider xmlModelProvider = artifact.XmlModelProvider as VSXmlModelProvider;
            Debug.Assert(xmlModelProvider != null, "Artifact's model provider is not type of VSXmlModelProvider.");
            if ((xmlModelProvider != null)
                && (xmlModelProvider.UndoManager != null))
            {
                var undoManager = xmlModelProvider.UndoManager;
                try
                {
                    // We need to temporarily disable the Undo Manager because this operation is not undoable.
                    undoManager?.Enable(0);

                    MigrateDiagramInformationCommand command = new MigrateDiagramInformationCommand(artifact);
                    CommandProcessor processor = new CommandProcessor(cpc, shouldNotifyObservers: false);
                    processor.EnqueueCommand(command);
                    processor.Invoke();

                    Debug.Assert(artifact.DiagramArtifact != null, "Diagram artifact should have been created by now.");
                    if (artifact.DiagramArtifact != null)
                    {
                        // Ensure that diagram file is added to the project.
                        AddDiagramFileToProject(artifact);

                        // Reload the artifacts.
                        artifact.ReloadArtifact();
                        artifact.IsDirty = true;

                        // The code below ensures mapping window and model-browser window are refreshed.
                        Debug.Assert(
                            PackageManager.Package.DocumentFrameMgr != null, "Could not find the DocumentFrameMgr for this package");
                        PackageManager.Package.DocumentFrameMgr?.SetCurrentContext(cpc.EditingContext);
                    }
                }
                finally
                {
                    undoManager?.Enable(1);
                }
            }
        }

        // <summary>
        //     Adds the newly created diagram file to the project that owns the EDMX it was split out of.
        // </summary>
        // <remarks>
        //     DTE's ItemOperations.AddExistingItem cannot be used here. It adds to whatever Solution Explorer has
        //     selected, but this command runs from the designer surface and the Model Browser, where the Solution
        //     Explorer selection is unrelated to the EDMX - and when nothing is selected it throws
        //     "Item can not be added to a project when multiple or no items are selected in the Solution Explorer."
        //     Resolving the project from the artifact's own path removes the dependency on the selection entirely.
        //     The diagram file is added under the EDMX's project item so it nests, which is how the shipping designer
        //     records it (a None item with DependentUpon pointing at the EDMX).
        // </remarks>
        private static void AddDiagramFileToProject(EntityDesignArtifact artifact)
        {
            var diagramFilePath = artifact.DiagramArtifact.Uri.LocalPath;

            ProjectItem edmxProjectItem = VsUtils.GetProjectItemForDocument(artifact.Uri.LocalPath, PackageManager.Package);
            if (edmxProjectItem is null)
            {
                // The EDMX is not part of a project - the Miscellaneous Files project, for example. The diagram file
                // still exists on disk beside it and the model still loads; there is simply nothing to add it to.
                VsUtils.LogToActivityLog(
                    $"MigrateDiagramInformation: '{artifact.Uri.LocalPath}' has no project item, so '{diagramFilePath}' was not added to a project.",
                    __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING);
                return;
            }

            // SDK-style projects pick the file up by glob as soon as it lands on disk, and adding it again throws.
            if (VsUtils.GetProjectItemForDocument(diagramFilePath, PackageManager.Package) is not null)
            {
                return;
            }

            Project containingProject = edmxProjectItem.ContainingProject;
            if (containingProject is null
                || VsUtils.IsMiscellaneousProject(containingProject))
            {
                // An EDMX opened on its own, with no solution or project loaded, lands in the Miscellaneous Files
                // project, which does not support ProjectItems extensibility. The diagram file sits beside the EDMX on
                // disk and the model loads from it either way; there is simply no project to record it in.
                VsUtils.LogToActivityLog(
                    $"MigrateDiagramInformation: '{artifact.Uri.LocalPath}' does not belong to a real project, so '{diagramFilePath}' was not added to one.");
                return;
            }

            ProjectItems targetCollection = edmxProjectItem.ProjectItems ?? containingProject.ProjectItems;
            if (targetCollection is null)
            {
                VsUtils.LogToActivityLog(
                    $"MigrateDiagramInformation: the project containing '{artifact.Uri.LocalPath}' does not support adding items, so '{diagramFilePath}' was not added to it.",
                    __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING);
                return;
            }

            try
            {
                targetCollection.AddFromFile(diagramFilePath);
            }
            catch (COMException ex)
            {
                // The diagrams have already been written to disk and the artifact reloaded by this point. A project
                // system that refuses the item should not make the migration itself look like it failed.
                VsUtils.LogToActivityLog(
                    $"MigrateDiagramInformation: could not add '{diagramFilePath}' to the project: {ex.Message}",
                    __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
            }
        }
    }
}
