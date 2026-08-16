// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Modeling.Shell;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.Design.Ide;

namespace Microsoft.Data.Entity.Design.Package
{
    /// <summary>
    ///     Finds the Visual Studio document view whose diagram can show a given <see cref="EFObject" />, and brings
    ///     that window forward.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The search runs in two passes: the active designer view first, then every open designer view for the
    ///         object's artifact. Each candidate diagram is asked to navigate; the first one that reports a match
    ///         wins and its frame is shown.
    ///     </para>
    ///     <para>
    ///         Deciding <em>which</em> window to show is shell work, which is why it lives here rather than beside
    ///         <see cref="DiagramNavigator" /> in the designer. See specs/platform-independence.md.
    ///     </para>
    /// </remarks>
    internal static class DesignerNavigator
    {

        #region Public Methods

        /// <summary>
        ///     Selects the shape for <paramref name="efobject" /> in whichever open designer view can show it.
        /// </summary>
        /// <param name="efobject">The object to navigate to. S-Space objects and unrooted objects are ignored.</param>
        internal static void NavigateTo(EFObject efobject)
        {
            if (efobject.RuntimeModelRoot() == null)
            {
                // nothing to navigate to, so just return;
                return;
            }

            if (efobject.RuntimeModelRoot() is StorageEntityModel)
            {
                // s-space object, so just return;
                return;
            }

            var selectionService = PackageManager.Package.GetMonitorSelectionService();
            Debug.Assert(selectionService != null, "Could not retrieve IMonitorSelectionService from Escher package.");
            if (selectionService?.CurrentDocumentView is SingleDiagramDocView activeDocView
                && TryNavigate(activeDocView, efobject))
            {
                return;
            }

            // Retrieves the doc data for the efobject.
            ModelingDocData docdata = VSHelpers.GetDocData(PackageManager.Package, efobject.Uri.LocalPath) as ModelingDocData;
            Debug.Assert(docdata != null, "Could not find get doc data for artifact with URI:" + efobject.Uri.LocalPath);
            if (docdata == null)
            {
                return;
            }

            foreach (var docView in docdata.DocViews)
            {
                SingleDiagramDocView singleDiagramDocView = docView as SingleDiagramDocView;
                Debug.Assert(
                    singleDiagramDocView != null,
                    "Why the doc view is not type of SingleDiagramDocView? Actual type:" + docView.GetType().Name);
                if (TryNavigate(singleDiagramDocView, efobject))
                {
                    return;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Asks one document view's diagram to navigate, and shows the view if it found a match.
        /// </summary>
        /// <param name="docView">The candidate view. May be <see langword="null" />.</param>
        /// <param name="efobject">The object to navigate to.</param>
        /// <returns><see langword="true" /> if this view matched and was brought forward.</returns>
        private static bool TryNavigate(SingleDiagramDocView docView, EFObject efobject)
        {
            if (docView?.Diagram is not EntityDesignerSurface surface)
            {
                return false;
            }

            if (!DiagramNavigator.NavigateToNodeInDiagram(surface, efobject))
            {
                return false;
            }

            // ensure that the right doc-view is shown and activated.
            docView.Frame.Show();

            return true;
        }

        #endregion

    }
}
