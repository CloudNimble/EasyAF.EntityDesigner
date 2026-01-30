// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.VisualStudio.Model;
using Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Data.Entity.Design.VisualStudio.Package
{
    // <summary>
    //     The EntityDesignDocumentFrameMgr class manages all document window frames that
    //     are associated to an EDMX file document if they were loaded in Escher or in the XML editor
    // </summary>
    internal class EntityDesignDocumentFrameMgr : DocumentFrameMgr
    {
        private readonly HashSet<Uri> _dirtyArtifactsOnClose = null;

        internal EntityDesignDocumentFrameMgr(IXmlDesignerPackage package)
            : base(package)
        {
        }

        protected internal override FrameWrapper CreateFrameWrapper(IVsWindowFrame frame)
        {
            return new EntityDesignFrameWrapper(frame);
        }

        // <summary>
        //     This method will set the editing context for the mapping details and model browser. This will
        //     also show/hide these tool windows.
        // </summary>
        protected internal override void SetCurrentContext(EditingContext context)
        {
            try
            {
                if (PackageManager.Package != null)
                {
                    PackageManager.Package.MappingDetailsWindow?.Context = context;
                    PackageManager.Package.ExplorerWindow?.Context = context;
                }
            }
            catch
            {
                // Hack Hack; FindToolWindow will throw an exception if we can't find the ExplorerWindow. We should find
                // a more specific way to handle this if we run into this situation.
            }
        }

        protected override void ClearErrorList(Uri oldUri, Uri newUri)
        {
            // clear out the error list so after the rename the errors are bound to the correct artifacts
            ErrorListHelper.ClearErrorsForDocAcrossLists(newUri);

            // for a save-as the errors are associated with the oldUri
            ErrorListHelper.ClearErrorsForDocAcrossLists(oldUri);
        }

        protected override void ClearErrorList(IVsHierarchy pHier, uint ItemID)
        {
            ErrorListHelper.ClearErrorsForDocAcrossLists(pHier, ItemID);
        }

        protected override bool HasDesignerExtension(Uri uri)
        {
            return VSArtifact.GetVSArtifactFileExtensions().Contains(Path.GetExtension(uri.LocalPath));
        }

        protected override void OnAfterDesignerDocumentWindowHide(Uri docUri)
        {
            // see if the browser is showing this
            var explorerWindow = PackageManager.Package.ExplorerWindow;
            if (explorerWindow != null)
            {
                var explorerUri = EditingContextManager.GetArtifactUri(explorerWindow.Context);
                if (UriComparer.OrdinalIgnoreCase.Equals(docUri, explorerUri))
                {
                    // the browser's Uri is closing, so clear out the browser
                    explorerWindow.Context = null;
                }
            }

            // see if the mapping window is showing this
            var mappingWindow = PackageManager.Package.MappingDetailsWindow;
            if (mappingWindow != null)
            {
                var mappingUri = EditingContextManager.GetArtifactUri(mappingWindow.Context);
                if (UriComparer.OrdinalIgnoreCase.Equals(docUri, mappingUri))
                {
                    // the mapping's Uri is closing, so clear it out
                    mappingWindow.Context = null;
                }
            }
        }

        public override int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            if (fFirstShow != 0)
            {
                if (pFrame != null)
                {
                    EntityDesignFrameWrapper frameWrapper = new EntityDesignFrameWrapper(pFrame);
                    if (frameWrapper.IsEscherDocInXmlEditor)
                    {
                        // we have an EDMX file that is being opened in the XML editor so we want to validate
                        // so users can fix up safe-mode errors or even non-safe mode errors
                        VisualStudioEdmxValidator.LoadAndValidateFiles(frameWrapper.Uri);
                    }
                }
            }
            return NativeMethods.S_OK;
        }

        protected override void OnAfterSave()
        {
            base.OnAfterSave();

            _dirtyArtifactsOnClose?.Clear();
        }

        protected override void OnBeforeLastDesignerDocumentUnlock(Uri docUri)
        {
            if (CurrentArtifact is VSArtifact vsArtifact && vsArtifact.Uri == docUri
                && vsArtifact.LayerManager != null)
            {
                vsArtifact.LayerManager.Unload();
            }
        }

        public override int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            var hr = base.OnElementValueChanged(elementid, varValueOld, varValueNew);

            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_DocumentFrame)
            {
                if (varValueOld != null)
                {
                    EntityDesignFrameWrapper oldFrame = new EntityDesignFrameWrapper(varValueOld as IVsWindowFrame);
                    if (oldFrame.IsEscherDocInEntityDesigner)
                    {
                        VSArtifact oldVsArtifact = PackageManager.Package.ModelManager.GetArtifact(oldFrame.Uri) as VSArtifact;
                        oldVsArtifact?.LayerManager.Unload();
                    }
                }

                if (varValueNew != null)
                {
                    EntityDesignFrameWrapper newFrame = new EntityDesignFrameWrapper(varValueNew as IVsWindowFrame);
                    if (newFrame.IsEscherDocInEntityDesigner)
                    {
                        VSArtifact vsArtifact = PackageManager.Package.ModelManager.GetArtifact(newFrame.Uri) as VSArtifact;
                        vsArtifact?.LayerManager.Load();
                    }
                }
            }

            return hr;
        }
    }
}
