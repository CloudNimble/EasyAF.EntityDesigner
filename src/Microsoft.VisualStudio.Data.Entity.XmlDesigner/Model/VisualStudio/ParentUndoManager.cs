// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio
{

    /// <summary>
    /// A decorator over an <see cref="IOleUndoManager"/> that batches undo units into a single, user-visible undo step.
    /// </summary>
    /// <remarks>
    /// A "scope" is started with <see cref="StartParentUndoScope(string)"/>, after which every <see cref="Add(IOleUndoUnit)"/>
    /// call attaches its undo unit to a single <see cref="ParentUndoUnit"/> instead of reaching the wrapped manager. When the
    /// scope is closed with <see cref="CloseParentUndoScope"/>, that one parent unit is pushed onto the wrapped
    /// <see cref="IOleUndoManager"/>, so the multiple XML-model and designer edits produced by one gesture are undone or redone
    /// by a single user action.
    /// <para>
    /// This manager wraps the <em>shell</em> undo manager, which is deliberately not the text buffer's own undo manager. The
    /// buffer exposes a separate <see cref="IOleUndoManager"/> via <c>IVsTextBuffer.GetUndoManager</c>, and the designer's
    /// document data disables it (<c>DiscardFrom(null)</c> followed by <c>Enable(0)</c>) at load time. If both managers stay
    /// live, a linked transaction rolls back through two independent undo stacks and the XML model concludes that its parse
    /// tree is out of sync with the buffer. Rolling the buffer back explicitly is unnecessary anyway: the units recorded here
    /// wrap XML model parse tree modifications, and undoing those commits the corresponding text back into the buffer. Do not
    /// "simplify" this class by pointing it at the buffer's undo manager or by re-enabling that manager.
    /// </para>
    /// </remarks>
    internal class ParentUndoManager : IOleUndoManager
    {

        #region Fields

        /// <summary>
        /// The unit currently collecting child undo units, or <see langword="null"/> when no scope is open.
        /// </summary>
        /// <remarks>
        /// While this is <see langword="null"/>, <see cref="Add(IOleUndoUnit)"/> silently drops units rather than forwarding
        /// them to the wrapped manager, which keeps un-scoped edits from landing on the shell undo stack as loose units.
        /// </remarks>
        private ParentUndoUnit _parentUndoUnit;

        /// <summary>
        /// The undo manager that this instance decorates and forwards all non-batched operations to.
        /// </summary>
        private readonly IOleUndoManager _wrappedUndoManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentUndoManager"/> class that decorates the specified undo manager.
        /// </summary>
        /// <param name="wrappedUndoManager">The <see cref="IOleUndoManager"/> to decorate. Completed parent undo units are added to this manager, and every other <see cref="IOleUndoManager"/> member is delegated to it.</param>
        /// <remarks>
        /// Callers are expected to pass the shell's undo manager, never the text buffer's. See the remarks on
        /// <see cref="ParentUndoManager"/> for why the two must not be confused.
        /// </remarks>
        public ParentUndoManager(IOleUndoManager wrappedUndoManager)
        {
            _wrappedUndoManager = wrappedUndoManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an undo unit to the currently open scope.
        /// </summary>
        /// <param name="pUU">The undo unit to collect into the open <see cref="ParentUndoUnit"/>.</param>
        /// <remarks>
        /// Unlike the other <see cref="IOleUndoManager"/> members, this one does not delegate to the wrapped manager: the whole
        /// point of the decorator is that individual units accumulate in the parent unit until the scope closes.
        /// </remarks>
        public void Add(IOleUndoUnit pUU)
        {
            // TODO AppDbproj If we initiate an XML Model transaction without a changescope, the
            // _parentUndoUnit will be null. This will go away once we introduce incremental changes
            _parentUndoUnit?.Add(pUU);
        }

        /// <summary>
        /// Closes a parent undo unit that was previously opened on the wrapped undo manager.
        /// </summary>
        /// <param name="pPUU">The parent undo unit to close.</param>
        /// <param name="fCommit">Non-zero to commit the unit onto the undo stack; zero to discard it.</param>
        /// <returns>The <c>HRESULT</c> returned by the wrapped undo manager.</returns>
        public int Close(IOleParentUndoUnit pPUU, int fCommit)
        {
            return _wrappedUndoManager.Close(pPUU, fCommit);
        }

        /// <summary>
        /// Closes the current undo scope, publishing the collected work as one undoable action.
        /// </summary>
        /// <remarks>
        /// The accumulated <see cref="ParentUndoUnit"/> is added to the wrapped <see cref="IOleUndoManager"/> and the scope is
        /// cleared so the next <see cref="StartParentUndoScope(string)"/> starts clean. Calling this without an open scope is a
        /// no-op, which keeps unbalanced begin/end pairs from throwing during transaction teardown.
        /// </remarks>
        public void CloseParentUndoScope()
        {
            if (_parentUndoUnit is not null)
            {
                _wrappedUndoManager.Add(_parentUndoUnit);
                _parentUndoUnit = null;
            }
        }

        /// <summary>
        /// Discards the specified undo unit and everything added after it from the wrapped undo manager.
        /// </summary>
        /// <param name="pUU">The undo unit to discard from, or <see langword="null"/> to discard the entire stack.</param>
        /// <remarks>
        /// This clears the wrapped (shell) undo stack only. The text buffer's separate undo stack is discarded and disabled
        /// once at document load by the document data, and must not be driven from here.
        /// </remarks>
        public void DiscardFrom(IOleUndoUnit pUU)
        {
            _wrappedUndoManager.DiscardFrom(pUU);
        }

        /// <summary>
        /// Enables or disables the wrapped undo manager.
        /// </summary>
        /// <param name="fEnable">Non-zero to enable undo, zero to disable it.</param>
        /// <remarks>
        /// This toggles the wrapped (shell) undo manager, not the text buffer's. The buffer's manager is intentionally left
        /// disabled for the lifetime of the designer document so that a linked transaction rolls back through exactly one undo
        /// stack; see the remarks on <see cref="ParentUndoManager"/>.
        /// </remarks>
        public void Enable(int fEnable)
        {
            _wrappedUndoManager.Enable(fEnable);
        }

        /// <summary>
        /// Retrieves an enumerator over the units on the wrapped manager's redo stack.
        /// </summary>
        /// <param name="ppEnum">When this method returns, contains the enumerator of redoable units.</param>
        public void EnumRedoable(out IEnumOleUndoUnits ppEnum)
        {
            _wrappedUndoManager.EnumRedoable(out ppEnum);
        }

        /// <summary>
        /// Retrieves an enumerator over the units on the wrapped manager's undo stack.
        /// </summary>
        /// <param name="ppEnum">When this method returns, contains the enumerator of undoable units.</param>
        public void EnumUndoable(out IEnumOleUndoUnits ppEnum)
        {
            _wrappedUndoManager.EnumUndoable(out ppEnum);
        }

        /// <summary>
        /// Retrieves the description of the wrapped manager's topmost redo unit.
        /// </summary>
        /// <param name="pBstr">When this method returns, contains the description shown on the shell's Redo command.</param>
        public void GetLastRedoDescription(out string pBstr)
        {
            _wrappedUndoManager.GetLastRedoDescription(out pBstr);
        }

        /// <summary>
        /// Retrieves the description of the wrapped manager's topmost undo unit.
        /// </summary>
        /// <param name="pBstr">When this method returns, contains the description shown on the shell's Undo command.</param>
        /// <remarks>
        /// For work batched by this decorator, the description is the scope name passed to
        /// <see cref="StartParentUndoScope(string)"/>, which is why that name should read as a user-facing action.
        /// </remarks>
        public void GetLastUndoDescription(out string pBstr)
        {
            _wrappedUndoManager.GetLastUndoDescription(out pBstr);
        }

        /// <summary>
        /// Retrieves the state of the wrapped manager's currently open parent undo unit.
        /// </summary>
        /// <param name="pdwState">When this method returns, contains the <c>UASFLAGS</c> state of the open parent unit.</param>
        /// <returns>The <c>HRESULT</c> returned by the wrapped undo manager.</returns>
        public int GetOpenParentState(out uint pdwState)
        {
            return _wrappedUndoManager.GetOpenParentState(out pdwState);
        }

        /// <summary>
        /// Opens a parent undo unit on the wrapped undo manager.
        /// </summary>
        /// <param name="pPUU">The parent undo unit to open.</param>
        /// <remarks>
        /// This is the OLE nesting mechanism and is independent of this class's own scope: it forwards straight through, whereas
        /// <see cref="StartParentUndoScope(string)"/> batches locally without touching the wrapped manager.
        /// </remarks>
        public void Open(IOleParentUndoUnit pPUU)
        {
            _wrappedUndoManager.Open(pPUU);
        }

        /// <summary>
        /// Redoes the wrapped manager's units up to and including the specified unit.
        /// </summary>
        /// <param name="pUU">The last unit to redo, or <see langword="null"/> to redo the topmost unit.</param>
        public void RedoTo(IOleUndoUnit pUU)
        {
            _wrappedUndoManager.RedoTo(pUU);
        }

        /// <summary>
        /// Starts an undo scope, so that subsequent undo units collapse into one user-visible action.
        /// </summary>
        /// <param name="name">The description of the scope, surfaced by <see cref="GetLastUndoDescription(out string)"/> on the shell's Undo command.</param>
        /// <remarks>
        /// Every unit passed to <see cref="Add(IOleUndoUnit)"/> between this call and <see cref="CloseParentUndoScope"/> is
        /// attached to a single <see cref="ParentUndoUnit"/>. Scopes do not nest: an already-open scope indicates an unbalanced
        /// begin/end pair and is asserted against in debug builds, where the previous scope's collected units are dropped.
        /// </remarks>
        public void StartParentUndoScope(string name)
        {
            Debug.Assert(_parentUndoUnit is null, "unexpected non-null value for _parentUndoUnit");
            _parentUndoUnit = new ParentUndoUnit(name);
        }

        /// <summary>
        /// Undoes the wrapped manager's units up to and including the specified unit.
        /// </summary>
        /// <param name="pUU">The last unit to undo, or <see langword="null"/> to undo the topmost unit.</param>
        /// <remarks>
        /// Undoing a unit produced by this decorator rolls back its children in reverse order in one pass, which is what makes a
        /// multi-step designer edit reversible with a single Undo.
        /// </remarks>
        public void UndoTo(IOleUndoUnit pUU)
        {
            _wrappedUndoManager.UndoTo(pUU);
        }

        #endregion

    }

}
