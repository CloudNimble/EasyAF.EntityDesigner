// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Modeling.Diagrams;
using System;

namespace Microsoft.Data.Entity.Design.Diagrams.View.Events
{
    /// <summary>
    ///     Reports that a view's watermark has been rebuilt and any clickable links on it need attaching.
    /// </summary>
    /// <remarks>
    ///     Every link the watermark can offer — open the toolbox, show the model browser, edit the XML, upgrade the
    ///     schema version — is a command against the host, so the host builds them. With nothing subscribed the
    ///     watermark is plain text, which is correct for a renderer that has no windows to open. See
    ///     specs/platform-independence.md.
    /// </remarks>
    internal sealed class WatermarkLinksRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The view whose watermark was rebuilt.
        /// </summary>
        internal DiagramView DiagramView { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request for the links on one view's watermark.
        /// </summary>
        /// <param name="diagramView">The view whose watermark was rebuilt.</param>
        internal WatermarkLinksRequestedEventArgs(DiagramView diagramView)
        {
            DiagramView = diagramView ?? throw new ArgumentNullException(nameof(diagramView));
        }

        #endregion

    }
}
