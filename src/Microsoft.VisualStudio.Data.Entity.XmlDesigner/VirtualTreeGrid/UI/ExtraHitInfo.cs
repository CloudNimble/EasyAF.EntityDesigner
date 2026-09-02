// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Drawing;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    internal struct ExtraHitInfo
    {
        public bool IsTruncated;
        public Rectangle ClippedItemRectangle; // The label and glyphs, clipped for string truncation
        public Rectangle FullLabelRectangle; // The full label rectangle without glyphs or truncation
        public int LabelOffset; // The width of the glyph regions
        public Font LabelFont; // The font used to draw the item
        public StringFormat LabelFormat; // The format used to draw the string
    }

}
