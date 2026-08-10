// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.Data.Entity.Design.VisualStudio.Model
{
    internal class VSDiagramArtifact : DiagramArtifact, IVsRunningDocTableEvents2
    {
        private bool _disabledBufferUndo;
        private uint _rdtCookie = VSConstants.VSCOOKIE_NIL;

        // <summary>
        //     Constructs a VSDiagramArtifact for the passed in URI
        // </summary>
        // <param name="modelManager">A reference of ModelManager</param>
        // <param name="uri">The Diagram File URI</param>
        // <param name="xmlModelProvider">If you pass null, then you must derive from this class and implement CreateModelProvider().</param>
        internal VSDiagramArtifact(ModelManager modelManager, Uri uri, XmlModelProvider xmlModelProvider)
            : base(modelManager, uri, xmlModelProvider)
        {
        }

        internal override void Init()
        {
            base.Init();

            Services.IVsRunningDocumentTable.AdviseRunningDocTableEvents(this, out _rdtCookie);
        }

        protected override void Dispose(bool disposing)
        {
            if (_rdtCookie != VSConstants.VSCOOKIE_NIL)
            {
                Services.IVsRunningDocumentTable.UnadviseRunningDocTableEvents(_rdtCookie);
            }

            base.Dispose(disposing);
        }

        #region IVsRunningDocTableEvents

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(
            uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew,
            uint itemidNew, string pszMkDocumentNew)
        {
            // We only need to worry about linked undo for artifacts that are not cached code gen models, since code gen artifacts don't exist
            // when the designer is open
            if (!IsCodeGenArtifact)
            {
                // First check to see if this is our document and it's being reloaded
                if (!_disabledBufferUndo
                    && (grfAttribs & (uint)__VSRDTATTRIB.RDTA_DocDataReloaded) == (uint)__VSRDTATTRIB.RDTA_DocDataReloaded)
                {
                    RunningDocumentTable rdt = new RunningDocumentTable(Services.ServiceProvider);
                    var rdi = rdt.GetDocumentInfo(docCookie);
                    if (rdi.Moniker.Equals(Uri.LocalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // DocData is XmlModelDocData
                        IVsTextBufferProvider textBufferProvider = rdi.DocData as IVsTextBufferProvider;
                        Debug.Assert(
                            textBufferProvider != null,
                            "The XML Model DocData over the diagram file is not IVsTextBufferProvider. Linked undo may not work correctly");
                        if (textBufferProvider != null)
                        {
                            var hr = textBufferProvider.GetTextBuffer(out IVsTextLines textLines);
                            Debug.Assert(
                                textLines != null,
                                "The IVsTextLines could not be found from the IVsTextBufferProvider. Linked undo may not work correctly");
                            if (NativeMethods.Succeeded(hr) && textLines != null)
                            {
                                hr = textLines.GetUndoManager(out IOleUndoManager bufferUndoMgr);

                                Debug.Assert(
                                    bufferUndoMgr != null, "Couldn't find the buffer undo manager. Linked undo may not work correctly");
                                if (NativeMethods.Succeeded(hr) && bufferUndoMgr != null)
                                {
                                    bufferUndoMgr.Enable(0);
                                    _disabledBufferUndo = true;
                                }
                            }
                        }
                    }
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
