// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Owns the transaction stack and the lock manager for every <see cref="VanillaXmlModelProvider" /> in the process.
    /// </summary>
    /// <remarks>
    /// Transactions nest, but only one may be listening to XLinq events at a time or a single edit would be recorded
    /// several times. The manager therefore deactivates the parent while a child runs and reactivates it when the
    /// child completes, and it only releases locks and raises completion events when the outermost transaction ends.
    /// </remarks>
    internal class SimpleTransactionManager : IDisposable
    {

        #region Fields

        /// <summary>
        /// The innermost active transaction per provider, or <see langword="null" /> when no transaction is open.
        /// </summary>
        private readonly Dictionary<VanillaXmlModelProvider, SimpleTransaction> _currentTransaction;

        /// <summary>
        /// The lock manager arbitrating document access between transactions.
        /// </summary>
        private readonly SimpleLockManager _lockManager;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the lock manager arbitrating document access between transactions.
        /// </summary>
        internal SimpleLockManager SimpleLockManager => _lockManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTransactionManager" /> class.
        /// </summary>
        internal SimpleTransactionManager()
        {
            _currentTransaction = [];
            _lockManager = new SimpleLockManager();
        }

        /// <summary>
        /// Releases the lock manager owned by this instance if <see cref="Dispose()" /> was never called.
        /// </summary>
        ~SimpleTransactionManager()
        {
            Dispose(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Releases the lock manager owned by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the lock manager owned by this instance.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lockManager?.Dispose();
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Opens a transaction, nesting it inside <paramref name="parent" /> when one is supplied.
        /// </summary>
        /// <param name="provider">The provider the transaction operates on.</param>
        /// <param name="name">A descriptive name for the transaction.</param>
        /// <param name="parent">The enclosing transaction, or <see langword="null" /> for a top level transaction.</param>
        /// <param name="userState">Arbitrary state the caller wants to associate with the transaction.</param>
        /// <returns>The newly opened, active transaction.</returns>
        /// <remarks>
        /// A top level transaction takes write locks on every open document up front, under a single monitor, so
        /// that two providers can never acquire the same documents in a different order and deadlock. A nested
        /// transaction inherits those locks and only needs its own change loggers.
        /// </remarks>
        internal SimpleTransaction BeginTransaction(
            VanillaXmlModelProvider provider, string name, SimpleTransaction parent, object userState)
        {
            SimpleTransaction tx = new SimpleTransaction(provider, name, parent, this, userState);

            if (parent is not null)
            {
                // Unsubscribe events on parent transaction
                parent.MakeInactive();

                // Create SimpleTransactionLogger for each document in VanillaXmlModelProvider
                foreach (var model in provider.OpenXmlModels)
                {
                    tx.EnlistResource(model as SimpleXmlModel);
                }
            }
            else
            {
                // This is the top level transaction, so acquire all locks to avoid deadlocks
                lock (this)
                {
                    foreach (var model in provider.OpenXmlModels)
                    {
                        tx.LockForWrite(model as SimpleXmlModel);
                    }
                }
            }

            // Make this the current Active transaction
            SetCurrentTransaction(provider, tx);
            tx.MakeActive();

            return tx;
        }

        /// <summary>
        /// Completes a transaction successfully and makes its parent current again.
        /// </summary>
        /// <param name="tx">The transaction being committed.</param>
        /// <remarks>
        /// Locks are only released and the completion event only raised when the outermost transaction commits,
        /// because a nested commit does not yet make the edits final.
        /// </remarks>
        internal void Commit(SimpleTransaction tx)
        {
            tx.MakeInactive();
            var parent = tx.parent;

            // Unlock all resources held by this transaction
            parent?.MakeActive();
            _currentTransaction[tx.provider] = parent;

            // Fire Events and unlock Store only when  Top-most Tx commits
            if (parent is null)
            {
                SimpleLockManager.UnlockAll(tx);
                tx.FireCompleted();
            }
        }

        /// <summary>
        /// Gets the innermost active transaction for a provider.
        /// </summary>
        /// <param name="provider">The provider to query.</param>
        /// <returns>The current transaction, or <see langword="null" /> when none is open.</returns>
        internal SimpleTransaction GetCurrentTransaction(VanillaXmlModelProvider provider)
        {
            SimpleTransaction current = null;
            if (_currentTransaction.ContainsKey(provider))
            {
                current = _currentTransaction[provider];
            }

            return current;
        }

        /// <summary>
        /// Completes a transaction unsuccessfully and makes its parent current again.
        /// </summary>
        /// <param name="tx">The transaction being rolled back.</param>
        /// <remarks>
        /// Unlike a commit, the completion event is raised even for a nested transaction, because listeners have
        /// already been told about the edits that were just reverted and need to hear that they were undone.
        /// </remarks>
        internal void Rollback(SimpleTransaction tx)
        {
            tx.MakeInactive();
            var parent = tx.parent;

            // Unlock all resources held by this transaction
            parent?.MakeActive();
            _currentTransaction[tx.provider] = parent;

            // Unlock Store only when Top-most Tx Completes
            if (parent is null)
            {
                SimpleLockManager.UnlockAll(tx);
            }

            // In case of Rollback we fire off event even for nested Tx
            tx.FireCompleted();
        }

        /// <summary>
        /// Records the innermost active transaction for a provider.
        /// </summary>
        /// <param name="provider">The provider the transaction belongs to.</param>
        /// <param name="tx">The transaction to make current, or <see langword="null" /> to clear it.</param>
        internal void SetCurrentTransaction(VanillaXmlModelProvider provider, SimpleTransaction tx)
        {
            if (_currentTransaction.ContainsKey(provider))
            {
                _currentTransaction[provider] = tx;
            }
            else
            {
                _currentTransaction.Add(provider, tx);
            }
        }

        #endregion

    }

}
