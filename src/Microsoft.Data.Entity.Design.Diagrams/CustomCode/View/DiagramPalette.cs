// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Drawing;

namespace Microsoft.Data.Entity.Design.Diagrams.View
{
    /// <summary>
    ///     The colors the diagram surface, its shapes and its connectors draw with.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The designer names the colors it needs; it does not know where they come from. A host that has a
    ///         theme supplies one through <see cref="DiagramTheme.Apply" />. A host that does not — the command
    ///         line renderer — supplies nothing, and these defaults stand.
    ///     </para>
    ///     <para>
    ///         This exists so no code on a headless path has to ask whether Visual Studio is present. That
    ///         question used to be answered by resolving <c>SVsUIShell</c> from the global service provider, which
    ///         is both the wrong question and, under .NET 10, a throwing one.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     DiagramTheme.Apply(new DiagramPalette { DiagramBackground = Color.FromArgb(45, 45, 48) });
    ///     </code>
    /// </example>
    internal sealed class DiagramPalette
    {

        #region Properties

        /// <summary>
        ///     Fill drawn behind the property icons in a shape's compartments. Icons are colorized against it,
        ///     so it has to match whatever the compartments are actually painted with.
        /// </summary>
        internal Color CompartmentFill { get; set; } = Color.WhiteSmoke;

        /// <summary>
        ///     Background of the diagram surface.
        /// </summary>
        internal Color DiagramBackground { get; set; } = Color.White;

        /// <summary>
        ///     Outline drawn around shapes emphasized because of the current selection.
        /// </summary>
        internal Color EmphasisOutline { get; set; } = SystemColors.Highlight;

        /// <summary>
        ///     Text drawn on shapes and connectors, such as association cardinalities.
        /// </summary>
        internal Color ShapeText { get; set; } = Color.Black;

        /// <summary>
        ///     Lasso drawn while rubber band selecting or zooming.
        /// </summary>
        internal Color ZoomLasso { get; set; } = SystemColors.Highlight;

        #endregion

    }
}
