// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Diagrams.View.Events
{
    /// <summary>
    ///     Asks the host whether storage entity sets left unmapped by a delete should also be deleted.
    /// </summary>
    /// <remarks>
    ///     Three outcomes, so <see cref="DeleteUnmappedSets" /> is nullable rather than a bool: delete the storage
    ///     sets too, delete only what was selected, or abandon the delete entirely. A host that does not answer
    ///     leaves it null, which cancels — the safe choice, since the alternative silently discards model the user
    ///     did not ask to lose. See specs/platform-independence.md.
    /// </remarks>
    internal sealed class UnmappedStorageEntitySetsDeletionRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     True to delete the unmapped storage sets as well, false to delete only the selected elements, null
        ///     to cancel the whole operation.
        /// </summary>
        internal bool? DeleteUnmappedSets { get; set; }

        /// <summary>
        ///     The storage entity sets that this delete would leave unmapped.
        /// </summary>
        internal ICollection<StorageEntitySet> UnmappedStorageEntitySets { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request describing <paramref name="unmappedStorageEntitySets" />.
        /// </summary>
        /// <param name="unmappedStorageEntitySets">The sets that would be left unmapped.</param>
        internal UnmappedStorageEntitySetsDeletionRequestedEventArgs(ICollection<StorageEntitySet> unmappedStorageEntitySets)
        {
            UnmappedStorageEntitySets = unmappedStorageEntitySets
                                        ?? throw new ArgumentNullException(nameof(unmappedStorageEntitySets));
        }

        #endregion

    }
}
