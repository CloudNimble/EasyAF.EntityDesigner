// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Diagrams.View.Events
{
    /// <summary>
    ///     Asks the host to collect the base and derived types for a new inheritance.
    /// </summary>
    /// <remarks>
    ///     See specs/platform-independence.md.
    /// </remarks>
    internal sealed class NewInheritanceRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The chosen base type.
        /// </summary>
        internal ConceptualEntityType BaseEntityType { get; set; }

        /// <summary>
        ///     Whether the request was declined. Starts true, so an unhandled request creates nothing.
        /// </summary>
        internal bool Cancelled { get; set; } = true;

        /// <summary>
        ///     The chosen derived type.
        /// </summary>
        internal EntityType DerivedEntityType { get; set; }

        /// <summary>
        ///     Entity types available to be either end of the inheritance.
        /// </summary>
        internal IList<ConceptualEntityType> EntityTypes { get; }

        /// <summary>
        ///     The base type the designer suggests, from the current selection, or null if nothing was selected.
        /// </summary>
        internal ConceptualEntityType SuggestedBaseEntityType { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request offering <paramref name="entityTypes" />.
        /// </summary>
        /// <param name="entityTypes">Entity types available to be either end.</param>
        /// <param name="suggestedBaseEntityType">Base type suggested by the current selection, may be null.</param>
        internal NewInheritanceRequestedEventArgs(
            IList<ConceptualEntityType> entityTypes, ConceptualEntityType suggestedBaseEntityType)
        {
            EntityTypes = entityTypes ?? throw new ArgumentNullException(nameof(entityTypes));
            SuggestedBaseEntityType = suggestedBaseEntityType;
        }

        #endregion

    }
}
