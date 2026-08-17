// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using VsShell = Microsoft.VisualStudio.Shell.Interop;
using VsTextMgr = Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Data.Entity.Design.XmlEngine.Common;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Common
{

    /// <summary>
    /// A static utility class that manages interaction with the RDT for any part of the system that needs such services.
    /// </summary>
    /// <remarks>
    /// The Running Document Table (RDT) is the shell's registry of every document currently open in Visual Studio, whether or not
    /// a window frame is showing it. Almost every operation here is expressed in terms of one of three RDT identities: a document
    /// moniker (a full path), a document cookie (an opaque <see cref="uint" /> handle assigned by the RDT), and a
    /// hierarchy/itemid pair (the project and node that owns the document). These three must be kept consistent, because the shell
    /// will happily hand back a cookie for a document registered under an 8.3 short path that does not match the long path a
    /// caller is holding.
    /// <para>
    /// Lock and reference lifetimes are the other hazard. Document locks are counted, so every lock taken must be matched by
    /// exactly one unlock or the document stays open invisibly for the remainder of the VS session; likewise every
    /// <see cref="IntPtr" /> doc data returned by the RDT is AddRef'd on the caller's behalf and must be released. Individual
    /// members below document the specific pairing they require.
    /// </para>
    /// <para>
    /// The singleton caches its shell services at construction time, on the UI thread, so that the rest of the class can be
    /// consumed from any thread without further <c>GetService</c> calls.
    /// </para>
    /// </remarks>
    internal sealed class RdtManager : IDisposable
    {

        #region Fields

        /// <summary>
        /// Maps a document cookie to the number of outstanding "keep alive on close" requests for that document.
        /// </summary>
        /// <remarks>
        /// A document is present in this dictionary if and only if this class currently holds an <see cref="VsShell._VSRDTFLAGS.RDT_EditLock" />
        /// on it. The count exists so that nested or overlapping callers can each request the document be kept alive without the
        /// first release dropping the lock out from under the others.
        /// </remarks>
        private readonly Dictionary<uint /* docCookie */, int /* ref count */> _docDataToKeepAliveOnClose = [];

        /// <summary>
        /// Guards <see cref="_docDataToKeepAliveOnClose" /> and the RDT lock and unlock calls that accompany it.
        /// </summary>
        /// <remarks>
        /// The lock covers the RDT call as well as the dictionary mutation so the reference count and the actual RDT lock state can
        /// never disagree, even when requests arrive from more than one thread.
        /// </remarks>
        private readonly object _docDataToKeepAliveOnCloseLock = new object();

        /// <summary>
        /// The cached DTE automation object, used to reach the active document.
        /// </summary>
        private readonly _DTE _dte;

        /// <summary>
        /// Backing store for the <see cref="Instance" /> singleton.
        /// </summary>
        /// <remarks>
        /// Declared <c>volatile</c> because it participates in a double-checked locking pattern; the volatile read prevents another
        /// thread from observing a partially constructed instance.
        /// </remarks>
        private static volatile RdtManager _instance;

        /// <summary>
        /// The cached invisible editor manager, used to open documents into the RDT without creating a window frame.
        /// </summary>
        private readonly VsShell.IVsInvisibleEditorManager _invisibleEditorManager;

        /// <summary>
        /// Guards creation of the <see cref="_instance" /> singleton.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// The cached running document table service.
        /// </summary>
        private readonly VsShell.IVsRunningDocumentTable _runningDocumentTable;

        /// <summary>
        /// The cached UI shell service, used to enumerate open document window frames.
        /// </summary>
        private readonly VsShell.IVsUIShell _uiShell;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the process-wide <see cref="RdtManager" />, creating it on first access.
        /// </summary>
        /// <returns>The singleton <see cref="RdtManager" />.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown by the constructor if the first access happens off the UI thread, or if the UI shell service is unavailable.
        /// </exception>
        /// <remarks>
        /// Because construction must happen on the UI thread, callers that may first touch this property from a background thread
        /// should call <see cref="InitializeInstance" /> from the UI thread during startup instead of relying on lazy creation here.
        /// </remarks>
        internal static RdtManager Instance
        {
            get
            {
                if (_instance is null)
                {
                    lock (_lock)
                    {
                        if (_instance is null)
                        {
                            InitializeInstance();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RdtManager" /> class, caching the shell services it needs.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called off the UI thread, or when the UI shell service cannot be obtained.
        /// </exception>
        /// <remarks>
        /// <c>GetService</c> calls must be made on the UI thread. Resolving every service once, here, is what allows the rest of
        /// the class to be called from any thread afterwards.
        /// </remarks>
        private RdtManager()
        {
            // assert if we're not on the UI thread.  GetService calls must be made on the UI thread, so caching
            // these services lets this class be used on multiple threads.

            if (Application.MessageLoop == false)
            {
                Debug.Fail("Must create RdtManager on the UI Thread");
                throw new InvalidOperationException("Must create RdtManager on the UI Thread");
            }

            _invisibleEditorManager =
                global::Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(VsShell.SVsInvisibleEditorManager)) as VsShell.IVsInvisibleEditorManager;
            _runningDocumentTable = global::Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(VsShell.IVsRunningDocumentTable)) as VsShell.IVsRunningDocumentTable;
            _dte = global::Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(_DTE)) as _DTE;
            _uiShell = global::Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(VsShell.SVsUIShell)) as VsShell.IVsUIShell;
            if (_uiShell is null)
            {
                throw new InvalidOperationException("Could not get _uiShell!");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a reference requesting that the document's doc data be kept alive even after its last window frame closes.
        /// </summary>
        /// <param name="docCookie">The RDT cookie of the document to keep alive.</param>
        /// <exception cref="InvalidOperationException">Thrown when the singleton cannot be created on this thread.</exception>
        /// <remarks>
        /// The first reference takes an <see cref="VsShell._VSRDTFLAGS.RDT_EditLock" /> on the document; subsequent references only
        /// bump a count. Every call must be paired with exactly one call to
        /// <see cref="RemoveKeepDocDataAliveOnCloseReference(uint)" />, because the edit lock is what keeps the document in the RDT
        /// and an unmatched call leaks the document (and its buffer) for the life of the VS session.
        /// </remarks>
        public void AddKeepDocDataAliveOnCloseReference(uint docCookie)
        {
            lock (_docDataToKeepAliveOnCloseLock)
            {
                if (_docDataToKeepAliveOnClose.TryGetValue(docCookie, out int refCount))
                {
                    _docDataToKeepAliveOnClose[docCookie] = refCount + 1;
                }
                else
                {
                    // If this is the first request to keep the doc data alive on close, issue an edit lock to the RDT
                    // which will ensure the doc data stays alive.
                    _docDataToKeepAliveOnClose.Add(docCookie, 1);
                    GetRunningDocumentTable().LockDocument((uint)VsShell._VSRDTFLAGS.RDT_EditLock, docCookie);
                }
            }
        }

        /// <summary>
        /// Closes the frame.  Returns true
        /// if the editor was found.
        /// </summary>
        /// <param name="fullFileName">The full path of the document whose window frame should be closed.</param>
        /// <param name="foundAndClosed">
        /// When this method returns, contains a non-zero value if the document was found in the RDT and closed; otherwise zero.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown by <see cref="NativeMethods.ThrowOnFailure(int)" /> when the shell reports a failure other than a cancelled save prompt.
        /// </exception>
        /// <remarks>
        /// <see cref="VSConstants.OLE_E_PROMPTSAVECANCELLED" /> is deliberately swallowed: the user dismissing the "save changes?"
        /// prompt is a normal outcome, not an error, and <paramref name="foundAndClosed" /> already reports that nothing closed.
        /// </remarks>
        public void CloseFrame(string fullFileName, out int foundAndClosed)
        {
            foundAndClosed = 0;
            if (string.IsNullOrEmpty(fullFileName) == false)
            {
                var rdt2 = GetRunningDocumentTable2();
                if (rdt2 is not null)
                {
                    var hr = rdt2.QueryCloseRunningDocument(fullFileName, out foundAndClosed);
                    if (hr != VSConstants.OLE_E_PROMPTSAVECANCELLED)
                    {
                        NativeMethods.ThrowOnFailure(hr);
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that no documents are still being held open by this manager.
        /// </summary>
        /// <remarks>
        /// This does not release anything. Any remaining entry in <see cref="_docDataToKeepAliveOnClose" /> means a caller took a
        /// keep-alive reference and never released it, so the corresponding RDT edit lock is about to be orphaned; the assert is
        /// there to surface that leak in debug builds rather than silently paper over it here.
        /// </remarks>
        public void Dispose()
        {
            lock (_docDataToKeepAliveOnCloseLock)
            {
                Debug.Assert(
                    _docDataToKeepAliveOnClose.Keys.Count == 0,
                    "RdtManager is still trying to keep doc data alive on dispose, this could be a symptom of memory leak from invisible doc data.");
            }
        }

        /// <summary>
        /// Returns the currently active document or string.empty
        /// </summary>
        /// <returns>The full path of the active document, or <see cref="string.Empty" /> if there is no active document.</returns>
        public string GetActiveDocument()
        {
            var dte = _dte;
            if (dte is not null)
            {
                var document = dte.ActiveDocument;
                if (document is not null)
                {
                    return document.FullName;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Reads the entire contents of a text buffer as a single string.
        /// </summary>
        /// <param name="textLines">The text buffer to read; may be <see langword="null" />.</param>
        /// <returns>The full text of the buffer, or <see langword="null" /> if the buffer was <see langword="null" /> or could not be read.</returns>
        /// <remarks>
        /// Failures are reported as <see langword="null" /> rather than thrown, because callers use this opportunistically against
        /// buffers that may belong to editors which do not implement full line access.
        /// </remarks>
        public static string GetAllTextFromTextLines(VsTextMgr.IVsTextLines textLines)
        {
            string content = null;
            if (textLines is not null)
            {
                var result = textLines.GetLastLineIndex(out int line, out int column);
                if (result == VSConstants.S_OK)
                {
                    result = textLines.GetLineText(0, 0, line, column, out content);
                    if (result != VSConstants.S_OK)
                    {
                        content = null;
                    }
                }
            }
            return content;
        }

        /// <summary>
        /// Enumerates the open document windows and returns the paths of the dirty documents the caller cares about.
        /// </summary>
        /// <param name="shouldHandle">A predicate that receives a candidate document path and returns whether it should be included.</param>
        /// <returns>The full paths of the dirty documents accepted by <paramref name="shouldHandle" />.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="shouldHandle" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the shell reports a failure while enumerating window frames.</exception>
        /// <remarks>
        /// This walks window frames rather than the RDT itself, so documents held open only by an invisible editor are not
        /// considered. Editors that return <see cref="VSConstants.E_NOTIMPL" /> from <c>IsDirty</c> (the binary editor, for one)
        /// are skipped instead of being treated as an error.
        /// </remarks>
        public List<string> GetDirtyFiles(Predicate<string> shouldHandle)
        {
            ArgumentValidation.CheckForNullReference(shouldHandle, "shouldHandle");

            List<string> dirtyFiles = new List<string>();
            var rdt = Instance.GetRunningDocumentTable();
            if (rdt is not null)
            {
                // Get the UI Shell
                var uiShell = _uiShell;

                // Go through the open documents and find it
                _ = ErrorHandler.ThrowOnFailure(uiShell.GetDocumentWindowEnum(out VsShell.IEnumWindowFrames windowFramesEnum));
                IVsWindowFrame[] windowFrames = new VsShell.IVsWindowFrame[1];
                while (windowFramesEnum.Next(1, windowFrames, out uint fetched) == VSConstants.S_OK
                       && fetched == 1)
                {
                    var windowFrame = windowFrames[0];
                    _ = ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)VsShell.__VSFPROPID.VSFPROPID_DocData, out object data));

                    if (data is VsShell.IPersistFileFormat fileFormat)
                    {
                        // The binary editor returns notimpl for IsDirty so just continue if
                        // the interface returns E_NOTIMPL
                        var hr = fileFormat.IsDirty(out int dirty);
                        if (hr == VSConstants.E_NOTIMPL)
                        {
                            continue;
                        }

                        _ = ErrorHandler.ThrowOnFailure(hr);
                        if (dirty == 1)
                        {
                            _ = NativeMethods.ThrowOnFailure(fileFormat.GetCurFile(out string candidateFilename, out uint _));
                            if (string.IsNullOrEmpty(candidateFilename) == false
                                &&
                                shouldHandle(candidateFilename))
                            {
                                dirtyFiles.Add(candidateFilename);
                            }
                        }
                    }
                }
            }

            return dirtyFiles;
        }

        /// <summary>
        /// Gets the DocData object from the RDT for the specified fullPath filename
        /// You might want to try casting this to an IVsPersistDocData2, but that may not work for all filetypes
        /// Returns 0 if the file is not in the RDT
        /// </summary>
        /// <param name="fullPathFileName">the fullpath filename whose docCookie is wanted</param>
        /// <returns>the docData of the specified file</returns>
        /// <remarks>
        /// The lookup is performed with <see cref="VsShell._VSRDTFLAGS.RDT_NoLock" />, so no unlock is required; the native doc data
        /// pointer that the RDT AddRef'd is converted to a managed object and released here, leaving the caller with an ordinary
        /// runtime callable wrapper and no additional cleanup obligation.
        /// </remarks>
        public static Object GetDocData(string fullPathFileName)
        {
            var ppunkDocData = IntPtr.Zero;
            Object returnObj = null;
            var rdt = Instance.GetRunningDocumentTable();
            if (rdt is not null)
            {

                try
                {
                    try
                    {
                        if (Path.IsPathRooted(fullPathFileName))
                        {
                            // Canonicalize the file path
                            // Use to get rid of 8.3 filenames, otherwise this'll fail
                            fullPathFileName = Path.GetFullPath(fullPathFileName);
                        }
                    }
                    catch (ArgumentException)
                    {
                    }

                    FindAndLockDocument(
                        (uint)(VsShell._VSRDTFLAGS.RDT_NoLock),
                        fullPathFileName,
                        out IVsHierarchy ppHier,
                        out uint pItemId,
                        out ppunkDocData,
                        out uint result);
                }
                finally
                {
                    if (ppunkDocData != IntPtr.Zero)
                    {
                        returnObj = Marshal.GetObjectForIUnknown(ppunkDocData);
                        Marshal.Release(ppunkDocData);
                    }
                }
            }
            return returnObj;
        }

        /// <summary>
        /// Returns the hierarchy for this file on the rdt
        /// </summary>
        /// <param name="docCookie">The RDT cookie of the document whose owning hierarchy is wanted.</param>
        /// <returns>The <see cref="VsShell.IVsHierarchy" /> that owns the document.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the RDT does not recognise <paramref name="docCookie" />.</exception>
        /// <remarks>
        /// <c>GetDocumentInfo</c> AddRefs the doc data it hands back even though only the hierarchy is wanted here, so the pointer is
        /// released in the <c>finally</c> block; skipping that would keep the document's doc data alive indefinitely.
        /// </remarks>
        public VsShell.IVsHierarchy GetHierarchyFromDocCookie(uint docCookie)
        {
            VsShell.IVsHierarchy ppHier;
            var ppunkDocData = IntPtr.Zero;

            try
            {
                NativeMethods.ThrowOnFailure(
                    _runningDocumentTable.GetDocumentInfo(
                        docCookie, out uint pgrfRDTFlags, out uint pdwReadLocks, out uint pdwEditLocks, out string pbstrMkDocument, out ppHier, out uint pitemid,
                        out ppunkDocData));
            }
            finally
            {
                if (ppunkDocData != IntPtr.Zero)
                {
                    Marshal.Release(ppunkDocData);
                }
            }
            return ppHier;
        }

        /// <summary>
        /// Get the running document table.
        /// </summary>
        /// <returns>
        /// The running document table for this run of VS.
        /// </returns>
        public VsShell.IVsRunningDocumentTable GetRunningDocumentTable()
        {
            return _runningDocumentTable;
        }

        /// <summary>
        /// Get the running document table.
        /// </summary>
        /// <returns>
        /// The running document table for this run of VS.
        /// </returns>
        /// <remarks>
        /// Returns <see langword="null" /> if the cached service does not implement the version 2 interface, so callers must check
        /// before use.
        /// </remarks>
        public VsShell.IVsRunningDocumentTable2 GetRunningDocumentTable2()
        {
            return _runningDocumentTable as VsShell.IVsRunningDocumentTable2;
        }

        /// <summary>
        /// Get IVsTextLines from a file.  If that file is in RDT, get text buffer from it.
        /// If the file is not in RDT, open that file in invisible editor and get text buffer
        /// from it.
        /// If failed to get text buffer, it will return null.
        /// </summary>
        /// <param name="fullPathFileName">File name with full path.</param>
        /// <returns>Text buffer for that file.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the RDT lookup itself fails.</exception>
        /// <remarks>
        /// This method created for refactoring usage, refactoring will work on all kinds of
        /// docdata, not only SqlEditorDocData.  If change this method, please get let LiangZ
        /// know.
        /// <para>
        /// The RDT lookup uses <see cref="VsShell._VSRDTFLAGS.RDT_NoLock" /> so there is no lock to release, and the AddRef'd doc
        /// data pointer is released in the <c>finally</c> block. When the file has to be opened in an invisible editor instead, that
        /// editor is released before returning: the returned buffer is only guaranteed to remain valid while some other party
        /// keeps the document open.
        /// </para>
        /// </remarks>
        public VsTextMgr.IVsTextLines GetTextLines(string fullPathFileName)
        {
            VsTextMgr.IVsTextLines textLines = null;

            var rdt = Instance.GetRunningDocumentTable();
            if (rdt is not null)
            {
                var ppunkDocData = IntPtr.Zero;
                try
                {
                    NativeMethods.ThrowOnFailure(
                        FindAndLockDocument(
                            (uint)(VsShell._VSRDTFLAGS.RDT_NoLock),
                            fullPathFileName,
                            out IVsHierarchy ppHier,
                            out uint pitemid,
                            out ppunkDocData,
                            out uint pdwCookie));
                    if (pdwCookie != 0)
                    {
                        if (ppunkDocData != IntPtr.Zero)
                        {
                            try
                            {
                                // Get text lines from the doc data
                                textLines = Marshal.GetObjectForIUnknown(ppunkDocData) as VsTextMgr.IVsTextLines;
                            }
                            catch (ArgumentException)
                            {
                                // Do nothing here, it will return null stream at the end.
                            }
                        }
                    }
                    else
                    {
                        // The file is not in RDT, open it in invisible editor and get the text lines from it.
                        VsShell.IVsInvisibleEditor invisibleEditor = null;
                        try
                        {
                            TryGetTextLinesAndInvisibleEditor(fullPathFileName, out invisibleEditor, out textLines);
                        }
                        finally
                        {
                            if (invisibleEditor is not null)
                            {
                                Marshal.ReleaseComObject(invisibleEditor);
                            }
                        }
                    }
                }
                finally
                {
                    if (ppunkDocData != IntPtr.Zero)
                    {
                        Marshal.Release(ppunkDocData);
                    }
                }
            }
            return textLines;
        }

        /// <summary>
        /// Returns the window frame for our document window
        /// </summary>
        /// <param name="fullFileName">The full path of the document whose window frame is wanted.</param>
        /// <returns>
        /// The IWindowFrame for our open document.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the shell reports a failure while enumerating window frames.</exception>
        /// <remarks>
        /// Matching is done on the document moniker reported by each frame's doc data, first with a case-insensitive comparison and
        /// then with <see cref="FileUtils.IsSamePath(string, string)" />, because the shell may have registered the document under a
        /// short (8.3) or otherwise non-canonical path that will not compare equal to the caller's path.
        /// </remarks>
        public VsShell.IVsWindowFrame GetWindowFrame(string fullFileName)
        {
            if (string.IsNullOrEmpty(fullFileName))
            {
                return null;
            }

            VsShell.IVsWindowFrame foundFrame = null;

            var rdt = Instance.GetRunningDocumentTable();
            if (rdt is not null)
            {
                // Get the UI Shell
                var uiShell = _uiShell;

                // Go through the open documents and find it
                ErrorHandler.ThrowOnFailure(uiShell.GetDocumentWindowEnum(out IEnumWindowFrames windowFramesEnum));
                IVsWindowFrame[] windowFrames = new VsShell.IVsWindowFrame[1];
                var thisFilename = fullFileName;

                while (windowFramesEnum.Next(1, windowFrames, out uint fetched) == VSConstants.S_OK
                       && fetched == 1)
                {
                    var windowFrame = windowFrames[0];
                    ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)VsShell.__VSFPROPID.VSFPROPID_DocData, out object data));

                    if (data is VsShell.IPersistFileFormat fileFormat)
                    {

                        NativeMethods.ThrowOnFailure(fileFormat.GetCurFile(out string candidateFilename, out uint formatIndex));
                        if (string.IsNullOrEmpty(candidateFilename) == false
                            &&
                            (string.Compare(candidateFilename, thisFilename, true, CultureInfo.CurrentCulture) == 0 ||
                             FileUtils.IsSamePath(candidateFilename, thisFilename)))
                        {
                            // Found it
                            foundFrame = windowFrame;
                            break;
                        }
                    }
                }
            }
            return foundFrame;
        }

        /// <summary>
        /// Is this doc data dirty?
        /// </summary>
        /// <param name="docData">The doc data object to test; may be any type.</param>
        /// <returns>True if the doc data is dirty, false if not or if it does not support persistence.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the doc data reports a failure from <c>IsDocDataDirty</c>.</exception>
        /// <remarks>
        /// Doc data that does not implement <see cref="VsShell.IVsPersistDocData" /> is reported as clean rather than as an error,
        /// since there is nothing for it to persist.
        /// </remarks>
        public static bool IsDirty(object docData)
        {
            if (docData is VsShell.IVsPersistDocData persistDocData)
            {
                NativeMethods.ThrowOnFailure(persistDocData.IsDocDataDirty(out int dirty));

                return (dirty != 0);
            }

            return false;
        }

        /// <summary>
        /// Is this document dirty?
        /// </summary>
        /// <param name="docFullPath">The full path to the document</param>
        /// <returns>True if document is dirty, false if not</returns>
        /// <exception cref="InvalidOperationException">Thrown when the doc data reports a failure from <c>IsDocDataDirty</c>.</exception>
        /// <remarks>
        /// A document that is not open in the RDT is reported as clean, since there are no unsaved in-memory changes to lose.
        /// </remarks>
        public static bool IsDirty(string docFullPath)
        {
            if (GetDocData(docFullPath) is VsShell.IVsPersistDocData docData)
            {
                NativeMethods.ThrowOnFailure(docData.IsDocDataDirty(out int dirty));

                return (dirty != 0);
            }
            return false;
        }

        /// <summary>
        /// Is the document behind this cookie dirty?
        /// </summary>
        /// <param name="docCookie">The RDT cookie of the document to test.</param>
        /// <returns>True if the document is dirty, false if not or if the cookie is not in the RDT.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the doc data reports a failure from <c>IsDocDataDirty</c>.</exception>
        public static bool IsDirty(uint docCookie)
        {
            if (TryGetDocDataFromCookie(docCookie, out object docData))
            {
                return IsDirty(docData);
            }
            return false;
        }

        /// <summary>
        /// Checks if a given file is in the RDT
        /// </summary>
        /// <param name="fullPathFileName">fullpath filename of the file in question</param>
        /// <returns>bool: IsFileInRdt</returns>
        public static bool IsFileInRdt(string fullPathFileName)
        {
            return !String.IsNullOrEmpty(fullPathFileName)
                   && (GetRdtCookie(fullPathFileName) != 0);
        }

        /// <summary>
        /// Get string content of a file.
        /// </summary>
        /// <param name="fullPathFileName">File name with full path.</param>
        /// <returns>Content of that file in string format.</returns>
        /// <remarks>
        /// This method created for refactoring usage, refactoring will work on all kinds of
        /// docdata, not only SqlEditorDocData.  If change this method, please get let LiangZ
        /// know.
        /// <para>
        /// Reading through the RDT rather than off disk matters because the in-memory buffer may hold unsaved edits that the file on
        /// disk does not. When the document is not already open, the invisible editor registered to read it is released in the
        /// <c>finally</c> block so the document does not stay in the RDT afterwards.
        /// </para>
        /// </remarks>
        public string ReadFromFile(string fullPathFileName)
        {
            string content;
            VsShell.IVsInvisibleEditor invisibleEditor = null;
            VsTextMgr.IVsTextLines textLines;
            try
            {
                if (IsFileInRdt(fullPathFileName))
                {
                    // File is in RDT
                    textLines = GetTextLines(fullPathFileName);
                }
                else
                {
                    // File is not in RDT, open it in invisble editor.
                    if (!TryGetTextLinesAndInvisibleEditor(fullPathFileName, out invisibleEditor, out textLines))
                    {
                        // Failed to get text lines or invisible editor.
                        textLines = null;
                    }
                }
                content = GetAllTextFromTextLines(textLines);
            }
            finally
            {
                // Close invisible editor from RDT
                if (invisibleEditor is not null)
                {
                    Marshal.ReleaseComObject(invisibleEditor);
                }
            }
            return content;
        }

        /// <summary>
        /// Releases one reference previously taken by <see cref="AddKeepDocDataAliveOnCloseReference(uint)" />.
        /// </summary>
        /// <param name="docCookie">The RDT cookie of the document to stop keeping alive.</param>
        /// <remarks>
        /// When the last reference is released, the <see cref="VsShell._VSRDTFLAGS.RDT_EditLock" /> taken by the first
        /// <see cref="AddKeepDocDataAliveOnCloseReference(uint)" /> is unlocked, which is what finally allows the document to leave
        /// the RDT. Calls for a cookie with no outstanding references are ignored, so an extra release cannot unbalance the lock
        /// count in the other direction.
        /// </remarks>
        public void RemoveKeepDocDataAliveOnCloseReference(uint docCookie)
        {
            lock (_docDataToKeepAliveOnCloseLock)
            {
                if (_docDataToKeepAliveOnClose.TryGetValue(docCookie, out int refCount))
                {
                    if (refCount == 1)
                    {
                        // If this was the last request to keep the doc data alive on close, remove the edit lock.
                        _docDataToKeepAliveOnClose.Remove(docCookie);
                        GetRunningDocumentTable().UnlockDocument((uint)VsShell._VSRDTFLAGS.RDT_EditLock, docCookie);
                    }
                    else
                    {
                        _docDataToKeepAliveOnClose[docCookie] = refCount - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Save the file if it is in the RDT and dirty.
        /// </summary>
        /// <param name="fullFilePath">The file to save</param>
        /// <exception cref="InvalidOperationException">Thrown when the document's doc data fails or cancels the save.</exception>
        public void SaveDirtyFile(string fullFilePath)
        {
            if (string.IsNullOrEmpty(fullFilePath) == false)
            {
                IList<string> dirtyFiles = [fullFilePath];
                SaveDirtyFiles(dirtyFiles);
            }
        }

        /// <summary>
        /// Save all dirty files.
        /// </summary>
        /// <param name="dirtyFiles">A list of dirty files to save.  These must be full path.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dirtyFiles" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a document's doc data fails or cancels the save.</exception>
        /// <remarks>
        /// The save is bracketed by <c>NotifyOnBeforeSave</c> and <c>NotifyOnAfterSave</c> so that RDT event sinks (source control
        /// glyph updates, file change tracking, and so on) see the same notifications they would for a save driven by the shell.
        /// Files that are not open in the RDT are skipped, since there is nothing in memory to flush.
        /// </remarks>
        public void SaveDirtyFiles(IList<string> dirtyFiles)
        {
            ArgumentValidation.CheckForNullReference(dirtyFiles, "dirtyFiles");

            var rdt = _runningDocumentTable;

            var fileCount = dirtyFiles.Count;
            for (var fileIndex = 0; fileIndex < fileCount; fileIndex++)
            {
                var filePath = dirtyFiles[fileIndex];
                if (GetDocData(filePath) is VsShell.IVsPersistDocData docData)
                {
                    var cookie = GetRdtCookie(filePath);
                    _ = NativeMethods.ThrowOnFailure(rdt.NotifyOnBeforeSave(cookie));
                    var result = docData.SaveDocData(VsShell.VSSAVEFLAGS.VSSAVE_Save, out _, out int cancelled);
                    if (result != VSConstants.S_OK
                        || cancelled != 0)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.Exception_FailedToSaveFile, filePath));
                    }
                    _ = NativeMethods.ThrowOnFailure(rdt.NotifyOnAfterSave(cookie));
                }
            }
        }

        /// <summary>
        /// Saves all the dirty files that pass the predicate
        /// </summary>
        /// <param name="shouldSave">A predicate that receives a dirty document's path and returns whether it should be saved.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="shouldSave" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a document's doc data fails or cancels the save.</exception>
        public void SaveDirtyFiles(Predicate<string> shouldSave)
        {
            ArgumentValidation.CheckForNullReference(shouldSave, "shouldSave");

            SaveDirtyFiles(GetDirtyFiles(shouldSave));
        }

        /// <summary>
        /// Sets the focus to the active document, if there is one.
        /// </summary>
        public void SetFocusToActiveDocument()
        {
            var dte = _dte;
            if (dte is not null)
            {
                var document = dte.ActiveDocument;
                document?.Activate();
            }
        }

        /// <summary>
        /// Determines whether this manager is currently holding the document open past the close of its last window.
        /// </summary>
        /// <param name="docCookie">The RDT cookie of the document to test.</param>
        /// <returns>True if at least one keep-alive reference is outstanding for the document, false otherwise.</returns>
        /// <remarks>
        /// Editors consult this while handling a close so they can leave the doc data intact instead of tearing it down; the answer
        /// is equivalent to asking whether this class currently holds an RDT edit lock on the document.
        /// </remarks>
        public bool ShouldKeepDocDataAliveOnClose(uint docCookie)
        {
            bool shouldKeepAlive;
            lock (_docDataToKeepAliveOnCloseLock)
            {
                shouldKeepAlive = _docDataToKeepAliveOnClose.ContainsKey(docCookie);
            }

            return shouldKeepAlive;
        }

        /// <summary>
        /// Returns the docdata for this cookie on the rdt
        /// </summary>
        /// <param name="cookie">The RDT cookie of the document whose doc data is wanted.</param>
        /// <param name="docData">When this method returns, contains the doc data object, or <see langword="null" /> if the lookup failed.</param>
        /// <returns>True if the doc data was retrieved, false otherwise.</returns>
        /// <remarks>
        /// <c>GetDocumentInfo</c> does not take a document lock, so nothing needs to be unlocked, but it does AddRef the doc data
        /// pointer; that reference is consumed by the marshalling here and released immediately, leaving the caller with a plain
        /// managed reference.
        /// </remarks>
        public static bool TryGetDocDataFromCookie(uint cookie, out object docData)
        {
            docData = null;

            var rdt = Instance.GetRunningDocumentTable();
            Debug.Assert(rdt is not null);
            if (rdt is not null)
            {

                var hr = rdt.GetDocumentInfo(
                    cookie,
                    out uint rdtFlags,
                    out uint readLocks,
                    out uint editLocks,
                    out string itemName,
                    out IVsHierarchy hierarchy,
                    out uint itemId,
                    out IntPtr unknownDocData);
                if (NativeMethods.Succeeded(hr))
                {
                    docData = Marshal.GetObjectForIUnknown(unknownDocData);
                    Marshal.Release(unknownDocData);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Open the file in invisible editor in RDT, and get text buffer from it.
        /// </summary>
        /// <param name="fullPathFileName">File name with full path.</param>
        /// <param name="spEditor">The result invisible editor.</param>
        /// <param name="textLines">The result text buffer.</param>
        /// <returns>True, if the file is opened correctly in invisible editor.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the invisible editor could not be registered.</exception>
        /// <remarks>
        /// The document is registered with no owning hierarchy; use the overload that takes an
        /// <see cref="VsShell.IVsProject" /> when the owning project is known. The caller takes ownership of
        /// <paramref name="spEditor" /> and must release it with <see cref="Marshal.ReleaseComObject(object)" /> once finished,
        /// because the invisible editor is what holds the document in the RDT; failing to release it leaves the document open with
        /// no window to reveal that fact.
        /// </remarks>
        public bool TryGetTextLinesAndInvisibleEditor(
            string fullPathFileName, out VsShell.IVsInvisibleEditor spEditor, out VsTextMgr.IVsTextLines textLines)
        {
            return TryGetTextLinesAndInvisibleEditor(fullPathFileName, null, out spEditor, out textLines);
        }

        /// <summary>
        /// Open the file in invisible editor in RDT, and get text buffer from it.
        /// </summary>
        /// <param name="fullPathFileName">File name with full path.</param>
        /// <param name="project">the hierarchy for the document</param>
        /// <param name="spEditor">The result invisible editor.</param>
        /// <param name="textLines">The result text buffer.</param>
        /// <returns>True, if the file is opened correctly in invisible editor.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the invisible editor could not be registered.</exception>
        /// <remarks>
        /// This method created for refactoring usage, refactoring will work on all kinds of
        /// docdata, not only SqlEditorDocData.  If change this method, please get let LiangZ
        /// know.
        /// <para>
        /// The caller takes ownership of <paramref name="spEditor" /> and must release it with
        /// <see cref="Marshal.ReleaseComObject(object)" /> once finished, even when this method returns false but still produced an
        /// editor: the invisible editor is the only thing holding the document in the RDT, so an unreleased editor keeps the
        /// document open invisibly for the rest of the session. Passing <paramref name="project" /> associates the document with the
        /// correct hierarchy, which the shell needs in order to apply the project's editor and source control behaviour.
        /// </para>
        /// <para>
        /// The doc data pointer returned by <c>GetDocData</c> is AddRef'd and is released here; <paramref name="textLines" /> remains
        /// valid for as long as the editor is held.
        /// </para>
        /// </remarks>
        public bool TryGetTextLinesAndInvisibleEditor(
            string fullPathFileName, VsShell.IVsProject project, out VsShell.IVsInvisibleEditor spEditor,
            out VsTextMgr.IVsTextLines textLines)
        {
            spEditor = null;
            textLines = null;

            // Need to open this file.  Use the invisible editor manager to do so.
            VsShell.IVsInvisibleEditorManager invisibleEditorMgr;
            var ppDocData = IntPtr.Zero;
            bool result;

            var iidIVsTextLines = typeof(VsTextMgr.IVsTextLines).GUID;

            try
            {
                invisibleEditorMgr = _invisibleEditorManager;

                NativeMethods.ThrowOnFailure(
                    invisibleEditorMgr.RegisterInvisibleEditor(
                        fullPathFileName, project, (uint)VsShell._EDITORREGFLAGS.RIEF_ENABLECACHING, null, out spEditor));
                if (spEditor is not null)
                {
                    var hr = spEditor.GetDocData(0, ref iidIVsTextLines, out ppDocData);
                    if (hr == VSConstants.S_OK
                        && ppDocData != IntPtr.Zero)
                    {
                        textLines = Marshal.GetTypedObjectForIUnknown(ppDocData, typeof(VsTextMgr.IVsTextLines)) as VsTextMgr.IVsTextLines;
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
                else
                {
                    result = false;
                }
            }
            finally
            {
                if (ppDocData != IntPtr.Zero)
                {
                    Marshal.Release(ppDocData);
                }
            }

            return result;
        }

        /// <summary>
        /// Refresh a file content with passed in string.  After change, the file will not be saved.
        /// </summary>
        /// <param name="fullPathFileName">File name with full path.</param>
        /// <param name="content">string content need to update the file with.</param>
        /// <returns>True if the content was written to the document buffer, false otherwise.</returns>
        /// <remarks>
        /// The document is left dirty in the RDT, so the change is visible to anything reading through this class but is not yet on
        /// disk.
        /// </remarks>
        public bool WriteToFile(string fullPathFileName, string content)
        {
            return WriteToFile(fullPathFileName, content, false);
        }

        /// <summary>
        /// Refresh a file content with passed in string.  Save or not save the file according to
        /// passed in option.
        /// </summary>
        /// <param name="fullPathFileName">File name with full path.</param>
        /// <param name="content">string content need to update the file with.</param>
        /// <param name="saveFile">Save file or not after writing.</param>
        /// <returns>True if the content was written to the document buffer, false otherwise.</returns>
        /// <remarks>
        /// This method created for refactoring usage, refactoring will work on all kinds of
        /// docdata, not only SqlEditorDocData.  If change this method, please get let LiangZ
        /// know.
        /// </remarks>
        public bool WriteToFile(string fullPathFileName, string content, bool saveFile)
        {
            return WriteToFile(fullPathFileName, content, saveFile, false);
        }

        /// <summary>
        /// Refresh a file content with passed in string.  Save or not save the file according to
        /// passed in option.
        /// </summary>
        /// <param name="fullPathFileName">File name with full path.</param>
        /// <param name="content">string content need to update the file with.</param>
        /// <param name="saveFile">Save file or not after writing.</param>
        /// <param name="createIfNotExist">Creates the file if it doesn't exist.</param>
        /// <returns>True if the content was written, false if the document buffer could not be obtained or updated.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the file has to be created and the creation fails, or when a requested save fails.
        /// </exception>
        /// <remarks>
        /// This method created for refactoring usage, refactoring will work on all kinds of
        /// docdata, not only SqlEditorDocData.  If change this method, please get let LiangZ
        /// know.
        /// <para>
        /// When the file has to be created, the content is written straight to disk and the buffer path is skipped entirely, since
        /// there is no document to reload. Otherwise the write goes through <c>ReloadLines</c> on the document's buffer so that any
        /// editor already showing the document sees the new content rather than being left with a stale buffer that would overwrite
        /// the file on its next save.
        /// </para>
        /// <para>
        /// If the document was not already open, the invisible editor registered to reach its buffer is released in the
        /// <c>finally</c> block so the document does not stay in the RDT.
        /// </para>
        /// </remarks>
        public bool WriteToFile(string fullPathFileName, string content, bool saveFile, bool createIfNotExist)
        {
            var succeed = true;
            VsShell.IVsInvisibleEditor invisibleEditor = null;
            VsTextMgr.IVsTextLines textLines = null;
            var fileDidNotExistAndShouldBeCreated = false;

            try
            {
                if (IsFileInRdt(fullPathFileName))
                {
                    // File is in RDT
                    textLines = GetTextLines(fullPathFileName);
                }
                else
                {
                    if (createIfNotExist &&
                        File.Exists(fullPathFileName) == false)
                    {
                        fileDidNotExistAndShouldBeCreated = true;

                        FileStream fileStream = null;
                        StreamWriter writer = null;
                        try
                        {
                            fileStream = File.Create(fullPathFileName);
                            // Make the newly created file unicode
                            writer = new StreamWriter(fileStream, Encoding.UTF8);

                            writer.Write(content);
                        }
                        catch (IOException e)
                        {
                            throw new InvalidOperationException(e.Message);
                        }
                        catch (ArgumentException e)
                        {
                            throw new InvalidOperationException(e.Message);
                        }
                        finally
                        {
                            writer?.Close();
                            fileStream?.Close();
                        }
                    }

                    // File is not in RDT, open it in invisible editor.
                    if (fileDidNotExistAndShouldBeCreated == false
                        && !TryGetTextLinesAndInvisibleEditor(fullPathFileName, out invisibleEditor, out textLines))
                    {
                        // Failed to get text lines or invisible editor.
                        textLines = null;
                    }
                }

                if (fileDidNotExistAndShouldBeCreated == false)
                {
                    if (textLines is not null)
                    {
                        var result = textLines.GetLastLineIndex(out int line, out int column);
                        if (result == VSConstants.S_OK)
                        {
                            var pContent = IntPtr.Zero;
                            try
                            {
                                // Copy the content to textLines.
                                pContent = Marshal.StringToHGlobalAuto(content);
                                result = textLines.ReloadLines(
                                    0, 0, line, column, pContent, content.Length,
                                    new[] { new VsTextMgr.TextSpan() });
                                if (result != VSConstants.S_OK)
                                {
                                    succeed = false;
                                }
                            }
                            finally
                            {
                                if (pContent != IntPtr.Zero)
                                {
                                    Marshal.FreeHGlobal(pContent);
                                }
                            }
                            if (saveFile)
                            {
                                List<string> list = new List<string> { fullPathFileName };
                                Instance.SaveDirtyFiles(list);
                            }
                        }
                        else
                        {
                            succeed = false;
                        }
                    }
                    else
                    {
                        succeed = false;
                    }
                }
            }
            finally
            {
                // Close invisible editor from RDT
                if (invisibleEditor is not null)
                {
                    Marshal.ReleaseComObject(invisibleEditor);
                }
            }
            return succeed;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the DocCookie from the RDT for the specified fullPath FileName
        /// Returns 0 if the file is not in the RDT
        /// </summary>
        /// <param name="fullPathFileName">the fullpath filename whose docCookie is wanted</param>
        /// <returns>the DocCookie of the specified file</returns>
        /// <remarks>
        /// The path is canonicalized first because the RDT keys documents by exact moniker, so an 8.3 short path will not match a
        /// document registered under its long path. The lookup uses <see cref="VsShell._VSRDTFLAGS.RDT_NoLock" />, so the returned
        /// cookie carries no lock and requires no matching unlock; the AddRef'd doc data pointer is released here.
        /// </remarks>
        internal static uint GetRdtCookie(string fullPathFileName)
        {
            uint result = 0;
            var rdt = Instance.GetRunningDocumentTable();
            if (rdt is not null)
            {
                var ppunkDocData = IntPtr.Zero;

                try
                {
                    try
                    {
                        if (Path.IsPathRooted(fullPathFileName))
                        {
                            // Canonicalize the file path
                            // Use to get rid of 8.3 filenames, otherwise this'll fail
                            fullPathFileName = Path.GetFullPath(fullPathFileName);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Contains invalid path characters - for instance a file-less moniker
                    }

                    FindAndLockDocument(
                        (uint)(VsShell._VSRDTFLAGS.RDT_NoLock),
                        fullPathFileName,
                        out IVsHierarchy ppHier,
                        out uint pItemId,
                        out ppunkDocData,
                        out result);
                }
                finally
                {
                    if (ppunkDocData != IntPtr.Zero)
                    {
                        Marshal.Release(ppunkDocData);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Creates the <see cref="Instance" /> singleton if it does not already exist.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called off the UI thread, or when the UI shell service cannot be obtained.
        /// </exception>
        /// <remarks>
        /// Exposed separately from <see cref="Instance" /> so that a package can force construction from the UI thread during
        /// initialization, guaranteeing that later access from a background thread never trips the UI thread requirement in the
        /// constructor.
        /// </remarks>
        internal static void InitializeInstance()
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new RdtManager();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This routine returns the document from the RDT.  We wrote this routine because
        /// sometimes 8.3 filenames are used and other times long filenames, so this routine
        /// tries and, if it fails, canonicalizes the path and tries again.
        /// </summary>
        /// <param name="rdtLockType">The <see cref="VsShell._VSRDTFLAGS" /> lock to take on the document.</param>
        /// <param name="fullPathFileName">The document moniker to look up.</param>
        /// <param name="ppHier">When this method returns, contains the hierarchy that owns the document, or <see langword="null" />.</param>
        /// <param name="pitemid">When this method returns, contains the itemid of the document within <paramref name="ppHier" />, or zero.</param>
        /// <param name="ppunkDocData">When this method returns, contains an AddRef'd pointer to the document's doc data, or <see cref="IntPtr.Zero" />.</param>
        /// <param name="pdwCookie">When this method returns, contains the document's RDT cookie, or zero if it is not in the RDT.</param>
        /// <returns>The HRESULT from the RDT; <see cref="VSConstants.S_FALSE" /> if the running document table was unavailable.</returns>
        /// <remarks>
        /// Every caller is responsible for two things. If <paramref name="rdtLockType" /> is anything other than
        /// <see cref="VsShell._VSRDTFLAGS.RDT_NoLock" />, the lock must later be released with a matching
        /// <c>IVsRunningDocumentTable.UnlockDocument</c> call using the same flags and <paramref name="pdwCookie" />, or the
        /// document stays open invisibly. Separately, a non-zero <paramref name="ppunkDocData" /> has been AddRef'd on the caller's
        /// behalf and must be released with <see cref="Marshal.Release(IntPtr)" /> (or consumed by a marshalling call that takes over
        /// the reference). All callers in this class pass <see cref="VsShell._VSRDTFLAGS.RDT_NoLock" />.
        /// <para>
        /// The retry exists because the RDT keys documents by exact moniker: a relative or short (8.3) path will not match a
        /// document registered under its canonical long path, so a failed lookup is retried against
        /// <see cref="Path.GetFullPath(string)" />. Path exceptions from that canonicalization are swallowed, leaving the original
        /// failure HRESULT as the result.
        /// </para>
        /// </remarks>
        private static int FindAndLockDocument(
            uint rdtLockType,
            string fullPathFileName,
            out VsShell.IVsHierarchy ppHier,
            out uint pitemid,
            out IntPtr ppunkDocData,
            out uint pdwCookie)
        {
            var hr = VSConstants.S_FALSE;
            var rdt = Instance.GetRunningDocumentTable();

            ppHier = null;
            pitemid = 0;
            ppunkDocData = IntPtr.Zero;
            pdwCookie = 0;

            if (rdt is not null)
            {
                hr = rdt.FindAndLockDocument(
                    rdtLockType,
                    fullPathFileName,
                    out ppHier,
                    out pitemid,
                    out ppunkDocData,
                    out pdwCookie);

                if (NativeMethods.Failed(hr))
                {
                    try
                    {
                        if (Path.IsPathRooted(fullPathFileName) == false)
                        {
                            fullPathFileName = Path.GetFullPath(fullPathFileName);

                            // Canonicalize the file path
                            // Use to get rid of 8.3 filenames, otherwise this'll fail
                            hr = rdt.FindAndLockDocument(
                                rdtLockType,
                                fullPathFileName,
                                out ppHier,
                                out pitemid,
                                out ppunkDocData,
                                out pdwCookie);
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            return hr;
        }

        #endregion

    }

}
