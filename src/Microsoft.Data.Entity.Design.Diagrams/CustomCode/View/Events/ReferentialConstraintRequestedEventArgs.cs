// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Diagrams.View.Events
{
    /// <summary>
    ///     Asks the host to let the user edit the referential constraint on an association.
    /// </summary>
    /// <remarks>
    ///     The host returns commands rather than mutating the model itself, so the designer stays in charge of
    ///     when and in what transaction they run. An unhandled request leaves <see cref="Commands" /> empty and
    ///     nothing happens. See specs/platform-independence.md.
    /// </remarks>
    internal sealed class ReferentialConstraintRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The association whose referential constraint is being edited.
        /// </summary>
        internal Association Association { get; }

        /// <summary>
        ///     Commands describing the user's changes, to be run by the designer. Empty if nothing changed.
        /// </summary>
        internal IEnumerable<Command> Commands { get; set; } = [];

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request against <paramref name="association" />.
        /// </summary>
        /// <param name="association">The association whose referential constraint is being edited.</param>
        internal ReferentialConstraintRequestedEventArgs(Association association)
        {
            Association = association ?? throw new ArgumentNullException(nameof(association));
        }

        #endregion

    }
}
