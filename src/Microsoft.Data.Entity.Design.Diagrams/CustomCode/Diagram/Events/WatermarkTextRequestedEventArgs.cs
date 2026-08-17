// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Dsl.View.Events
{
    /// <summary>
    ///     Offers the host the chance to replace the watermark the designer chose.
    /// </summary>
    /// <remarks>
    ///     The designer decides the watermark from what it can see — whether the model is structurally sound and
    ///     whether its schema version is usable. A host may know something the designer cannot, such as whether the
    ///     containing project supports Entity Framework at all, and can say so instead. Leaving
    ///     <see cref="Text" /> alone keeps the designer's own wording, which is what happens with nothing
    ///     subscribed. See specs/platform-independence.md.
    /// </remarks>
    internal sealed class WatermarkTextRequestedEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        ///     The watermark to display. Starts as the designer's own choice; a handler may replace it.
        /// </summary>
        internal string Text { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a request carrying the designer's chosen watermark.
        /// </summary>
        /// <param name="text">The watermark the designer chose.</param>
        internal WatermarkTextRequestedEventArgs(string text)
        {
            Text = text;
        }

        #endregion

    }
}
