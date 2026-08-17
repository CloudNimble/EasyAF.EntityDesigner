// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone
{

    /// <summary>
    /// The strength of a lock held on a document by a <see cref="SimpleTransaction" />.
    /// </summary>
    /// <remarks>
    /// The members are ordered from weakest to strongest so <see cref="SimpleLockManager" /> can compare them with
    /// the relational operators when recalculating the strongest lock held on a resource, and can use their integer
    /// values to index the lock compatibility table. <see cref="_Length" /> is a sentinel, not a real lock mode.
    /// </remarks>
    internal enum LockMode
    {

        /// <summary>
        /// No lock is held.
        /// </summary>
        Null,

        /// <summary>
        /// Read Mode. Shared with other readers.
        /// </summary>
        Read, // Read Mode

        /// <summary>
        /// Write Mode. Exclusive of readers and other writers.
        /// </summary>
        Write, // Write Mode

        /// <summary>
        /// The number of LockModes. Used to size the compatibility table and to bound iteration; never a valid mode.
        /// </summary>
        _Length // The number of LockModes

    }

}
