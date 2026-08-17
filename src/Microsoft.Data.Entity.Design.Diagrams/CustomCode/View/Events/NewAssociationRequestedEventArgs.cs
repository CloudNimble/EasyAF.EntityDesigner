// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Edmx.Entity;

namespace Microsoft.Data.Entity.Design.Diagrams.View.Events
{
    /// <summary>
    ///     Asks the host to collect the details of a new association.
    /// </summary>
    /// <remarks>
    ///     The suggested ends are the designer's best guess from the current selection. A host may use them as
    ///     defaults and let the user change them, or accept them unchanged. See specs/platform-independence.md.
    /// </remarks>
    internal sealed class NewAssociationRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     Name for the new association.
        /// </summary>
        internal string AssociationName { get; set; }

        /// <summary>
        ///     Whether the request was declined. Starts true, so an unhandled request creates nothing.
        /// </summary>
        internal bool Cancelled { get; set; } = true;

        /// <summary>
        ///     Whether to create foreign key properties for the association.
        /// </summary>
        internal bool CreateForeignKeyProperties { get; set; }

        /// <summary>
        ///     Entity type at the first end.
        /// </summary>
        internal ConceptualEntityType End1Entity { get; set; }

        /// <summary>
        ///     Multiplicity of the first end.
        /// </summary>
        internal string End1Multiplicity { get; set; }

        /// <summary>
        ///     Navigation property name on the first end, or empty for none.
        /// </summary>
        internal string End1NavigationPropertyName { get; set; }

        /// <summary>
        ///     Entity type at the second end.
        /// </summary>
        internal ConceptualEntityType End2Entity { get; set; }

        /// <summary>
        ///     Multiplicity of the second end.
        /// </summary>
        internal string End2Multiplicity { get; set; }

        /// <summary>
        ///     Navigation property name on the second end, or empty for none.
        /// </summary>
        internal string End2NavigationPropertyName { get; set; }

        /// <summary>
        ///     Entity types available to be either end.
        /// </summary>
        internal IEnumerable<EntityType> EntityTypes { get; }

        /// <summary>
        ///     The end the designer suggests for the first end, based on the current selection.
        /// </summary>
        internal EntityType SuggestedEnd1 { get; }

        /// <summary>
        ///     The end the designer suggests for the second end.
        /// </summary>
        internal EntityType SuggestedEnd2 { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request offering <paramref name="entityTypes" /> and the suggested ends.
        /// </summary>
        /// <param name="entityTypes">Entity types available to be either end.</param>
        /// <param name="suggestedEnd1">Suggested first end.</param>
        /// <param name="suggestedEnd2">Suggested second end.</param>
        internal NewAssociationRequestedEventArgs(
            IEnumerable<EntityType> entityTypes, EntityType suggestedEnd1, EntityType suggestedEnd2)
        {
            EntityTypes = entityTypes ?? throw new ArgumentNullException(nameof(entityTypes));
            SuggestedEnd1 = suggestedEnd1;
            SuggestedEnd2 = suggestedEnd2;
        }

        #endregion

    }
}
