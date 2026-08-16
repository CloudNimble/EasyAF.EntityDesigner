// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{

    /// <summary>
    /// An <see cref="XmlTransaction" /> over one or more XLinq documents owned by a <see cref="VanillaXmlModelProvider" />.
    /// </summary>
    /// <remarks>
    /// The transaction does not snapshot the documents. It attaches a <see cref="SimpleTransactionLogger" /> to each
    /// enlisted document and records every mutation as a reversible command, so a rollback simply replays those
    /// commands backwards against the live tree.
    /// </remarks>
    public class SimpleTransaction : XmlTransaction
    {

        #region Fields

        /// <summary>
        /// The enclosing transaction, or <see langword="null" /> when this is the top level transaction.
        /// </summary>
        internal SimpleTransaction parent;

        /// <summary>
        /// The provider whose documents this transaction operates on.
        /// </summary>
        internal VanillaXmlModelProvider provider;

        /// <summary>
        /// The transaction manager that opened this transaction and that completes it.
        /// </summary>
        private readonly SimpleTransactionManager manager;

        /// <summary>
        /// The descriptive name supplied by the caller that opened the transaction.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// The change logger for each enlisted document, keyed by document.
        /// </summary>
        private readonly Dictionary<XDocument, SimpleTransactionLogger> resources;

        /// <summary>
        /// The current lifecycle state of the transaction.
        /// </summary>
        private XmlTransactionStatus status;

        /// <summary>
        /// Arbitrary state the caller associated with the transaction.
        /// </summary>
        private readonly object _userState;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the descriptive name supplied when the transaction was opened.
        /// </summary>
        public override string Name => name;

        /// <summary>
        /// Gets the enclosing transaction, or <see langword="null" /> when this is the top level transaction.
        /// </summary>
        public override XmlTransaction Parent => parent;

        /// <summary>
        /// Gets the provider whose documents this transaction operates on.
        /// </summary>
        public override XmlModelProvider Provider => provider;

        /// <summary>
        /// Gets the current lifecycle state of the transaction.
        /// </summary>
        public override XmlTransactionStatus Status => status;

        /// <summary>
        /// Gets the state associated with the undo unit for this transaction.
        /// </summary>
        /// <remarks>
        /// This provider does not integrate with a host undo stack, so there is never any undo state to expose.
        /// </remarks>
        public override object UndoUserState => null;

        /// <summary>
        /// Gets the arbitrary state the caller associated with the transaction.
        /// </summary>
        public override object UserState => _userState;

        /// <summary>
        /// Gets or sets a value indicating whether the transaction was opened by the designer rather than by the editor.
        /// </summary>
        internal bool DesignerTransaction { get; set; }

        /// <summary>
        /// Gets the number of enlisted documents that have recorded at least one change.
        /// </summary>
        internal int ModelsUpdated
        {
            get
            {
                var count = 0;
                foreach (var logger in resources.Values)
                {
                    if (logger.HasChanges)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTransaction" /> class in the active state.
        /// </summary>
        /// <param name="provider">The provider whose documents the transaction operates on.</param>
        /// <param name="name">A descriptive name for the transaction.</param>
        /// <param name="parent">The enclosing transaction, or <see langword="null" /> for a top level transaction.</param>
        /// <param name="mgr">The transaction manager that opened this transaction.</param>
        /// <param name="userState">Arbitrary state the caller wants to associate with the transaction.</param>
        internal SimpleTransaction(
            VanillaXmlModelProvider provider, string name, SimpleTransaction parent, SimpleTransactionManager mgr, object userState)
        {
            this.provider = provider;
            this.name = name;
            this.parent = parent;
            manager = mgr;
            DesignerTransaction = true;

            resources = [];
            status = XmlTransactionStatus.Active;
            _userState = userState;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enumerates the changes recorded so far across every document the provider has open.
        /// </summary>
        /// <returns>The recorded changes, in the order they will be applied.</returns>
        public override IEnumerable<IXmlChange> Changes()
        {
            foreach (var model in provider.OpenXmlModels)
            {
                foreach (var change in Changes(model))
                {
                    yield return change;
                }
            }
        }

        /// <summary>
        /// Enumerates the changes recorded so far against a single document.
        /// </summary>
        /// <param name="model">The model whose changes are requested.</param>
        /// <returns>The recorded changes for <paramref name="model" />, or an empty sequence when it was never enlisted.</returns>
        public override IEnumerable<IXmlChange> Changes(XmlModel model)
        {
            SimpleXmlModel m = model as SimpleXmlModel;
            if (resources.TryGetValue(m.Document, out SimpleTransactionLogger logger))
            {
                foreach (var cmd in logger.TxCommands)
                {
                    yield return cmd.Change;
                }
            }
        }

        /// <summary>
        /// Commits the transaction, promoting its recorded changes to the parent transaction when nested.
        /// </summary>
        /// <exception cref="XmlTransactionException">Thrown when the transaction has already been committed or aborted.</exception>
        /// <remarks>
        /// If promoting the commands to the parent fails the transaction is rolled back before the failure is
        /// rethrown, so a partially merged command list is never left behind on the parent.
        /// </remarks>
        public override void Commit()
        {
            if (status == XmlTransactionStatus.Committed
                || status == XmlTransactionStatus.Aborted)
            {
                throw new XmlTransactionException(Resources.VanillaProvider_TxAlreadyCompleted);
            }

            try
            {
                // This is a Child Tx
                parent?.AppendCommands(this);

                status = XmlTransactionStatus.Committed;
            }
            catch
            {
                Rollback();
                throw;
            }

            manager.Commit(this);
        }

        /// <summary>
        /// Rolls the transaction back by reverting every recorded change against the live XLinq tree.
        /// </summary>
        /// <exception cref="XmlTransactionException">Thrown when the transaction has already been committed or aborted.</exception>
        /// <remarks>
        /// Failures during the undo are swallowed: the transaction must always reach the aborted state and release
        /// its locks, otherwise the provider would be wedged. A tree left in an inconsistent state is expected to be
        /// discarded and re-parsed by the caller.
        /// </remarks>
        public override void Rollback()
        {
            if (status == XmlTransactionStatus.Committed
                || status == XmlTransactionStatus.Aborted)
            {
                throw new XmlTransactionException(Resources.VanillaProvider_TxAlreadyCompleted);
            }

            try
            {
                // If Store is null, then it means this is a Tx created by XmlEditor
                // In that case XmlEditor will probably drop the current tree anyways
                if (provider is not null)
                {
                    // Client wants to rollback, so undo changes on XLINQ tree
                    UndoTransaction();
                }
            }
            catch
            {
                // TODO: Changes cannot be rolled back now, drop the current tree and re-parse whole document
            }
            finally
            {
                status = XmlTransactionStatus.Aborted;
                manager.Rollback(this);
            }
        }

        /// <summary>
        /// Releases the resources held by the transaction, rolling it back when it is still active.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> when called from <see cref="System.IDisposable.Dispose" />; <see langword="false" /> when called from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (status == XmlTransactionStatus.Active)
            {
                Rollback();
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Absorbs the commands recorded by a committing child transaction into this transaction's loggers.
        /// </summary>
        /// <param name="childTransaction">The child transaction whose commands are promoted.</param>
        internal void AppendCommands(SimpleTransaction childTransaction)
        {
            foreach (var doc in childTransaction.resources.Keys)
            {
                var logger = resources[doc];
                foreach (var cmd in childTransaction.resources[doc].TxCommands)
                {
                    logger.AddCommand(cmd);
                }

                foreach (var cmd in childTransaction.resources[doc].UndoCommands)
                {
                    logger.AddCommand(cmd);
                }
            }
        }

        /// <summary>
        /// Enlists a model in the transaction by attaching a change logger to its document.
        /// </summary>
        /// <param name="model">The model to enlist.</param>
        internal void EnlistResource(SimpleXmlModel model)
        {
            var doc = model.Document;
            resources.Add(doc, new SimpleTransactionLogger(doc, this));
        }

        /// <summary>
        /// Raises the provider's transaction completed event for this transaction.
        /// </summary>
        internal void FireCompleted()
        {
            provider.FireTransactionCompleted(this);
        }

        /// <summary>
        /// Acquires a write lock on a model and enlists it in the transaction.
        /// </summary>
        /// <param name="doc">The model to lock and enlist.</param>
        internal void LockForWrite(SimpleXmlModel doc)
        {
            Lock(doc, LockMode.Write);
        }

        /// <summary>
        /// Resumes listening to XLinq change events on every enlisted document.
        /// </summary>
        internal void MakeActive()
        {
            // Making a Tx Inactive means it will start listening to events on XLINQ tree
            status = XmlTransactionStatus.Active;
            foreach (var logger in resources.Values)
            {
                logger.Start();
            }
        }

        /// <summary>
        /// Stops listening to XLinq change events on every enlisted document.
        /// </summary>
        /// <remarks>
        /// This is what keeps a single edit from being recorded twice while a nested transaction is running.
        /// </remarks>
        internal void MakeInactive()
        {
            // Making a Tx Inactive means it will no longer listen to events on XLINQ tree
            foreach (var logger in resources.Values)
            {
                logger.Stop();
            }
        }

        /// <summary>
        /// Reverts every recorded change against the live XLinq tree.
        /// </summary>
        /// <remarks>
        /// The commands are replayed from the end of each undo list backwards, because each command only restores
        /// the state that immediately preceded it.
        /// </remarks>
        internal void UndoTransaction()
        {
            // Undo Transaction in oppposite order
            foreach (var logger in resources.Values)
            {
                var cmds = logger.UndoCommands;
                var count = cmds.Count;
                for (var i = count - 1; i >= 0; i--)
                {
                    var modelCmd = cmds[i];
                    modelCmd.Undo();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Acquires a lock of the requested mode on a model, enlisting it when the lock is a write lock.
        /// </summary>
        /// <param name="doc">The model to lock.</param>
        /// <param name="mode">The lock mode to acquire.</param>
        /// <remarks>
        /// Locking is skipped entirely once the transaction has completed, so a stale reference cannot re-acquire
        /// locks that were already released on its behalf.
        /// </remarks>
        private void Lock(SimpleXmlModel doc, LockMode mode)
        {
            if (Status == XmlTransactionStatus.Active)
            {
                if (mode == LockMode.Write)
                {
                    manager.SimpleLockManager.LockForWrite(this, doc);
                    EnlistResource(doc);
                }
                else
                {
                    manager.SimpleLockManager.LockForRead(this, doc);
                }
            }
        }

        #endregion

    }

}
