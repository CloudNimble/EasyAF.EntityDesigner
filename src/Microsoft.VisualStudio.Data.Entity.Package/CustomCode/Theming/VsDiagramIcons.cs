// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using System.Drawing;
using EntityDesignerRes = Microsoft.Data.Entity.Design.Diagrams.Properties.DiagramsResources;

namespace Microsoft.VisualStudio.Data.Entity.Package.Theming
{

    /// <summary>
    ///     Tints the designer's property and header icons for the current theme and hands them to the designer.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The designer holds the icons and the shapes draw them, but producing them means GDI recolouring
    ///         against the compartment fill and the header text colour, which is host work. This is where that
    ///         happens; the designer receives finished images.
    ///     </para>
    ///     <para>
    ///         Header icons are keyed by text colour, which is only ever black or white — the shapes pick one
    ///         based on how bright their fill is — so both are supplied up front rather than on demand.
    ///     </para>
    ///     <para>
    ///         Nothing subscribes to this under the command line renderer, which supplies no icons and emits its
    ///         own SVG symbols instead. See specs/layer-map.md.
    ///     </para>
    /// </remarks>
    internal static class VsDiagramIcons
    {

        #region Internal Methods

        /// <summary>
        ///     Rebuilds every icon against <paramref name="compartmentFillColor" /> and pushes them into the
        ///     designer, replacing whatever was there.
        /// </summary>
        /// <param name="compartmentFillColor">The colour the property compartments are painted with.</param>
        internal static void Apply(Color compartmentFillColor)
        {
            DiagramImageHelper.Instance.SetPropertyIcons(
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.Property, compartmentFillColor, true),
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.Property, compartmentFillColor, false),
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.PropertyPK, compartmentFillColor, true),
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.PropertyPK, compartmentFillColor, false),
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.ComplexProperty, compartmentFillColor, true),
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.ComplexProperty, compartmentFillColor, false),
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.NavigationProperty, compartmentFillColor, true),
                ThemeUtils.GetThemedPropertyIcon(EntityDesignerRes.NavigationProperty, compartmentFillColor, false));

            // SetPropertyIcons clears the header cache, so both text colours have to be re-registered after it.
            DiagramImageHelper.Instance.SetHeaderIcons(Color.Black, CreateHeaderIcons(Color.Black));
            DiagramImageHelper.Instance.SetHeaderIcons(Color.White, CreateHeaderIcons(Color.White));
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Tints the four header icons for one text colour.
        /// </summary>
        /// <param name="textColor">The colour the entity header text is drawn in.</param>
        /// <returns>The tinted icons.</returns>
        private static HeaderIconSet CreateHeaderIcons(Color textColor)
        {
            return new HeaderIconSet(
                ThemeUtils.GetColorizedHeaderIcon(EntityDesignerRes.EntityGlyph, textColor),
                ThemeUtils.GetColorizedHeaderIcon(EntityDesignerRes.BaseTypeIcon, textColor),
                ThemeUtils.GetColorizedHeaderIcon(EntityDesignerRes.ChevronExpanded, textColor),
                ThemeUtils.GetColorizedHeaderIcon(EntityDesignerRes.ChevronCollapsed, textColor));
        }

        #endregion

    }

}
