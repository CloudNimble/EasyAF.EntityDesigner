// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Dsl.CustomCode.Utils;
using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Microsoft.Data.Entity.Design.Dsl.View
{
    /// <summary>
    ///     Holds the <see cref="DiagramPalette" /> in force and re-themes the designer when it changes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The host pushes colors in; the designer never reaches out for them. Visual Studio calls
    ///         <see cref="Apply" /> at package initialization and again whenever the theme changes. Nothing calls
    ///         it in the command line renderer, so <see cref="DiagramPalette" />'s defaults stand and no style set
    ///         is overridden at all — which is exactly what happened before, when the theme lookup silently gave
    ///         up outside Visual Studio.
    ///     </para>
    ///     <para>
    ///         Deliberately not an event. The designer used to subscribe a closure to the static
    ///         <c>VSColorTheme.ThemeChanged</c> from <c>InitializeResources</c> and never unsubscribed, because a
    ///         shape class has no teardown point. Having the host push instead puts the subscription in the
    ///         package, which does have a lifetime, and the handler can be removed.
    ///     </para>
    ///     <para>
    ///         <b>Single threaded</b>, like everything else built on the Modeling SDK. See
    ///         specs/threading-model.md.
    ///     </para>
    /// </remarks>
    internal static class DiagramTheme
    {

        #region Fields

        /// <summary>
        ///     Used when no host has supplied a palette.
        /// </summary>
        private static readonly DiagramPalette FallbackPalette = new DiagramPalette();

        /// <summary>
        ///     Class style sets that have been created, so a later <see cref="Apply" /> can re-theme them.
        /// </summary>
        /// <remarks>
        ///     Bounded and long lived: the Modeling SDK caches one style set per shape class in a static field,
        ///     so this holds a handful of entries for the life of the process.
        /// </remarks>
        private static readonly List<StyleSet> RegisteredStyleSets = [];

        private static DiagramPalette _palette;

        #endregion

        #region Properties

        /// <summary>
        ///     The palette currently in force, never null.
        /// </summary>
        internal static DiagramPalette Current
        {
            get { return _palette ?? FallbackPalette; }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Adopts <paramref name="palette" /> and re-themes everything already created.
        /// </summary>
        /// <param name="palette">The colors to draw with.</param>
        internal static void Apply(DiagramPalette palette)
        {
            if (palette is null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            _palette = palette;

            foreach (var styleSet in RegisteredStyleSets)
            {
                ApplyTo(styleSet);
            }

            // Shapes and connectors cache their themed colors and repaint from the cache, so tell them the cache
            // is stale rather than pushing colors at them.
            EntityTypeShape.IsColorThemeSet = false;
            AssociationConnector.IsColorThemeSet = false;

            // Property icons are colorized against the compartment fill, so they have to be rebuilt too — but
            // that is GDI recolouring, which belongs to whoever supplied the palette. A host that pushes a
            // palette pushes icons with it. See specs/layer-map.md.
        }

        /// <summary>
        ///     Records a class style set so a later <see cref="Apply" /> can re-theme it, and themes it now if a
        ///     palette is already in force.
        /// </summary>
        /// <param name="styleSet">The class style set being initialized.</param>
        /// <remarks>
        ///     Registration and <see cref="Apply" /> can happen in either order: style sets are created lazily on
        ///     first use, which may be before or after the package initializes.
        /// </remarks>
        internal static void Register(StyleSet styleSet)
        {
            if (styleSet is null)
            {
                throw new ArgumentNullException(nameof(styleSet));
            }

            if (!RegisteredStyleSets.Contains(styleSet))
            {
                RegisteredStyleSets.Add(styleSet);
            }

            if (_palette is not null)
            {
                ApplyTo(styleSet);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Writes the current palette's surface colors into <paramref name="styleSet" />.
        /// </summary>
        private static void ApplyTo(StyleSet styleSet)
        {
            styleSet.OverrideBrushColor(DiagramBrushes.DiagramBackground, Current.DiagramBackground);
            styleSet.OverridePenColor(DiagramPens.ZoomLasso, Current.ZoomLasso);
        }

        #endregion

    }
}
