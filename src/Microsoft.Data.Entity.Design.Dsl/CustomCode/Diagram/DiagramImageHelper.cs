// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Drawing;
using Microsoft.Data.Entity.Design.VisualStudio;
using EntityDesignerRes = Microsoft.Data.Entity.Design.EntityDesigner.Properties.Resources;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View
{
    /// <summary>
    /// Contains the set of header icons for an entity type shape, colorized to match the header text.
    /// </summary>
    internal sealed class HeaderIconSet
    {
        public HeaderIconSet(Bitmap entityGlyph, Bitmap baseTypeIcon, Bitmap chevronExpanded, Bitmap chevronCollapsed)
        {
            EntityGlyph = entityGlyph;
            BaseTypeIcon = baseTypeIcon;
            ChevronExpanded = chevronExpanded;
            ChevronCollapsed = chevronCollapsed;
        }

        public Bitmap EntityGlyph { get; }
        public Bitmap BaseTypeIcon { get; }
        public Bitmap ChevronExpanded { get; }
        public Bitmap ChevronCollapsed { get; }

        public void Dispose()
        {
            EntityGlyph?.Dispose();
            BaseTypeIcon?.Dispose();
            ChevronExpanded?.Dispose();
            ChevronCollapsed?.Dispose();
        }
    }

    /// <summary>
    /// A singleton helper that pre-loads and themes all property icons at diagram startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Icons are cached in two versions for each property type:
    /// <list type="bullet">
    ///   <item><description>100% zoom variant: Scaled to device pixels for crisp rendering at 100% zoom.</description></item>
    ///   <item><description>Normal variant: High-resolution with DPI metadata, allowing the DSL zoom transformation to scale appropriately.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This approach eliminates stutter on the first zoom change by pre-loading all icons
    /// when the diagram is first associated with a view, rather than lazily loading them
    /// on first use.
    /// </para>
    /// <para>
    /// The singleton properly disposes of existing icons when reloading due to theme changes,
    /// and guards against re-entrant calls during loading.
    /// </para>
    /// </remarks>
    internal sealed class DiagramImageHelper
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="DiagramImageHelper"/>.
        /// </summary>
        /// <value>The shared singleton instance used throughout the application.</value>
        public static DiagramImageHelper Instance { get; } = new DiagramImageHelper();

        /// <summary>
        /// Guard flag to prevent re-entrant calls to <see cref="Load"/> while loading is in progress.
        /// </summary>
        private bool _isLoading;

        /// <summary>
        /// Cache of header icons keyed by text color. Since text color is only ever black or white,
        /// this will contain at most 2 entries, avoiding redundant icon creation for entities
        /// with different fill colors but the same text color.
        /// </summary>
        private readonly Dictionary<Color, HeaderIconSet> _headerIconsByTextColor = [];

        // Source bitmaps for header icons (loaded once, reused for colorization)
        private static readonly Bitmap SourceEntityGlyph = EntityDesignerRes.EntityGlyph;
        private static readonly Bitmap SourceBaseTypeIcon = EntityDesignerRes.BaseTypeIcon;
        private static readonly Bitmap SourceChevronExpanded = EntityDesignerRes.ChevronExpanded;
        private static readonly Bitmap SourceChevronCollapsed = EntityDesignerRes.ChevronCollapsed;

        /// <summary>
        /// Gets the scalar property icon optimized for 100% zoom level.
        /// </summary>
        /// <value>
        /// A bitmap scaled to device pixels for crisp rendering when the diagram zoom is at 100%.
        /// Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image PropertyIcon100 { get; private set; }

        /// <summary>
        /// Gets the primary key property icon optimized for 100% zoom level.
        /// </summary>
        /// <value>
        /// A bitmap scaled to device pixels for crisp rendering when the diagram zoom is at 100%.
        /// Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image PropertyPKIcon100 { get; private set; }

        /// <summary>
        /// Gets the complex property icon optimized for 100% zoom level.
        /// </summary>
        /// <value>
        /// A bitmap scaled to device pixels for crisp rendering when the diagram zoom is at 100%.
        /// Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image ComplexPropertyIcon100 { get; private set; }

        /// <summary>
        /// Gets the navigation property icon optimized for 100% zoom level.
        /// </summary>
        /// <value>
        /// A bitmap scaled to device pixels for crisp rendering when the diagram zoom is at 100%.
        /// Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image NavigationPropertyIcon100 { get; private set; }

        /// <summary>
        /// Gets the scalar property icon for non-100% zoom levels.
        /// </summary>
        /// <value>
        /// A high-resolution bitmap with DPI metadata set so that the DSL zoom transformation
        /// can scale it appropriately. Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image PropertyIconNormal { get; private set; }

        /// <summary>
        /// Gets the primary key property icon for non-100% zoom levels.
        /// </summary>
        /// <value>
        /// A high-resolution bitmap with DPI metadata set so that the DSL zoom transformation
        /// can scale it appropriately. Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image PropertyPKIconNormal { get; private set; }

        /// <summary>
        /// Gets the complex property icon for non-100% zoom levels.
        /// </summary>
        /// <value>
        /// A high-resolution bitmap with DPI metadata set so that the DSL zoom transformation
        /// can scale it appropriately. Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image ComplexPropertyIconNormal { get; private set; }

        /// <summary>
        /// Gets the navigation property icon for non-100% zoom levels.
        /// </summary>
        /// <value>
        /// A high-resolution bitmap with DPI metadata set so that the DSL zoom transformation
        /// can scale it appropriately. Returns <c>null</c> if <see cref="Load"/> has not been called.
        /// </value>
        public Image NavigationPropertyIconNormal { get; private set; }

        /// <summary>
        /// Gets a value indicating whether icons have been loaded.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Load"/> has been called successfully; otherwise, <c>false</c>.
        /// This property is primarily useful for unit testing and for avoiding redundant loads.
        /// </value>
        internal bool IsLoaded { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagramImageHelper"/> class.
        /// </summary>
        /// <remarks>
        /// Private constructor to enforce the singleton pattern. Use <see cref="Instance"/> to access
        /// the shared instance.
        /// </remarks>
        private DiagramImageHelper()
        {
        }

        /// <summary>
        /// Loads and themes all property icons using the specified compartment fill color.
        /// </summary>
        /// <param name="compartmentFillColor">
        /// The background color of the compartment used for theming the icons.
        /// This is typically <see cref="Color.WhiteSmoke"/> as defined in the DSL model.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method should be called when the diagram is first associated with a view
        /// (in <c>OnAssociated</c>). It creates themed versions of all property icons in
        /// two variants: one optimized for 100% zoom and one for other zoom levels.
        /// </para>
        /// <para>
        /// If icons were previously loaded, they are disposed before new ones are created.
        /// Re-entrant calls while loading is in progress are ignored.
        /// </para>
        /// </remarks>
        public void Load(Color compartmentFillColor)
        {
            if (_isLoading)
            {
                return;
            }

            try
            {
                _isLoading = true;
                DisposeAllIcons();

                PropertyIcon100 = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.Property, compartmentFillColor, true);
                PropertyIconNormal = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.Property, compartmentFillColor, false);

                PropertyPKIcon100 = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.PropertyPK, compartmentFillColor, true);
                PropertyPKIconNormal = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.PropertyPK, compartmentFillColor, false);

                ComplexPropertyIcon100 = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.ComplexProperty, compartmentFillColor, true);
                ComplexPropertyIconNormal = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.ComplexProperty, compartmentFillColor, false);

                NavigationPropertyIcon100 = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.NavigationProperty, compartmentFillColor, true);
                NavigationPropertyIconNormal = ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.NavigationProperty, compartmentFillColor, false);

                IsLoaded = true;
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Reloads all icons when the Visual Studio theme changes.
        /// </summary>
        /// <param name="compartmentFillColor">
        /// The background color of the compartment used for theming the icons.
        /// This is typically <see cref="Color.WhiteSmoke"/> as defined in the DSL model.
        /// </param>
        /// <remarks>
        /// This method should be called from the theme change handler (e.g., <c>VSColorTheme.ThemeChanged</c>)
        /// to ensure icons are properly themed for the new Visual Studio color scheme.
        /// </remarks>
        public void OnThemeChanged(Color compartmentFillColor)
        {
            Load(compartmentFillColor);
        }

        /// <summary>
        /// Gets header icons colorized for the specified text color.
        /// </summary>
        /// <param name="textColor">The text color (typically black or white based on fill brightness).</param>
        /// <returns>A cached or newly created set of header icons matching the text color.</returns>
        /// <remarks>
        /// Since text color is only ever black or white, this cache will contain at most 2 entries,
        /// dramatically reducing icon creation for diagrams with many entities of the same brightness.
        /// </remarks>
        public HeaderIconSet GetHeaderIcons(Color textColor)
        {
            if (_headerIconsByTextColor.TryGetValue(textColor, out var existingSet))
            {
                return existingSet;
            }

            var newSet = new HeaderIconSet(
                ThemeUtils.GetColorizedHeaderIcon(SourceEntityGlyph, textColor),
                ThemeUtils.GetColorizedHeaderIcon(SourceBaseTypeIcon, textColor),
                ThemeUtils.GetColorizedHeaderIcon(SourceChevronExpanded, textColor),
                ThemeUtils.GetColorizedHeaderIcon(SourceChevronCollapsed, textColor));

            _headerIconsByTextColor[textColor] = newSet;
            return newSet;
        }

        /// <summary>
        /// Disposes all currently loaded icons and resets the loaded state.
        /// </summary>
        /// <remarks>
        /// This method safely disposes each icon if it exists, sets all icon properties to <c>null</c>,
        /// and sets <see cref="IsLoaded"/> to <c>false</c>. It is called internally before loading
        /// new icons to prevent memory leaks.
        /// </remarks>
        private void DisposeAllIcons()
        {
            PropertyIcon100?.Dispose();
            PropertyIconNormal?.Dispose();
            PropertyPKIcon100?.Dispose();
            PropertyPKIconNormal?.Dispose();
            ComplexPropertyIcon100?.Dispose();
            ComplexPropertyIconNormal?.Dispose();
            NavigationPropertyIcon100?.Dispose();
            NavigationPropertyIconNormal?.Dispose();

            PropertyIcon100 = null;
            PropertyIconNormal = null;
            PropertyPKIcon100 = null;
            PropertyPKIconNormal = null;
            ComplexPropertyIcon100 = null;
            ComplexPropertyIconNormal = null;
            NavigationPropertyIcon100 = null;
            NavigationPropertyIconNormal = null;

            // Dispose and clear header icons cache
            foreach (var iconSet in _headerIconsByTextColor.Values)
            {
                iconSet.Dispose();
            }
            _headerIconsByTextColor.Clear();

            IsLoaded = false;
        }
    }
}
