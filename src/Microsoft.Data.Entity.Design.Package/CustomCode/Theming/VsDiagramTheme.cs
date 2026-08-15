// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.Package.Theming
{
    /// <summary>
    ///     Pushes Visual Studio's themed colors into the diagram designer, and keeps them current.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is the only place that maps Visual Studio color keys onto the designer's palette. The Dsl
    ///         project names the colors it wants and knows nothing about <see cref="VSColorTheme" />, which is
    ///         what lets the same assembly run under the command line renderer where no shell exists.
    ///     </para>
    ///     <para>
    ///         The subscription lives here rather than in the designer because the package has a lifetime and can
    ///         unsubscribe. The designer previously hooked the static <c>VSColorTheme.ThemeChanged</c> from
    ///         <c>InitializeResources</c> and had nowhere to unhook, leaving the only unpaired handler in the
    ///         solution.
    ///     </para>
    /// </remarks>
    internal sealed class VsDiagramTheme : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Constructors

        /// <summary>
        ///     Applies the current theme and starts tracking changes to it.
        /// </summary>
        internal VsDiagramTheme()
        {
            VSColorTheme.ThemeChanged += OnThemeChanged;
            DiagramTheme.Apply(ReadPalette());
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Stops tracking theme changes.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            VSColorTheme.ThemeChanged -= OnThemeChanged;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Reads the designer's colors out of the current Visual Studio theme.
        /// </summary>
        private static DiagramPalette ReadPalette()
        {
            return new DiagramPalette
            {
                DiagramBackground = VSColorTheme.GetThemedColor(EnvironmentColors.DesignerBackgroundColorKey),
                ZoomLasso = VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerLassoColorKey),
                EmphasisOutline = VSColorTheme.GetThemedColor(EnvironmentColors.ClassDesignerEmphasisBorderColorKey),
                ShapeText = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey)

                // CompartmentFill is deliberately left at its default. Compartments are painted with the fill
                // color declared in DslDefinition.dsl, which is not themed, so colorizing the property icons
                // against anything else would make them disagree with what is drawn behind them.
            };
        }

        /// <summary>
        ///     Re-reads the palette when the user changes theme.
        /// </summary>
        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            DiagramTheme.Apply(ReadPalette());
        }

        #endregion

    }
}
