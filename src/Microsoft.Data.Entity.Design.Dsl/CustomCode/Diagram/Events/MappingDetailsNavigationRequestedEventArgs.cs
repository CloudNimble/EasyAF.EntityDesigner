// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Model;

namespace Microsoft.Data.Entity.Design.Dsl.View.Events
{
    /// <summary>
    ///     Reports that navigation landed on a mapping element, so a host showing mapping details can follow.
    /// </summary>
    /// <remarks>
    ///     The designer navigates its own shapes and then says where it ended up. Whether anything else follows —
    ///     a mapping details window, a log line, nothing at all — is the host's business. See
    ///     specs/platform-independence.md.
    /// </remarks>
    internal sealed class MappingDetailsNavigationRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The mapping element that was navigated to.
        /// </summary>
        internal EFObject MappingElement { get; }

        /// <summary>
        ///     Whether the element maps through modification functions rather than tables, or <see langword="null" />
        ///     if the navigation says nothing either way and any existing choice should be left alone.
        /// </summary>
        /// <remarks>
        ///     Reported as a fact about the model rather than as a display mode, because which of the host's tabs
        ///     that corresponds to — or whether the host has tabs at all — is not the designer's business.
        /// </remarks>
        internal bool? UsesFunctionMapping { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a report describing where navigation landed.
        /// </summary>
        /// <param name="mappingElement">The mapping element that was navigated to.</param>
        /// <param name="usesFunctionMapping">
        ///     Whether the element maps through modification functions, or <see langword="null" /> if unknown.
        /// </param>
        internal MappingDetailsNavigationRequestedEventArgs(EFObject mappingElement, bool? usesFunctionMapping)
        {
            MappingElement = mappingElement ?? throw new ArgumentNullException(nameof(mappingElement));
            UsesFunctionMapping = usesFunctionMapping;
        }

        #endregion

    }
}
