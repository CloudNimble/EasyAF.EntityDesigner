// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{

    /// <summary>
    /// Grants and releases read and write locks on documents on behalf of <see cref="SimpleTransaction" /> instances.
    /// </summary>
    /// <remarks>
    /// The manager is deliberately simple: it has no wait graph, so it cannot detect a genuine deadlock cycle.
    /// Instead a waiter that makes no progress for five seconds gives up and reports a deadlock. Because the
    /// top level transaction acquires every document lock up front, lock ordering problems are avoided in practice.
    /// </remarks>
    internal class SimpleLockManager : IDisposable
    {

        #region Fields

        // Defines types of lock requests, which can be granted while holding other locks
        /// <summary>
        /// Indexed by the lock mode currently held and then by the lock mode requested; <see langword="true" />
        /// means the request may be granted while the held mode is in force.
        /// </summary>
        private static readonly bool[][] CompatibilityTable = new bool[(int)LockMode._Length][]
            {
                // Null
                new bool[(int)LockMode._Length] { true, true, true },
                // Read
                new bool[(int)LockMode._Length] { true, true, true },
                // Write
                new bool[(int)LockMode._Length] { true, false, false }
            };

        /// <summary>
        /// Maps each locked resource to the entry that tracks which transactions hold which modes on it.
        /// </summary>
        private readonly Dictionary<Object, ResourceEntry> _resourceTable = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Releases the wait handles owned by this instance if <see cref="Dispose()" /> was never called.
        /// </summary>
        ~SimpleLockManager()
        {
            Dispose(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Releases the wait handles owned by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Acquires a lock of the requested mode on a resource for a transaction, blocking until it can be granted.
        /// </summary>
        /// <param name="txId">The transaction the lock is granted to.</param>
        /// <param name="resource">The resource to lock.</param>
        /// <param name="mode">The lock mode being requested.</param>
        /// <exception cref="XmlTransactionException">Thrown when the lock could not be granted within five seconds, which is treated as a deadlock.</exception>
        /// <remarks>
        /// Any lock the transaction already holds is temporarily unregistered before compatibility is evaluated,
        /// so that a transaction can upgrade its own lock instead of blocking against itself; the previous mode is
        /// restored if the upgrade cannot be granted.
        /// </remarks>
        public void Lock(SimpleTransaction txId, object resource, LockMode mode)
        {
            ResourceEntry lockTarget = null;

            lock (_resourceTable)
            {
                lockTarget = GetResoureEntry(resource);

                if (lockTarget is null)
                {
                    lockTarget = new ResourceEntry();
                    _resourceTable[resource] = lockTarget;
                }
            }

            for (var c = 0;; c++)
            {
                if (c > 0)
                {
                    if (!lockTarget.UnlockEvent.WaitOne(5000, false))
                    {
                        throw new XmlTransactionException("DeadLock Detected...");
                    }
                }

                lock (lockTarget)
                {
                    var oldLockMode = lockTarget.GetExistingLockMode(txId);
                    if (oldLockMode != LockMode.Null)
                    {
                        lockTarget.Unregister(txId, oldLockMode);
                    }
                    if (lockTarget.Compatible(mode))
                    {
                        lockTarget.Register(txId, mode);
                        return;
                    }
                    if (oldLockMode != LockMode.Null)
                    {
                        lockTarget.Register(txId, oldLockMode);
                    }
                }
            }
        }

        /// <summary>
        /// Acquires a read lock on a document for a transaction.
        /// </summary>
        /// <param name="txId">The transaction the lock is granted to.</param>
        /// <param name="doc">The document to lock.</param>
        public void LockForRead(SimpleTransaction txId, object doc)
        {
            Lock(txId, doc, LockMode.Read);
        }

        /// <summary>
        /// Acquires a write lock on a document for a transaction.
        /// </summary>
        /// <param name="txId">The transaction the lock is granted to.</param>
        /// <param name="doc">The document to lock.</param>
        public void LockForWrite(SimpleTransaction txId, object doc)
        {
            Lock(txId, doc, LockMode.Write);
        }

        /// <summary>
        /// Releases every lock held by a transaction across all resources.
        /// </summary>
        /// <param name="txId">The transaction whose locks are released.</param>
        /// <remarks>
        /// This is called when the top most transaction completes, so the whole resource table is swept rather
        /// than tracking per transaction lock lists.
        /// </remarks>
        public void UnlockAll(SimpleTransaction txId)
        {
            ResourceEntry lockTarget = null;
            lock (_resourceTable)
            {
                IDictionaryEnumerator resenum = _resourceTable.GetEnumerator();

                while (resenum.MoveNext())
                {
                    lockTarget = resenum.Value as ResourceEntry;
                    if (lockTarget is null)
                    {
                        continue;
                    }
                    lock (lockTarget)
                    {
                        for (var lockMode = (int)LockMode.Read; lockMode < (int)LockMode._Length; lockMode++)
                        {
                            lockTarget.Unregister(txId, (LockMode)lockMode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Releases the wait handles owned by this instance.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var resourceEntry in _resourceTable.Values)
                {
                    resourceEntry.Dispose();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Looks up the lock entry for a resource.
        /// </summary>
        /// <param name="src">The resource to look up.</param>
        /// <returns>The entry tracking locks on <paramref name="src" />, or <see langword="null" /> when the resource has never been locked.</returns>
        private ResourceEntry GetResoureEntry(object src)
        {
            ResourceEntry rEntry = null;
            if (_resourceTable.ContainsKey(src))
            {
                rEntry = _resourceTable[src];
            }

            return rEntry;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Tracks which transactions hold which lock modes on a single resource.
        /// </summary>
        /// <remarks>
        /// The wait handle is created lazily and only when someone actually has to wait, because most resources are
        /// never contended and an <see cref="AutoResetEvent" /> per resource would otherwise waste kernel handles.
        /// </remarks>
        private sealed class ResourceEntry : IDisposable
        {

            #region Fields

            /// <summary>
            /// Signalled when the strongest lock on this resource weakens, so waiters can re-evaluate their request.
            /// </summary>
            private AutoResetEvent _autoResetEvent;

            /// <summary>
            /// The strongest lock mode currently held on this resource by any transaction.
            /// </summary>
            private LockMode _lockMode;

            /// <summary>
            /// Maps a lock mode to the set of transactions holding that mode. The value of each inner dictionary
            /// entry is the key itself; the dictionary is used purely as a set.
            /// </summary>
            private readonly Dictionary<LockMode, Dictionary<SimpleTransaction, SimpleTransaction>> _transactions;

            #endregion

            #region Properties

            // Define a property for UnlockEvent
            /// <summary>
            /// Gets the wait handle that is signalled when the lock on this resource weakens, creating it on first use.
            /// </summary>
            public AutoResetEvent UnlockEvent
            {
                get
                {
                    /* Avoid race condition where two threads can create
                       two different AutoResetEvent objects for one resource */
                    lock (this)
                    {
                        _autoResetEvent ??= new AutoResetEvent(false);
                    }
                    return _autoResetEvent;
                }
            }

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ResourceEntry" /> class in the unlocked state.
            /// </summary>
            public ResourceEntry()
            {
                _transactions = [];
                _lockMode = LockMode.Null;
            }

            /// <summary>
            /// Releases the wait handle owned by this instance if <see cref="Dispose()" /> was never called.
            /// </summary>
            ~ResourceEntry()
            {
                Dispose(false);
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Releases the wait handle owned by this instance.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Determines which lock mode a transaction currently holds on this resource.
            /// </summary>
            /// <param name="txId">The transaction to look for.</param>
            /// <returns>The mode held by <paramref name="txId" />, or <see cref="LockMode.Null" /> when it holds no lock.</returns>
            public LockMode GetExistingLockMode(SimpleTransaction txId)
            {
                for (var i = 0; i < (int)LockMode._Length; i++)
                {
                    var tList = GetTransactionList((LockMode)i);
                    if (tList is not null
                        && tList.ContainsKey(txId))
                    {
                        return (LockMode)i;
                    }
                }
                return LockMode.Null;
            }

            /// <summary>
            /// Gets the set of transactions holding the given lock mode on this resource.
            /// </summary>
            /// <param name="mode">The lock mode to look up.</param>
            /// <returns>The set of holders, or <see langword="null" /> when no transaction has ever held that mode here.</returns>
            public Dictionary<SimpleTransaction, SimpleTransaction> GetTransactionList(LockMode mode)
            {
                Dictionary<SimpleTransaction, SimpleTransaction> tList = null;
                if (_transactions.ContainsKey(mode))
                {
                    tList = _transactions[mode];
                }

                return tList;
            }

            /// <summary>
            /// Records that a transaction holds a lock of the given mode on this resource.
            /// </summary>
            /// <param name="context">the context for which to record the lock</param>
            /// <param name="request">the mode of the lock being granted</param>
            /// <remarks>
            /// The wait handle is reset so that a waiter woken by an earlier release does not mistake the newly
            /// granted lock for an opportunity to proceed.
            /// </remarks>
            public void Register(SimpleTransaction context, LockMode request)
            {
                var transactionList = GetTransactionList(request);
                if (transactionList is null)
                {
                    transactionList = [];
                    _transactions[request] = transactionList;
                }

                // Add the transaction to the list for _request_ lock mode
                transactionList[context] = context;

                // Update the strongest lock mode, if necessary
                if (request > _lockMode)
                {
                    _lockMode = request;
                }

                _autoResetEvent?.Reset();
            }

            /// <summary>
            /// Release a lock on the passed in transaction context with
            /// the specified mode. Return immediately if the context does
            /// not hold a lock for this mode.
            /// </summary>
            /// <param name="context">the context for which to remove the lock</param>
            /// <param name="request">the mode of the lock to be released</param>
            /// <remarks>
            /// After the release the strongest remaining mode is recomputed by walking down from the released mode;
            /// waiters are only woken when that recomputation actually weakened the lock.
            /// </remarks>
            public void Unregister(SimpleTransaction context, LockMode request)
            {
                // First get the hash table for this lock mode
                var transactionList = GetTransactionList(request);

                if (transactionList is null
                    || !transactionList.ContainsKey(context))
                {
                    // This transaction wasn't registered, return immediately
                    return;
                }

                transactionList.Remove(context);

                for (var l = request; l > LockMode.Null; _lockMode = --l)
                {
                    // recalculate the strongest lock mode
                    var nextTransactionList = GetTransactionList(l);
                    if (nextTransactionList is null)
                    {
                        continue;
                    }
                    if (nextTransactionList.Count > 0)
                    {
                        break;
                    }
                }

                if (request > _lockMode)
                {
                    // if anyone was waiting for this lock, they should recheck
                    _autoResetEvent?.Set();
                }
            }

            #endregion

            #region Internal Methods

            /// <summary>
            /// Determines whether a lock request can be granted while the current lock on this resource is held.
            /// </summary>
            /// <param name="request">The lock mode being requested.</param>
            /// <returns><see langword="true" /> when the request is compatible with the mode currently held; otherwise <see langword="false" />.</returns>
            internal bool Compatible(LockMode request)
            {
                return CompatibilityTable[(int)_lockMode][(int)request];
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// Releases the wait handle owned by this instance.
            /// </summary>
            /// <param name="disposing"><see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from the finalizer.</param>
            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _autoResetEvent?.Dispose();
                }
            }

            #endregion

        }

        #endregion

    }

}
