// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections;
using Microsoft.Data.Entity.Design.Diagrams.View;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     Arranges the shapes on an <see cref="EntityDesignerSurface" />.
    /// </summary>
    /// <remarks>
    ///     Implementations are held by <see cref="LayoutEngineManager" /> and selected through its
    ///     <see cref="LayoutEngineManager.Current" /> property, so a surface never names a concrete engine.
    ///     An engine owns its own transaction and progress reporting, because how much work it does — and
    ///     therefore whether the user needs to be told about it — is the engine's business rather than the
    ///     surface's. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal abstract class LayoutEngineBase
    {

        #region Properties

        /// <summary>
        ///     The name shown to the user when this engine is offered as a choice.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        ///     The stable identifier this engine is registered under.
        /// </summary>
        /// <remarks>
        ///     Persisted and passed on the command line, so it must not change once shipped. It is not the
        ///     same thing as <see cref="DisplayName" />, which is free to be localized or reworded.
        /// </remarks>
        public abstract string Key { get; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Arranges <paramref name="shapes" /> on <paramref name="surface" />.
        /// </summary>
        /// <param name="surface">The diagram whose shapes are being arranged.</param>
        /// <param name="shapes">
        ///     The shapes to arrange. Callers pass anything from the whole of
        ///     <see cref="Microsoft.VisualStudio.Modeling.Diagrams.ShapeElement.NestedChildShapes" /> down to the
        ///     handful of shapes a drag-and-drop just created.
        /// </param>
        /// <remarks>
        ///     A weakly typed <see cref="IList" /> rather than <c>IList&lt;ShapeElement&gt;</c> because that is
        ///     what the Modeling SDK's own <c>AutoLayoutShapeElements</c> takes and what every existing caller
        ///     already holds. Tightening it would push a cast onto all five call sites and buy nothing.
        /// </remarks>
        public abstract void Layout(EntityDesignerSurface surface, IList shapes);

        #endregion

    }

}
