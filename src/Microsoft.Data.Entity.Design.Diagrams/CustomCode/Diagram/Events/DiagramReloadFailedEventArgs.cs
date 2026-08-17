// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Dsl.View.Events
{
    /// <summary>
    ///     Reports that the designer could not rebuild itself from the model, and needs the user told.
    /// </summary>
    /// <remarks>
    ///     Raised at most once per designer: after the first failure the diagram is in an unknown state and every
    ///     subsequent operation tends to fail the same way, so repeating the message only buries it. Where the
    ///     message goes — an error list, a log file, nowhere — is the host's business. See
    ///     specs/platform-independence.md.
    /// </remarks>
    internal sealed class DiagramReloadFailedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The local path of the artifact the designer was rebuilding.
        /// </summary>
        internal string ArtifactPath { get; }

        /// <summary>
        ///     The message describing the failure, already localized.
        /// </summary>
        internal string Message { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a report describing a failed reload.
        /// </summary>
        /// <param name="message">The localized message describing the failure.</param>
        /// <param name="artifactPath">The local path of the artifact being rebuilt.</param>
        internal DiagramReloadFailedEventArgs(string message, string artifactPath)
        {
            Message = message;
            ArtifactPath = artifactPath;
        }

        #endregion

    }
}
