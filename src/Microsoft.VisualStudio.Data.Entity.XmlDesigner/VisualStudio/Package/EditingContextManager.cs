// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package
{

    /// <summary>
    ///     Maps artifact URIs to the <see cref="EditingContext" /> instances that hold them, one context per
    ///     open document.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This type is what guarantees a document has exactly one editing context. Every entry point that
    ///         needs a context for a URI goes through <see cref="GetNewOrExistingContext(Uri)" />, which returns
    ///         the existing context when one has already been created and only builds a new one otherwise. Two
    ///         contexts over the same artifact would give the designer and the XML editor separate, silently
    ///         diverging views of the same file.
    ///     </para>
    ///     <para>
    ///         The artifact to context map exists rather than reverse looking up the context that owns a given
    ///         <see cref="EFArtifact" />, because that would require the artifact to hold a reference back into
    ///         the designer. The model layer deliberately knows nothing about the designer, so the association
    ///         is kept out here instead.
    ///     </para>
    ///     <para>
    ///         The frame map is a separate concern: it records which artifact each open window frame is currently
    ///         showing, so that closing or re-targeting a frame does not disturb the artifact-to-context mapping
    ///         other frames rely on.
    ///     </para>
    /// </remarks>
    internal class EditingContextManager
    {

        #region Fields

        /// <summary>
        ///     The one context per artifact map that makes a document's editing context unique.
        /// </summary>
        /// <remarks>
        ///     A reverse lookup from <see cref="EFArtifact" /> to its context is not possible without giving the
        ///     artifact a reference to the designer, which the model layer intentionally does not have.
        /// </remarks>
        private Dictionary<EFArtifact, EditingContext> _mapArtifactToEditingContext = [];

        /// <summary>
        ///     The context each open window frame is currently showing.
        /// </summary>
        /// <remarks>
        ///     Despite the name, the value is the <see cref="EditingContext" /> rather than the URI; the URI is
        ///     reached through the context's artifact service so that it always reflects the artifact currently
        ///     loaded, not a stale copy taken when the frame was opened.
        /// </remarks>
        private Dictionary<FrameWrapper, EditingContext> _mapFrameToUri = [];

        /// <summary>
        ///     The package supplying the model manager and the XML model provider used to load artifacts.
        /// </summary>
        private readonly IXmlDesignerPackage _package;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="EditingContextManager" /> class.
        /// </summary>
        /// <param name="package">The package whose model manager owns the artifacts this manager maps.</param>
        internal EditingContextManager(IXmlDesignerPackage package)
        {
            _package = package;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Gets the artifact for <paramref name="itemUri" /> from the model manager, loading it if it is not
        ///     already loaded.
        /// </summary>
        /// <param name="itemUri">The URI of the artifact to obtain.</param>
        /// <returns>The existing or newly loaded artifact, or <see langword="null" /> if none could be produced.</returns>
        /// <remarks>
        ///     Virtual so tests and derived managers can supply an artifact without going through the Visual Studio
        ///     XML model provider, which requires a live shell.
        /// </remarks>
        protected virtual EFArtifact GetNewOrExistingArtifact(Uri itemUri)
        {
            return _package.ModelManager.GetNewOrExistingArtifact(itemUri, new VSXmlModelProvider(_package, _package));
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Drops the editing context for <paramref name="uri" />, clears the artifact from the model manager,
        ///     and disposes the context.
        /// </summary>
        /// <param name="uri">The URI of the artifact being closed.</param>
        /// <remarks>
        ///     Removing the map entry before disposing keeps a caller that re-enters during disposal from handing
        ///     out a context that is on its way out. Does nothing when the artifact is not loaded or has no
        ///     context, so closing a document twice is harmless.
        /// </remarks>
        internal void CloseArtifact(Uri uri)
        {
            Debug.Assert(uri is not null, "uri != null");

            var artifact = _package.ModelManager.GetArtifact(uri);
            if (artifact is not null
                && _mapArtifactToEditingContext.TryGetValue(artifact, out EditingContext editingContext))
            {
                _mapArtifactToEditingContext.Remove(artifact);
                _package.ModelManager.ClearArtifact(artifact.Uri);
                editingContext.Dispose();
            }
        }

        /// <summary>
        ///     Determines whether an editing context already exists for <paramref name="itemUri" />.
        /// </summary>
        /// <param name="itemUri">The URI to test.</param>
        /// <returns><see langword="true" /> if a context exists for the artifact; otherwise <see langword="false" />.</returns>
        /// <remarks>
        ///     Resolving the artifact may load it, so this answers "is there a context" and not "is anything
        ///     already in memory".
        /// </remarks>
        internal bool DoesContextExist(Uri itemUri)
        {
            var artifact = GetNewOrExistingArtifact(itemUri);
            if (artifact is not null)
            {
                return _mapArtifactToEditingContext.ContainsKey(artifact);
            }

            return false;
        }

        /// <summary>
        ///     Gets the artifact held by <paramref name="context" />.
        /// </summary>
        /// <param name="context">The editing context to read, which may be <see langword="null" />.</param>
        /// <returns>
        ///     The artifact from the context's <see cref="EFArtifactService" />, or <see langword="null" /> when
        ///     the context is null or carries no artifact service.
        /// </returns>
        /// <remarks>
        ///     This is nothing more than a null safe wrapper over <c>context.GetEFArtifactService().Artifact</c>.
        ///     It performs no lookup, no loading, and no registration, and it never creates a context or an
        ///     artifact — callers frequently assume otherwise. A <see langword="null" /> result means only that
        ///     the context in hand has no artifact attached yet; use
        ///     <see cref="GetNewOrExistingContext(Uri)" /> when an artifact is actually required.
        /// </remarks>
        internal static EFArtifact GetArtifact(EditingContext context)
        {
            if (context is not null)
            {
                var service = context.GetEFArtifactService();
                if (service is not null)
                {
                    return service.Artifact;
                }
            }
            return null;
        }

        /// <summary>
        ///     Gets the URI of the artifact held by <paramref name="context" />.
        /// </summary>
        /// <param name="context">The editing context to read, which may be <see langword="null" />.</param>
        /// <returns>The artifact's URI, or <see langword="null" /> when the context holds no artifact.</returns>
        /// <remarks>
        ///     Shares the null safe, lookup free behaviour of <see cref="GetArtifact(EditingContext)" />.
        /// </remarks>
        internal static Uri GetArtifactUri(EditingContext context)
        {
            var item = GetArtifact(context);
            if (item is not null)
            {
                return item.Uri;
            }
            return null;
        }

        /// <summary>
        ///     Gets the URIs whose views should be treated as related to <paramref name="frame" />.
        /// </summary>
        /// <param name="frame">The frame to inspect.</param>
        /// <returns>
        ///     Just the frame's own URI when it hosts the designer, every open artifact URI when it hosts the XML
        ///     editor, or <see langword="null" /> when the frame shows neither.
        /// </returns>
        /// <remarks>
        ///     The asymmetry is deliberate. A designer frame owns exactly one artifact, whereas editing the XML
        ///     directly can invalidate any other open document, so the XML editor case fans out to all of them.
        /// </remarks>
        internal Collection<Uri> GetAssociatedUris(FrameWrapper frame)
        {
            if (frame.IsDesignerDocInDesigner)
            {
                return new Collection<Uri>(new[] { frame.Uri });
            }
            if (frame.IsDesignerDocInXmlEditor)
            {
                return GetAssociatedUris(frame.Uri);
            }
            return null;
        }

        /// <summary>
        ///     Gets the URI of the artifact <paramref name="frame" /> is currently showing, associating the frame
        ///     with its own URI if it has not been recorded yet.
        /// </summary>
        /// <param name="frame">The frame to query.</param>
        /// <returns>
        ///     The URI of the artifact behind the frame's context, or <see langword="null" /> if that context has
        ///     no artifact service.
        /// </returns>
        /// <remarks>
        ///     The URI is read back off the artifact rather than returned from the map, so that a frame retargeted
        ///     to a different artifact reports the artifact it actually shows.
        /// </remarks>
        internal Uri GetCurrentUri(FrameWrapper frame)
        {
            if (!_mapFrameToUri.TryGetValue(frame, out EditingContext context))
            {
                if (context is null)
                {
                    SetCurrentUri(frame, frame.Uri);
                    return frame.Uri;
                }
            }

            var artifactService = context.GetEFArtifactService();
            Debug.Assert(
                artifactService is not null && artifactService.Artifact is not null,
                "There is no artifact service/artifact tied to this editing context!");
            if (artifactService is not null
                && artifactService.Artifact is not null)
            {
                return artifactService.Artifact.Uri;
            }
            return null;
        }

        /// <summary>
        ///     Gets the editing context for <paramref name="itemUri" />, creating one only if the artifact does
        ///     not already have a context.
        /// </summary>
        /// <param name="itemUri">The URI of the artifact whose context is wanted.</param>
        /// <returns>
        ///     The single editing context for that artifact, or <see langword="null" /> when the URI is null or no
        ///     artifact could be produced for it.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The first lookup asks the model manager only for an artifact that is already loaded, because
        ///         constructing a context is expensive and the overwhelmingly common case is that one exists. Only
        ///         when that misses does the method fall through to the path that may load the artifact.
        ///     </para>
        ///     <para>
        ///         The second <c>TryGetValue</c> is not redundant: <see cref="GetNewOrExistingArtifact(Uri)" />
        ///         may resolve to a different artifact instance than the first lookup found, and that instance may
        ///         already own a context. This is the choke point that keeps one document to one context.
        ///     </para>
        /// </remarks>
        internal EditingContext GetNewOrExistingContext(Uri itemUri)
        {
            EditingContext itemContext = null;

            // creating a new context is an expensive operation, so optimize for the case where it exists
            var item = _package.ModelManager.GetArtifact(itemUri);
            if (item is not null)
            {
                _mapArtifactToEditingContext.TryGetValue(item, out itemContext);
            }

            // there isn't one, so call the path that will create it
            if (itemContext is null)
            {
                item = GetNewOrExistingArtifact(itemUri);
                if (itemUri is not null
                    && item is not null
                    && !_mapArtifactToEditingContext.TryGetValue(item, out itemContext))
                {
                    EFArtifactService service = new EFArtifactService(item);

                    EditingContext editingContext = new EditingContext();
                    editingContext.SetEFArtifactService(service);
                    itemContext = editingContext;
                    _mapArtifactToEditingContext[item] = itemContext;
                }
            }

            return itemContext;
        }

        /// <summary>
        ///     Forgets the frame that is closing and detects whether its document was closed with unsaved changes.
        /// </summary>
        /// <param name="closingFrame">The frame being closed.</param>
        /// <remarks>
        ///     <para>
        ///         Only the frame association is dropped. The artifact and its editing context stay alive, because
        ///         other frames may still be showing the same document; tearing the context down is
        ///         <see cref="CloseArtifact(Uri)" />'s job.
        ///     </para>
        ///     <para>
        ///         When the document was dirty at close time, every model that refers to it is stale and should be
        ///         refreshed from what is on disk. That refresh is not implemented yet, so the detection currently
        ///         runs to no effect.
        ///     </para>
        /// </remarks>
        internal void OnCloseFrame(FrameWrapper closingFrame)
        {
            if (_mapFrameToUri.ContainsKey(closingFrame))
            {
                _mapFrameToUri.Remove(closingFrame);

                if (closingFrame.Uri is not null)
                {
                    RunningDocumentTable rdt = new RunningDocumentTable(_package);
                    var doc = rdt.FindDocument(closingFrame.Uri.LocalPath);
                    if (doc is not null)
                    {
                        var isModified = false;
                        using (DocData docData = new DocData(doc))
                        {
                            isModified = docData.Modified;
                        }
                        if (isModified)
                        {
                            // document was modified but was closed without saving changes;
                            // we need to refresh all sets that refer to the document
                            // so that they revert to the document that is persisted in the file system

                            // TODO: add this functinality
                            //ModelManager.RefreshModelForLocation(closingFrame.Uri);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Associates <paramref name="frame" /> with the editing context for <paramref name="itemUri" />.
        /// </summary>
        /// <param name="frame">The frame to record.</param>
        /// <param name="itemUri">The URI of the artifact the frame is now showing.</param>
        /// <remarks>
        ///     Goes through <see cref="GetNewOrExistingContext(Uri)" /> rather than creating a context directly,
        ///     so pointing a frame at a document that is already open reuses that document's one context.
        /// </remarks>
        internal void SetCurrentUri(FrameWrapper frame, Uri itemUri)
        {
            var context = GetNewOrExistingContext(itemUri);
            _mapFrameToUri[frame] = context;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Gets <paramref name="itemDocUri" /> together with the URIs of every other open artifact.
        /// </summary>
        /// <param name="itemDocUri">The URI to place first in the result.</param>
        /// <returns>A collection holding <paramref name="itemDocUri" /> and each distinct open artifact URI.</returns>
        /// <remarks>
        ///     The given URI is added unconditionally and then skipped while walking the open contexts, so it
        ///     appears exactly once even when it is itself open. The comparison is ordinal and case insensitive
        ///     because these are file URIs on a case insensitive file system.
        /// </remarks>
        private Collection<Uri> GetAssociatedUris(Uri itemDocUri)
        {
            Collection<Uri> associated = new Collection<Uri>
            {
                itemDocUri
            };
            foreach (var editingContext in GetOpenContexts())
            {
                var item = GetArtifact(editingContext);
                if (item is not null)
                {
                    var itemUri = item.Uri;
                    if (!UriComparer.OrdinalIgnoreCase.Equals(itemUri, itemDocUri))
                    {
                        associated.Add(itemUri);
                    }
                }
            }

            return associated;
        }

        /// <summary>
        ///     Gets the editing contexts currently open.
        /// </summary>
        /// <returns>A snapshot of the open contexts.</returns>
        /// <remarks>
        ///     Copies the values into a new list rather than returning the dictionary's live value collection,
        ///     because callers iterate the result while operations such as closing an artifact mutate the map.
        /// </remarks>
        private IEnumerable<EditingContext> GetOpenContexts()
        {
            return new List<EditingContext>(_mapArtifactToEditingContext.Values);
        }

        #endregion

    }

}
