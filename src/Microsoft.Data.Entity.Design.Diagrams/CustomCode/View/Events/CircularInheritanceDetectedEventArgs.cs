// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using System;

namespace Microsoft.Data.Entity.Design.Diagrams.View.Events
{
    /// <summary>
    ///     Reports that an inheritance was abandoned because it would have been circular.
    /// </summary>
    /// <remarks>
    ///     Carries the two entity types rather than a message. The designer states what happened; the host decides
    ///     how to say it, in its own words and its own language. A host that ignores this simply gets no
    ///     inheritance, which is already the outcome. See specs/platform-independence.md.
    /// </remarks>
    internal sealed class CircularInheritanceDetectedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The type that would have become the base type.
        /// </summary>
        internal ConceptualEntityType BaseEntityType { get; }

        /// <summary>
        ///     The type whose base type was being set.
        /// </summary>
        internal ConceptualEntityType DerivedEntityType { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a report describing the rejected inheritance.
        /// </summary>
        /// <param name="derivedEntityType">The type whose base type was being set.</param>
        /// <param name="baseEntityType">The type that would have become the base type.</param>
        internal CircularInheritanceDetectedEventArgs(
            ConceptualEntityType derivedEntityType, ConceptualEntityType baseEntityType)
        {
            DerivedEntityType = derivedEntityType;
            BaseEntityType = baseEntityType;
        }

        #endregion

    }
}
