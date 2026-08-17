// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows.Forms.Design;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using EntityDesignerRes = Microsoft.Data.Entity.Design.Diagrams.Properties.Resources;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Model;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.Data.Entity.Design.Diagrams.View.Events;

namespace Microsoft.VisualStudio.Data.Entity.Package.Watermark
{
    /// <summary>
    ///     Puts the clickable links on a designer's watermark and carries out what they ask for.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Every watermark link is a command against Visual Studio: show a tool window, open the document in
    ///         the XML editor, retarget the document's schema version. The designer raises
    ///         <see cref="EntityDesignerSurface.WatermarkLinksRequested" /> once its watermark is built and this
    ///         attaches the rest, which is why the same designer renders a plain-text watermark under the command
    ///         line renderer.
    ///     </para>
    ///     <para>
    ///         It also answers <see cref="EntityDesignerSurface.WatermarkTextRequested" />, because whether the
    ///         containing project supports Entity Framework is something only the shell can see.
    ///     </para>
    /// </remarks>
    internal sealed class VsDiagramWatermark : IDisposable
    {

        #region Fields

        private readonly EntityDesignerSurface _surface;
        private bool _isDisposed;

        #endregion

        #region Constructors

        /// <summary>
        ///     Subscribes to <paramref name="surface" />'s watermark requests.
        /// </summary>
        /// <param name="surface">The designer surface whose watermark to dress.</param>
        /// <exception cref="ArgumentNullException"><paramref name="surface" /> is <see langword="null" />.</exception>
        internal VsDiagramWatermark(EntityDesignerSurface surface)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));

            _surface.WatermarkLinksRequested += OnWatermarkLinksRequested;
            _surface.WatermarkTextRequested += OnWatermarkTextRequested;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Stops dressing the watermark.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _surface.WatermarkLinksRequested -= OnWatermarkLinksRequested;
            _surface.WatermarkTextRequested -= OnWatermarkTextRequested;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Retargets an artifact's XML namespaces to <paramref name="targetSchemaVersion" /> and points the
        ///     mapping window at the result.
        /// </summary>
        /// <param name="package">The package owning the document frame manager.</param>
        /// <param name="editingContext">The editing context for the document.</param>
        /// <param name="entityDesignArtifact">The artifact to retarget.</param>
        /// <param name="targetSchemaVersion">The schema version to retarget to.</param>
        // internal for testing
        internal static void ReversionModel(
            IEdmPackage package, EditingContext editingContext, EntityDesignArtifact entityDesignArtifact, Version targetSchemaVersion)
        {
            CommandProcessorContext cpc = new CommandProcessorContext(
                editingContext, EfiTransactionOriginator.EntityDesignerOriginatorId,
                EntityDesignerRes.RetargetDocumentFromWatermarkTransactionName, entityDesignArtifact);

            RetargetXmlNamespaceCommand.RetargetArtifactXmlNamespaces(cpc, entityDesignArtifact, targetSchemaVersion);

            // The code below ensure that our mapping window works property after the command is executed.
            Debug.Assert(package.DocumentFrameMgr != null, "Could not find the DocumentFrameMgr for this package");
            package.DocumentFrameMgr?.SetCurrentContext(editingContext);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Adds one link to the watermark, if its text appears in the watermark at all.
        /// </summary>
        private static LinkLabel.Link AddLink(DiagramView diagramView, string linkText, object linkData)
        {
            LinkLabel.Link link = null;
            diagramView.Watermark.ForeColor = SystemColors.WindowText;
            var diagramWatermark = diagramView.Watermark;

            var waterMarkBegin = diagramWatermark.Text.IndexOf(linkText, StringComparison.OrdinalIgnoreCase);

            if (waterMarkBegin > 0)
            {
                var waterMarkLength = linkText.Length;
                link = new LinkLabel.Link(waterMarkBegin, waterMarkLength);
                link.Name = linkText;
                link.LinkData = linkData;

                // Note that even if the link has the same bounds and link data, the standard Contains()
                // operation will return FALSE based on the hashcode. Thus we need to add a key. The following
                // debug-time check will verify that if there is a link associated with the key, it has the same
                // bounds as the incoming link; i.e. we can't have two 'Toolbox' links in a watermark.
#if DEBUG
                if (diagramWatermark.Links.ContainsKey(linkText))
                {
                    var linkAlreadyInWatermark = diagramWatermark.Links[linkText];
                    Debug.Assert(
                        linkAlreadyInWatermark != null,
                        "Attempted to do debug-time verification of link '" + linkText
                        + "' in watermark but couldn't find the link even though there is a key for it");
                    if (linkAlreadyInWatermark != null)
                    {
                        // Verify that the bounds and link data are the same
                        Debug.Assert(
                            linkAlreadyInWatermark.Start == link.Start && linkAlreadyInWatermark.Length == link.Length,
                            "We found a link in the watermark associated with '" + linkText
                            + "' but it has different bounds than the one we are trying to add. This is not allowed.");
                        Debug.Assert(
                            linkAlreadyInWatermark.LinkData == link.LinkData,
                            "We found a link in the watermark associated with '" + linkText
                            + "' but it has different link data than the one we are trying to add. This is not allowed.");
                    }
                }
#endif
                if (false == diagramWatermark.Links.ContainsKey(linkText))
                {
                    diagramWatermark.Links.Add(link);
                }
            }

            return link;
        }

        /// <summary>
        ///     Retargets the document's schema version.
        /// </summary>
        private void OnUpgradeLinkClicked()
        {
            // Display hourglass since the operation may take some time especially for a large model.
            using (new VsUtils.HourglassHelper())
            {
                var editingContext = _surface.GetModel().EditingContext;
                EntityDesignArtifact entityDesignArtifact = editingContext.GetEFArtifactService().Artifact as EntityDesignArtifact;
                Debug.Assert(entityDesignArtifact != null, "EFArtifact is not an instance of EntityDesignArtifact");
                if (entityDesignArtifact != null)
                {
                    var targetSchemaVersion = EdmUtils.GetEntityFrameworkVersion(
                        VSHelpers.GetProjectForDocument(entityDesignArtifact.Uri.LocalPath, PackageManager.Package),
                        PackageManager.Package, useLatestIfNoEF: false);

                    ReversionModel(PackageManager.Package, editingContext, entityDesignArtifact, targetSchemaVersion);
                }
            }
        }

        /// <summary>
        ///     Dispatches a watermark link click to whichever window or command it stands for.
        /// </summary>
        private void OnWatermarkLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (e.Link.LinkData is LinkAction linkAction)
            {
                switch (linkAction)
                {
                    case LinkAction.XmlEditor:
                        OnXmlEditorLinkClicked();
                        break;

                    case LinkAction.Upgrade:
                        OnUpgradeLinkClicked();
                        break;

                    case LinkAction.ShowModelBrowser:
                        Debug.Assert(PackageManager.Package != null, "Entity Designer Package is null.");
                        Debug.Assert(
                            PackageManager.Package?.ExplorerWindow != null,
                            "Unable to get instance of Model Browser window from package.");
                        PackageManager.Package?.ExplorerWindow?.Show();
                        break;

                    default:
                        Debug.Fail("unexpected LinkAction value in LinkData value");
                        break;
                }
            }
            else if (e.Link.LinkData is Guid toolWindow
                     && toolWindow != Guid.Empty)
            {
                IUIService uiService = PackageManager.Package.GetService(typeof(IUIService)) as IUIService;
                uiService?.ShowToolWindow(toolWindow);
            }
            else
            {
                Debug.Fail("unexpected data for LinkData");
            }
        }

        /// <summary>
        ///     Rebuilds the watermark's links.
        /// </summary>
        private void OnWatermarkLinksRequested(object sender, WatermarkLinksRequestedEventArgs e)
        {
            var watermark = e.DiagramView.Watermark;

            watermark.Links.Clear();
            AddLink(e.DiagramView, EntityDesignerRes.DesignerWatermarkToolboxLink, StandardToolWindows.Toolbox);
            AddLink(e.DiagramView, EntityDesignerRes.DesignerWatermarkModelBrowserLink, LinkAction.ShowModelBrowser);
            AddLink(e.DiagramView, EntityDesignerRes.DesignerWatermarkXmlEditorLink, LinkAction.XmlEditor);
            AddLink(e.DiagramView, EntityDesignerRes.DesignerWatermarkUpgradeLink, LinkAction.Upgrade);

            // ensure the colors for the watermark LinkLabel are correct by VS UX
            VSHelpers.AssignLinkLabelColor(watermark);

            // detach first: the watermark is rebuilt on every reload, and the control outlives the rebuild
            watermark.LinkClicked -= OnWatermarkLinkClicked;
            watermark.LinkClicked += OnWatermarkLinkClicked;
        }

        /// <summary>
        ///     Replaces the designer's watermark when the project does not support Entity Framework at all.
        /// </summary>
        private void OnWatermarkTextRequested(object sender, WatermarkTextRequestedEventArgs e)
        {
            if (_surface.GetModel()?.EditingContext?.GetEFArtifactService()?.Artifact is not VSArtifact artifact)
            {
                return;
            }

            var project = VSHelpers.GetProjectForDocument(artifact.Uri.LocalPath, PackageManager.Package);
            Debug.Assert(project != null);

            if (!VsUtils.EntityFrameworkSupportedInProject(project, PackageManager.Package, allowMiscProject: true))
            {
                e.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    EntityDesignerRes.DesignerWatermark_EDMNotSupported,
                    EntityDesignerRes.DesignerWatermarkXmlEditorLink).Replace(@"\n", "\n");
            }
        }

        /// <summary>
        ///     Opens the document in the XML editor.
        /// </summary>
        private void OnXmlEditorLinkClicked()
        {
            var artifact = _surface.GetModel().EditingContext.GetEFArtifactService().Artifact;

            IServiceProvider sp = PackageManager.Package;
            if (sp != null)
            {
                // Open the referenced document using our editor.
                VsShellUtilities.OpenDocumentWithSpecificEditor(
                    sp, artifact.Uri.LocalPath, CommonPackageConstants.xmlEditorGuid, VSConstants.LOGVIEWID_Primary,
                    out IVsUIHierarchy hierarchy, out uint itemid, out IVsWindowFrame frame);
                if (frame != null)
                {
                    NativeMethods.ThrowOnFailure(frame.Show());
                }
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///     What a watermark link does when clicked.
        /// </summary>
        /// <remarks>
        ///     "View Toolbox" is not here; it is indicated by a tool window GUID in the link data instead.
        /// </remarks>
        private enum LinkAction
        {
            Unknown,
            XmlEditor,
            Upgrade,
            ShowModelBrowser
        }

        #endregion

    }
}
