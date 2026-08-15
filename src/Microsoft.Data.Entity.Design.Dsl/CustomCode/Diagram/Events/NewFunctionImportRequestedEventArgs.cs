// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using System;

namespace Microsoft.Data.Entity.Design.Dsl.View.Events
{
    /// <summary>
    ///     Asks the host to collect the details of a new function import and create it.
    /// </summary>
    /// <remarks>
    ///     The designer resolves the model elements a function import needs to sit between — the storage model it
    ///     reads from, the conceptual container it lands in, and the entity type to return — but choosing the
    ///     stored procedure is a conversation with the user. With nothing subscribed no function import is
    ///     created, which is the right outcome for a host that cannot ask. See specs/platform-independence.md.
    /// </remarks>
    internal sealed class NewFunctionImportRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The artifact the function import belongs to.
        /// </summary>
        internal EFArtifact Artifact { get; }

        /// <summary>
        ///     The conceptual entity container the function import lands in.
        /// </summary>
        internal ConceptualEntityContainer ConceptualContainer { get; }

        /// <summary>
        ///     The conceptual model the function import lands in.
        /// </summary>
        internal ConceptualEntityModel ConceptualModel { get; }

        /// <summary>
        ///     The editing context for the document.
        /// </summary>
        internal EditingContext EditingContext { get; }

        /// <summary>
        ///     The entity type to return, or <see langword="null" /> when nothing was selected.
        /// </summary>
        internal EntityType ReturnEntityType { get; }

        /// <summary>
        ///     The storage model holding the functions that can be imported.
        /// </summary>
        internal StorageEntityModel StorageModel { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request for a new function import.
        /// </summary>
        /// <param name="editingContext">The editing context for the document.</param>
        /// <param name="artifact">The artifact the function import belongs to.</param>
        /// <param name="storageModel">The storage model holding the importable functions.</param>
        /// <param name="conceptualModel">The conceptual model the function import lands in.</param>
        /// <param name="conceptualContainer">The conceptual entity container the function import lands in.</param>
        /// <param name="returnEntityType">The entity type to return, or <see langword="null" /> for none.</param>
        internal NewFunctionImportRequestedEventArgs(
            EditingContext editingContext, EFArtifact artifact, StorageEntityModel storageModel,
            ConceptualEntityModel conceptualModel, ConceptualEntityContainer conceptualContainer, EntityType returnEntityType)
        {
            EditingContext = editingContext;
            Artifact = artifact;
            StorageModel = storageModel;
            ConceptualModel = conceptualModel;
            ConceptualContainer = conceptualContainer;
            ReturnEntityType = returnEntityType;
        }

        #endregion

    }
}
