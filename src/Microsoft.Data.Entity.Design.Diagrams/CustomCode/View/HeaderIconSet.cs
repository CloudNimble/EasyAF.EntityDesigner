// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Drawing;

namespace Microsoft.Data.Entity.Design.Diagrams.View
{

    /// <summary>
    /// Contains the set of header icons for an entity type shape, colorized to match the header text.
    /// </summary>
    internal sealed class HeaderIconSet : IDisposable
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

        /// <summary>
        /// Releases the four bitmaps this set owns.
        /// </summary>
        public void Dispose()
        {
            EntityGlyph?.Dispose();
            BaseTypeIcon?.Dispose();
            ChevronExpanded?.Dispose();
            ChevronCollapsed?.Dispose();
        }
    }

}
