// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Edmx.Entity;

namespace Microsoft.Data.Entity.Design.Diagrams.View.Events
{
    /// <summary>
    ///     Asks the host to collect the details of a new entity type.
    /// </summary>
    /// <remarks>
    ///     The designer knows it needs an entity type; it does not know how the values are gathered. Visual Studio
    ///     answers this with a dialog, the command line could answer it from arguments, and a host that answers
    ///     nothing leaves <see cref="Cancelled" /> set and no entity type is created. See
    ///     specs/platform-independence.md.
    /// </remarks>
    internal sealed class NewEntityTypeRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     Base type for the new entity, or null to create a root entity type.
        /// </summary>
        internal ConceptualEntityType BaseEntityType { get; set; }

        /// <summary>
        ///     Whether the request was declined. Starts true, so an unhandled request creates nothing.
        /// </summary>
        internal bool Cancelled { get; set; } = true;

        /// <summary>
        ///     Whether to create a key property. Ignored when <see cref="BaseEntityType" /> is set.
        /// </summary>
        internal bool CreateKeyProperty { get; set; }

        /// <summary>
        ///     Name for the new entity type.
        /// </summary>
        internal string EntityName { get; set; }

        /// <summary>
        ///     Name for the new entity set.
        /// </summary>
        internal string EntitySetName { get; set; }

        /// <summary>
        ///     Name of the key property to create.
        /// </summary>
        internal string KeyPropertyName { get; set; }

        /// <summary>
        ///     Type of the key property to create.
        /// </summary>
        internal string KeyPropertyType { get; set; }

        /// <summary>
        ///     The conceptual model the entity type will be added to.
        /// </summary>
        internal ConceptualEntityModel Model { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request against <paramref name="model" />.
        /// </summary>
        /// <param name="model">The conceptual model the entity type will be added to.</param>
        internal NewEntityTypeRequestedEventArgs(ConceptualEntityModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        #endregion

    }
}
