// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Listens to the change events of a single XLinq document and records every mutation as a reversible command
    /// on behalf of a <see cref="SimpleTransaction" />.
    /// </summary>
    /// <remarks>
    /// Two command lists are maintained deliberately. The undo list is a faithful, unfiltered log used to roll the
    /// transaction back. The transaction list is the optimized set that is pushed to the text buffer on commit: it
    /// drops edits to subtrees that were subsequently removed, drops edits to nodes whose ancestor was added within
    /// the same transaction, and merges repeated edits of one node, because the source modifier cannot update text
    /// it inserted earlier in the same transaction.
    /// </remarks>
    internal class SimpleTransactionLogger
    {

        #region Fields

        /// <summary>
        /// Cached delegate for <see cref="OnAfterChange" /> so it can be unsubscribed by reference.
        /// </summary>
        private EventHandler<XObjectChangeEventArgs> afterEvent;

        /// <summary>
        /// Cached delegate for <see cref="OnBeforeChange" /> so it can be unsubscribed by reference.
        /// </summary>
        private EventHandler<XObjectChangeEventArgs> beforeEvent;

        /// <summary>
        /// The factory used to create commands, allowing a host to substitute buffer-aware implementations.
        /// </summary>
        private readonly CommandFactory cmdFactory;

        /// <summary>
        /// The index in <see cref="txCommands" /> below which commands have already been flushed and must not be
        /// filtered out by later removals.
        /// </summary>
        private int committedPosition = 0;

        /// <summary>
        /// The change captured by the "before" notification, completed and turned into a command by the "after"
        /// notification for the same edit.
        /// </summary>
        private SimpleXmlChange currentChange;

        /// <summary>
        /// The document being observed.
        /// </summary>
        private readonly XDocument model;

        /// <summary>
        /// The set of nodes added during this transaction, used as a set (the value repeats the key).
        /// </summary>
        private readonly Dictionary<XObject, object> nodesAdded;

        /// <summary>
        /// The transaction this logger records for.
        /// </summary>
        private readonly SimpleTransaction tx;

        /// <summary>
        /// The optimized command list that represents the net effect of the transaction.
        /// </summary>
        private readonly List<XmlModelCommand> txCommands;

        /// <summary>
        /// The unfiltered command list used to roll the transaction back.
        /// </summary>
        private readonly List<XmlModelCommand> undoCommands;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether any change has been recorded for the observed document.
        /// </summary>
        internal bool HasChanges => TxCommands is not null ? TxCommands.Count > 0 : false;

        /// <summary>
        /// Gets the optimized command list that represents the net effect of the transaction.
        /// </summary>
        internal List<XmlModelCommand> TxCommands => txCommands;

        /// <summary>
        /// Gets the unfiltered command list used to roll the transaction back.
        /// </summary>
        internal List<XmlModelCommand> UndoCommands => undoCommands;

        /// <summary>
        /// Gets the cached delegate for <see cref="OnAfterChange" />.
        /// </summary>
        /// <remarks>
        /// The delegate is cached because subscribing and unsubscribing must use the same instance, and a fresh
        /// method group conversion would produce a delegate that does not compare equal on unsubscribe.
        /// </remarks>
        private EventHandler<XObjectChangeEventArgs> AfterEventHandler
        {
            get
            {
                afterEvent ??= OnAfterChange;
                return afterEvent;
            }
        }

        /// <summary>
        /// Gets the cached delegate for <see cref="OnBeforeChange" />.
        /// </summary>
        /// <remarks>
        /// The delegate is cached because subscribing and unsubscribing must use the same instance, and a fresh
        /// method group conversion would produce a delegate that does not compare equal on unsubscribe.
        /// </remarks>
        private EventHandler<XObjectChangeEventArgs> BeforeEventHandler
        {
            get
            {
                beforeEvent ??= OnBeforeChange;
                return beforeEvent;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTransactionLogger" /> class for a document.
        /// </summary>
        /// <param name="m">The document to observe.</param>
        /// <param name="t">The transaction the recorded commands belong to.</param>
        /// <remarks>
        /// A private <see cref="CommandFactory" /> is created when the transaction's provider cannot supply one,
        /// so the logger still works for transactions that were not opened by a <see cref="VanillaXmlModelProvider" />.
        /// </remarks>
        internal SimpleTransactionLogger(XDocument m, SimpleTransaction t)
        {
            model = m;
            tx = t;
            undoCommands = [];
            txCommands = [];

            nodesAdded = [];
            if ((tx.Provider is not VanillaXmlModelProvider provider || provider.CommandFactory is null))
            {
                cmdFactory = new CommandFactory();
            }
            else
            {
                cmdFactory = provider.CommandFactory;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether a node, or any of its ancestors, was added during this transaction.
        /// </summary>
        /// <param name="node">The node to test.</param>
        /// <param name="parent">The container to start the ancestor walk from.</param>
        /// <returns><see langword="true" /> when the node itself or an ancestor was added by this transaction; otherwise <see langword="false" />.</returns>
        public bool IsAncestorAdded(XObject node, XContainer parent)
        {
            if (nodesAdded.ContainsKey(node))
            {
                return true;
            }
            for (var p = parent; p is not null; p = p.Parent)
            {
                if (nodesAdded.ContainsKey(p))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Records a command in the optimized transaction list, filtering and merging it as required.
        /// </summary>
        /// <param name="cmd">The command to record.</param>
        /// <remarks>
        /// Name changes are inserted at the front rather than appended: an insertion can force a short end tag
        /// <c>&lt;A/&gt;</c> to be expanded to <c>&lt;A&gt;&lt;/A&gt;</c>, and a later rename of that same node would
        /// then produce two edits against one end tag, which the source modifier does not support.
        /// </remarks>
        internal void AddCommand(XmlModelCommand cmd)
        {
            var change = cmd.Change;
            var node = change.Node;

            if (change.Action == XObjectChange.Remove)
            {
                // If we are removing a node, then all changes to any descendent of this node
                // can be discarded since the whole thing will be ripped out of the buffer anyway.
                var rs = RemoveAllDescendentChanges(change);
                if (rs == RemoveStatus.FoundSelfAdd)
                {
                    /*
                     * 9/21/2007 - not sure what to do about the stuff below.  Seems specific to the editor
                     * so commenting it out.
                     *
                    // then this remove is a no-op, unless the Add operation is one of those
                    // special ones where the buffer already contained the value - in this case
                    // the Remove operation is real, and the add operation is a no-op, otherwise
                    // they both cancel each other out.
                    string bufferValue = XmlEditorNoPushToBuffer(change.Node);
                    if (bufferValue == null) {
                        return;
                    }
                     */
                }
            }
            if (IsAncestorAdded(node, change.Parent))
            {
                // If the ancestor was added by this transaction, then we can ignore the child
                // command because the XLink insert will insert the entire thing into the buffer
                // including all changes already made by the child transaction.
                return;
            }

            switch (change.Action)
            {
                case XObjectChange.Add:
                    AddNode(node); // record the add operation.
                    break;
                case XObjectChange.Name:
                    Merge(cmd);
                    // Name changes must be done first, because an Add operation might
                    // cause a short end tag <A/> to be converted to <A></A>, and then
                    // a rename after that of the same node would result in two edit
                    // operations on the same end tag which is not supported by SourceModifier.
                    undoCommands.Insert(0, cmd);
                    txCommands.Insert(0, cmd);
                    return;
                case XObjectChange.Value:
                    Merge(cmd);
                    break;
                case XObjectChange.Remove:
                    break;
            }

            txCommands.Add(cmd);
        }

        /// <summary>
        /// Subscribes to the change events of the observed document.
        /// </summary>
        internal void Start()
        {
            model.Changing += BeforeEventHandler;
            model.Changed += AfterEventHandler;
        }

        /// <summary>
        /// Unsubscribes from the change events of the observed document.
        /// </summary>
        internal void Stop()
        {
            model.Changing -= BeforeEventHandler;
            model.Changed -= AfterEventHandler;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Records that a node was added by this transaction.
        /// </summary>
        /// <param name="node">The node that was added.</param>
        private void AddNode(XObject node)
        {
            if (!nodesAdded.ContainsKey(node))
            {
                nodesAdded[node] = node;
            }
        }

        /// <summary>
        /// Determines whether an already recorded change targets the node of a new change or one of its descendants.
        /// </summary>
        /// <param name="existingChange">The change already recorded.</param>
        /// <param name="newChange">The change being recorded now.</param>
        /// <returns><see langword="true" /> when <paramref name="existingChange" /> is subsumed by <paramref name="newChange" />; otherwise <see langword="false" />.</returns>
        /// <remarks>
        /// The parent containers are compared as well as the nodes themselves so that a move, which surfaces as a
        /// remove followed by an add under a different parent, is not mistaken for a redundant pair of edits.
        /// </remarks>
        private static bool IsDescendentOrSelf(SimpleXmlChange existingChange, SimpleXmlChange newChange)
        {
            var child = existingChange.Node;
            var parent = newChange.Node;
            if (child == parent)
            {
                if (child.Parent is null)
                {
                    //either two deletes
                    //or a node N was added and then deleted
                    if (existingChange.Parent == newChange.Parent)
                    {
                        return true;
                    }
                }
                else if (newChange.Parent == existingChange.Parent)
                {
                    //do not return true in case of move
                    return true;
                }
            }
            if (parent is XElement pe)
            {
                for (XElement e = existingChange.Parent as XElement; e is not null; e = e.Parent)
                {
                    if (e == pe)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Collapses earlier commands that target the same node into <paramref name="newCommand" />.
        /// </summary>
        /// <param name="newCommand">The command that absorbs the earlier ones.</param>
        /// <remarks>
        /// Absorbed commands are dropped from the undo list as well as the transaction list, because merging
        /// mutates the surviving command object in place and the undo list holds the very same instances.
        /// </remarks>
        private void Merge(XmlModelCommand newCommand)
        {
            // Some commands have to be merged (for example if the name or value of the same
            // node is changed twice) because the SourceModifier is not designed to update
            // stuff it just inserted into the buffer during the same transaction.
            List<XmlModelCommand> toRemove = null;
            foreach (var cmd in txCommands)
            {
                if (newCommand != cmd
                    && newCommand.Merge(cmd))
                {
                    toRemove ??= [];
                    toRemove.Add(cmd);
                }
            }
            RemoveRange(txCommands, toRemove);
            // And remove this from undo list also, since we have edited the same command objects!
            RemoveRange(undoCommands, toRemove);
        }

        /// <summary>
        /// Completes the pending change with the post-edit state and turns it into a command.
        /// </summary>
        /// <param name="sender">The <see cref="XObject" /> that changed.</param>
        /// <param name="e">The kind of change that occurred.</param>
        /// <remarks>
        /// For attributes the last attribute that already existed before the transaction started is located here,
        /// because XLinq gives attributes no stable position and that anchor is the only way to reproduce the
        /// original attribute order when the edit is pushed to a text buffer.
        /// </remarks>
        private void OnAfterChange(object sender, XObjectChangeEventArgs e)
        {
            XObject node = sender as XObject;
            var action = e.ObjectChange;
            XmlModelCommand commandToAdd = null;

            XElement element = null;
            XProcessingInstruction pi = null;

            switch (action)
            {
                case XObjectChange.Add:
                    AddNodeChangeInternal addChange = currentChange as AddNodeChangeInternal;
                    addChange.Parent = node.Parent;
                    addChange.Parent ??= node.Document;
                    XAttribute attrib = (node as XAttribute);
                    if (attrib is not null)
                    {
                        addChange.NextNode = attrib.NextAttribute;
                        var stableAttr = attrib.PreviousAttribute;
                        while (stableAttr is not null
                               && nodesAdded.ContainsKey(stableAttr))
                        {
                            stableAttr = stableAttr.PreviousAttribute;
                        }
                        addChange.lastStableAttribute = stableAttr;
                    }
                    else
                    {
                        addChange.NextNode = (node as XNode).NextNode;
                    }

                    commandToAdd = cmdFactory.CreateAddNodeCommand((VanillaXmlModelProvider)tx.Provider, addChange);
                    break;
                case XObjectChange.Remove:
                    RemoveNodeChange removeChange = currentChange as RemoveNodeChange;
                    commandToAdd = cmdFactory.CreateRemoveNodeCommand((VanillaXmlModelProvider)tx.Provider, removeChange);
                    break;
                case XObjectChange.Name:
                    NodeNameChange nameChange = currentChange as NodeNameChange;
                    if ((element = node as XElement) is not null)
                    {
                        nameChange.NewName = element.Name;
                    }
                    else if ((pi = node as XProcessingInstruction) is not null)
                    {
                        nameChange.NewName = XName.Get(pi.Target);
                    }
                    commandToAdd = cmdFactory.CreateSetNameCommand((VanillaXmlModelProvider)tx.Provider, nameChange);
                    break;
                case XObjectChange.Value:
                    NodeValueChange valueChange = currentChange as NodeValueChange;
                    if ((element = node as XElement) is not null)
                    {
                        valueChange.NewValue = element.Value;
                    }
                    else if (node is XAttribute attribute)
                    {
                        valueChange.NewValue = attribute.Value;
                    }
                    else if (node is XText text)
                    {
                        valueChange.NewValue = text.Value;
                    }
                    else if (node is XComment comment)
                    {
                        valueChange.NewValue = comment.Value;
                    }
                    else if ((pi = node as XProcessingInstruction) is not null)
                    {
                        valueChange.NewValue = pi.Data;
                    }

                    commandToAdd = cmdFactory.CreateSetValueCommand((VanillaXmlModelProvider)tx.Provider, valueChange);
                    break;
            }
            AddCommand(commandToAdd);
            undoCommands.Add(commandToAdd); // always record full undo command list.
        }

        /// <summary>
        /// Captures the pre-edit state of the node that is about to change.
        /// </summary>
        /// <param name="sender">The <see cref="XObject" /> that is about to change.</param>
        /// <param name="e">The kind of change that is about to occur.</param>
        /// <exception cref="NotSupportedException">Thrown when the change targets a DTD node, or when a name or value change is reported for a node kind that is not handled.</exception>
        /// <remarks>
        /// State such as the former parent, the following sibling and the previous name or value is only readable
        /// before the edit is applied, which is why it has to be captured here rather than in the "after" handler.
        /// </remarks>
        private void OnBeforeChange(object sender, XObjectChangeEventArgs e)
        {
            XObject node = sender as XObject;

            // We do not allow editing DTDs through XmlModel
            if (node is XDocumentType)
            {
                var msg = String.Format(CultureInfo.CurrentCulture, XmlEngineResources.VanillaProvider_DtdNodesReadOnly);
                throw new NotSupportedException(msg);
            }
            var action = e.ObjectChange;
            currentChange = null;

            XElement element = null;
            XProcessingInstruction pi = null;

            switch (action)
            {
                case XObjectChange.Add:
                    AddNodeChangeInternal addChange = new AddNodeChangeInternal(node, action);
                    currentChange = addChange;
                    break;
                case XObjectChange.Remove:
                    RemoveNodeChange removeChange = new RemoveNodeChange(node, action);
                    removeChange.Parent = node.Parent;
                    removeChange.Parent ??= node.Document;
                    XAttribute attrib = (node as XAttribute);
                    if (attrib is not null)
                    {
                        removeChange.NextNode = attrib.NextAttribute;
                    }
                    else
                    {
                        removeChange.NextNode = (node as XNode).NextNode;
                    }
                    currentChange = removeChange;
                    break;
                case XObjectChange.Name:
                    NodeNameChange nameChange = new NodeNameChange(node, action);
                    if ((element = node as XElement) is not null)
                    {
                        nameChange.OldName = element.Name;
                    }
                    else if ((pi = node as XProcessingInstruction) is not null)
                    {
                        nameChange.OldName = XName.Get(pi.Target);
                    }
                    else
                    {
                        Debug.Assert(false, "The name of something changed that we're not handling here!");
                        throw new NotSupportedException();
                    }
                    currentChange = nameChange;
                    break;
                case XObjectChange.Value:
                    NodeValueChange valueChange = new NodeValueChange(node, action);
                    if ((element = node as XElement) is not null)
                    {
                        valueChange.OldValue = element.Value;
                    }
                    else if (node is XAttribute attribute)
                    {
                        valueChange.OldValue = attribute.Value;
                    }
                    else if (node is XText text)
                    {
                        valueChange.OldValue = text.Value;
                        if (text.Parent is not null)
                        {
                            valueChange.Parent = text.Parent;
                        }
                        else
                        {
                            valueChange.Parent = text.Document;
                        }
                    }
                    else if (node is XComment comment)
                    {
                        valueChange.OldValue = comment.Value;
                    }
                    else if ((pi = node as XProcessingInstruction) is not null)
                    {
                        valueChange.OldValue = pi.Data;
                    }
                    else
                    {
                        Debug.Assert(false, "The value of something changed that we're not handling here!");
                        throw new NotSupportedException();
                    }
                    currentChange = valueChange;
                    break;
            }
        }

        /// <summary>
        /// Drops every uncommitted command that targets the node being removed or one of its descendants.
        /// </summary>
        /// <param name="newChange">The removal that supersedes the earlier commands.</param>
        /// <returns><see cref="RemoveStatus.FoundSelfAdd" /> when the removal cancels an insertion of the very same node recorded in this transaction; otherwise <see cref="RemoveStatus.None" />.</returns>
        /// <remarks>
        /// Scanning starts at <see cref="committedPosition" /> so commands that have already been pushed to the
        /// buffer are left intact; only pending commands may be discarded.
        /// </remarks>
        private RemoveStatus RemoveAllDescendentChanges(SimpleXmlChange newChange)
        {
            // This node is being removed, therefore any change to any child of this
            // node is now unnecessary, so we can filter them out.
            var rc = RemoveStatus.None;
            List<XmlModelCommand> toRemove = null;
            for (int i = committedPosition, n = txCommands.Count; i < n; i++)
            {
                var cmd = txCommands[i];
                var change = cmd.Change;
                if (IsDescendentOrSelf(change, newChange))
                {
                    if (change.Node == newChange.Node
                        && change.Action == XObjectChange.Add)
                    {
                        rc = RemoveStatus.FoundSelfAdd; // we are removing an add of the same node!
                    }
                    toRemove ??= [];
                    toRemove.Add(cmd);
                }
            }
            RemoveRange(txCommands, toRemove);
            return rc;
        }

        /// <summary>
        /// Removes a set of commands from a command list.
        /// </summary>
        /// <param name="list">The list to remove from.</param>
        /// <param name="toRemove">The commands to remove, or <see langword="null" /> when there is nothing to remove.</param>
        /// <remarks>
        /// Discarding an add command also retracts its entry from the added-node set; otherwise later edits to that
        /// node would still be suppressed by <see cref="IsAncestorAdded" /> even though the insertion is gone.
        /// </remarks>
        private void RemoveRange(List<XmlModelCommand> list, List<XmlModelCommand> toRemove)
        {
            if (toRemove is not null)
            {
                foreach (var cmd in toRemove)
                {
                    list.Remove(cmd);
                    var change = cmd.Change;
                    var key = change.Node;
                    if (change.Action == XObjectChange.Add
                        &&
                        nodesAdded.ContainsKey(key))
                    {
                        nodesAdded.Remove(key);
                    }
                }
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// The outcome of discarding the commands superseded by a node removal.
        /// </summary>
        private enum RemoveStatus
        {

            /// <summary>
            /// No superseded insertion of the removed node itself was found.
            /// </summary>
            None,

            /// <summary>
            /// The removal cancelled an insertion of the very same node recorded earlier in this transaction.
            /// </summary>
            FoundSelfAdd

        }

        #endregion

    }

}
