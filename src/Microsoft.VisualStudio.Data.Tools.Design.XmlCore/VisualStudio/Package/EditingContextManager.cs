// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Design.Serialization;

namespace Microsoft.Data.Entity.Design.VisualStudio.Package
{
    /// <summary>
    ///     The ArtifactManager keeps track of all EDM artifact files, a list
    ///     of frames that are loaded with artifacts file documents,
    ///     and the association of frames to a particular artifact
    /// </summary>
    internal class EditingContextManager
    {
        // we need this hash table; we could do reverse-lookups on the EFArtifact for the EditingContext that holds it, but we don't want to have
        // any references to the designer inside the artifact. 
        private Dictionary<EFArtifact, EditingContext> _mapArtifactToEditingContext = [];
        private Dictionary<FrameWrapper, EditingContext> _mapFrameToUri = [];
        private readonly IXmlDesignerPackage _package;

        internal EditingContextManager(IXmlDesignerPackage package)
        {
            _package = package;
        }

        
        internal static EFArtifact GetArtifact(EditingContext context)
        {
            if (context != null)
            {
                var service = context.GetEFArtifactService();
                if (service != null)
                {
                    return service.Artifact;
                }
            }
            return null;
        }

        internal static Uri GetArtifactUri(EditingContext context)
        {
            var item = GetArtifact(context);
            if (item != null)
            {
                return item.Uri;
            }
            return null;
        }

        protected virtual EFArtifact GetNewOrExistingArtifact(Uri itemUri)
        {
            return _package.ModelManager.GetNewOrExistingArtifact(itemUri, new VSXmlModelProvider(_package, _package));
        }

        internal bool DoesContextExist(Uri itemUri)
        {
            var artifact = GetNewOrExistingArtifact(itemUri);
            if (artifact != null)
            {
                return _mapArtifactToEditingContext.ContainsKey(artifact);
            }

            return false;
        }

        internal EditingContext GetNewOrExistingContext(Uri itemUri)
        {
            EditingContext itemContext = null;

            // creating a new context is an expensive operation, so optimize for the case where it exists
            var item = _package.ModelManager.GetArtifact(itemUri);
            if (item != null)
            {
                _mapArtifactToEditingContext.TryGetValue(item, out itemContext);
            }

            // there isn't one, so call the path that will create it
            if (itemContext == null)
            {
                item = GetNewOrExistingArtifact(itemUri);
                if (itemUri != null
                    && item != null
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

        internal void OnCloseFrame(FrameWrapper closingFrame)
        {
            if (_mapFrameToUri.ContainsKey(closingFrame))
            {
                _mapFrameToUri.Remove(closingFrame);

                if (null != closingFrame.Uri)
                {
                    RunningDocumentTable rdt = new RunningDocumentTable(_package);
                    var doc = rdt.FindDocument(closingFrame.Uri.LocalPath);
                    if (doc != null)
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

        internal void CloseArtifact(Uri uri)
        {
            Debug.Assert(uri != null, "uri != null");

            var artifact = _package.ModelManager.GetArtifact(uri);
            if (artifact != null
                && _mapArtifactToEditingContext.TryGetValue(artifact, out EditingContext editingContext))
            {
                _mapArtifactToEditingContext.Remove(artifact);
                _package.ModelManager.ClearArtifact(artifact.Uri);
                editingContext.Dispose();
            }
        }

        private IEnumerable<EditingContext> GetOpenContexts()
        {
            return new List<EditingContext>(_mapArtifactToEditingContext.Values);
        }

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

        private Collection<Uri> GetAssociatedUris(Uri itemDocUri)
        {
            Collection<Uri> associated = new Collection<Uri>
            {
                itemDocUri
            };
            foreach (var editingContext in GetOpenContexts())
            {
                var item = GetArtifact(editingContext);
                if (item != null)
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

        internal Uri GetCurrentUri(FrameWrapper frame)
        {
            if (!_mapFrameToUri.TryGetValue(frame, out EditingContext context))
            {
                if (context == null)
                {
                    SetCurrentUri(frame, frame.Uri);
                    return frame.Uri;
                }
            }

            var artifactService = context.GetEFArtifactService();
            Debug.Assert(
                artifactService != null && artifactService.Artifact != null,
                "There is no artifact service/artifact tied to this editing context!");
            if (artifactService != null
                && artifactService.Artifact != null)
            {
                return artifactService.Artifact.Uri;
            }
            return null;
        }

        internal void SetCurrentUri(FrameWrapper frame, Uri itemUri)
        {
            var context = GetNewOrExistingContext(itemUri);
            _mapFrameToUri[frame] = context;
        }
    }
}
