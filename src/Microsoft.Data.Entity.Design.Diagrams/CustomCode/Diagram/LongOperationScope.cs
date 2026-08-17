// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Dsl.View
{
    /// <summary>
    ///     Reports the end of a long designer operation when disposed.
    /// </summary>
    /// <remarks>
    ///     Returned by <see cref="EntityDesignerSurface.BeginLongOperation" />. Deliberately a designer-owned type
    ///     rather than something the host supplies: the designer says when work starts and stops, and the host
    ///     decides what, if anything, to show. See specs/layer-map.md.
    /// </remarks>
    internal readonly struct LongOperationScope : IDisposable
    {

        #region Fields

        private readonly EntityDesignerSurface _surface;

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a scope that reports back to <paramref name="surface" /> when it ends.
        /// </summary>
        /// <param name="surface">The surface running the operation.</param>
        internal LongOperationScope(EntityDesignerSurface surface)
        {
            _surface = surface;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Reports that the operation has finished.
        /// </summary>
        public void Dispose()
        {
            _surface?.OnLongOperationEnded();
        }

        #endregion

    }
}
