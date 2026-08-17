// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     A structure representing a single column header in the tree. An array
    ///     of these structures is passed to VirtualTreeControl.SetColumnHeaders to
    ///     change the headers. A single header item can be fixed size, percentage
    ///     based, or percentage based with a minimum size. If all columns are percentage
    ///     based with no minimum, then you will not see a horizontal scrollbar (unless
    ///     the control is sized extremely narrow). If at least one column is percentage
    ///     based then you will never have blank space to the right of the column in the
    ///     tree. Column types can be mixed-and-matched.
    /// </summary>
    internal struct VirtualTreeColumnHeader
    {
        internal const int MinimumPixelWidth = 4; // Just enough for a reasonable splitter bar.
        private string myText;
        private float myPercentage;
        private int myWidth; // Positive for an adjustable width, negative for a fixed width.
        private int myImageIndex;
        private VirtualTreeColumnHeaderStyles myStyle;

        /// <summary>
        ///     Create a column header specifying just the text. This constructor
        ///     should be used only if the setPercentages parameter of SetColumnHeaders
        ///     is true.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        public VirtualTreeColumnHeader(string headerText)
            : this(headerText, VirtualTreeColumnHeaderStyles.Default, -1)
        {
        }

        /// <summary>
        ///     Create a column header specifying the text and style. This constructor
        ///     should be used only if the setPercentages parameter of SetColumnHeaders
        ///     is true.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="style">Style settings for this header</param>
        public VirtualTreeColumnHeader(string headerText, VirtualTreeColumnHeaderStyles style)
            : this(headerText, style, -1)
        {
        }

        /// <summary>
        ///     Create a column header specifying the text, style, and image. This constructor
        ///     should be used only if the setPercentages parameter of SetColumnHeaders
        ///     is true.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="style">Style settings for this header</param>
        /// <param name="imageIndex">
        ///     The index of the image for this header, or -1 for no image. Images
        ///     are provided by setting the VirtualTreeControl.HeaderImageList property.
        /// </param>
        public VirtualTreeColumnHeader(string headerText, VirtualTreeColumnHeaderStyles style, int imageIndex)
        {
            myText = headerText;
            myPercentage = 1f;
            myWidth = MinimumPixelWidth;
            myStyle = style;
            myImageIndex = (imageIndex < 0) ? -1 : imageIndex;
        }

        /// <summary>
        ///     Create a column header with a percentage-based width. See
        ///     comments on the Percentage property describing the Percentage value.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="percentage">The percentage at the right edge of the column</param>
        public VirtualTreeColumnHeader(string headerText, float percentage)
            : this(headerText, percentage, VirtualTreeColumnHeaderStyles.Default, -1)
        {
        }

        /// <summary>
        ///     Create a column header with a percentage-based width and non-default style. See
        ///     comments on the Percentage property describing the Percentage value.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="percentage">The percentage at the right edge of the column</param>
        /// <param name="style">Style settings for this header</param>
        public VirtualTreeColumnHeader(string headerText, float percentage, VirtualTreeColumnHeaderStyles style)
            : this(headerText, percentage, style, -1)
        {
        }

        /// <summary>
        ///     Create a column header with a percentage-based width, non-default style, and image. See
        ///     comments on the Percentage property describing the Percentage value.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="percentage">The percentage at the right edge of the column</param>
        /// <param name="style">Style settings for this header</param>
        /// <param name="imageIndex">
        ///     The index of the image for this header, or -1 for no image. Images
        ///     are provided by setting the VirtualTreeControl.HeaderImageList property.
        /// </param>
        public VirtualTreeColumnHeader(string headerText, float percentage, VirtualTreeColumnHeaderStyles style, int imageIndex)
        {
            myText = headerText;
            myPercentage = percentage;
            myWidth = MinimumPixelWidth;
            myStyle = style;
            myImageIndex = (imageIndex < 0) ? -1 : imageIndex;
        }

        /// <summary>
        ///     Create a column header with a percentage-based width and a minimum size. See
        ///     comments on the Percentage property describing the Percentage value.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="percentage">The percentage at the right edge of the column</param>
        /// <param name="width">The minimum width for this column</param>
        public VirtualTreeColumnHeader(string headerText, float percentage, int width)
            : this(headerText, percentage, width, VirtualTreeColumnHeaderStyles.Default, -1)
        {
        }

        /// <summary>
        ///     Create a column header with a percentage-based width, a minimum size, and non-default style. See
        ///     comments on the Percentage property describing the Percentage value.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="percentage">The percentage at the right edge of the column</param>
        /// <param name="width">The minimum width for this column</param>
        /// <param name="style">Style settings for this header</param>
        public VirtualTreeColumnHeader(string headerText, float percentage, int width, VirtualTreeColumnHeaderStyles style)
            : this(headerText, percentage, width, style, -1)
        {
        }

        /// <summary>
        ///     Create a column header with a percentage-based width, a minimum size, non-default style, and image. See
        ///     comments on the Percentage property describing the Percentage value.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="percentage">The percentage at the right edge of the column</param>
        /// <param name="width">The minimum width for this column</param>
        /// <param name="style">Style settings for this header</param>
        /// <param name="imageIndex">
        ///     The index of the image for this header, or -1 for no image. Images
        ///     are provided by setting the VirtualTreeControl.HeaderImageList property.
        /// </param>
        public VirtualTreeColumnHeader(string headerText, float percentage, int width, VirtualTreeColumnHeaderStyles style, int imageIndex)
        {
            myText = headerText;
            myPercentage = percentage;
            myWidth = Math.Max(width, MinimumPixelWidth);
            myStyle = style;
            myImageIndex = (imageIndex < 0) ? -1 : imageIndex;
        }

        /// <summary>
        ///     Create an adjustable column header with a fixed width that is not
        ///     calculated as a percentage of the current control width.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="width">The fixed pixel width for this column</param>
        public VirtualTreeColumnHeader(string headerText, int width)
            : this(headerText, width, false, VirtualTreeColumnHeaderStyles.Default, -1)
        {
        }

        /// <summary>
        ///     Create an adjustable column header with a fixed width that is not
        ///     calculated as a percentage of the current control width, and a non-default style.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="width">The fixed pixel width for this column</param>
        /// <param name="style">Style settings for this header</param>
        public VirtualTreeColumnHeader(string headerText, int width, VirtualTreeColumnHeaderStyles style)
            : this(headerText, width, false, style, -1)
        {
        }

        /// <summary>
        ///     Create an adjustable column header with a fixed width that is not
        ///     calculated as a percentage of the current control width, a non-default style, and an image.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="width">The fixed pixel width for this column</param>
        /// <param name="style">Style settings for this header</param>
        /// <param name="imageIndex">
        ///     The index of the image for this header, or -1 for no image. Images
        ///     are provided by setting the VirtualTreeControl.HeaderImageList property.
        /// </param>
        public VirtualTreeColumnHeader(string headerText, int width, VirtualTreeColumnHeaderStyles style, int imageIndex)
            : this(headerText, width, false, style, imageIndex)
        {
        }

        /// <summary>
        ///     Create an adjustable or static width column header with a width that is not
        ///     calculated as a percentage of the current control width.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="width">The fixed pixel width for this column</param>
        /// <param name="nonAdjustable">True if the user should not be allowed to change the width.</param>
        public VirtualTreeColumnHeader(string headerText, int width, bool nonAdjustable)
            : this(headerText, width, nonAdjustable, VirtualTreeColumnHeaderStyles.Default, -1)
        {
        }

        /// <summary>
        ///     Create an adjustable or static width column header with a width that is not
        ///     calculated as a percentage of the current control width, and a non-default style.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="width">The fixed pixel width for this column</param>
        /// <param name="nonAdjustable">True if the user should not be allowed to change the width.</param>
        /// <param name="style">Style settings for this header</param>
        public VirtualTreeColumnHeader(string headerText, int width, bool nonAdjustable, VirtualTreeColumnHeaderStyles style)
            : this(headerText, width, nonAdjustable, style, -1)
        {
        }

        /// <summary>
        ///     Create an adjustable or static width column header with a width that is not
        ///     calculated as a percentage of the current control width, a non-default style, and an image.
        /// </summary>
        /// <param name="headerText">The text to display in the column header</param>
        /// <param name="width">The fixed pixel width for this column</param>
        /// <param name="nonAdjustable">True if the user should not be allowed to change the width.</param>
        /// <param name="style">Style settings for this header</param>
        /// <param name="imageIndex">
        ///     The index of the image for this header, or -1 for no image. Images
        ///     are provided by setting the VirtualTreeControl.HeaderImageList property.
        /// </param>
        public VirtualTreeColumnHeader(
            string headerText, int width, bool nonAdjustable, VirtualTreeColumnHeaderStyles style, int imageIndex)
        {
            myText = headerText;
            myPercentage = 0f;
            var setWidth = Math.Max(width, MinimumPixelWidth);
            myWidth = nonAdjustable ? -setWidth : setWidth;
            myStyle = style;
            myImageIndex = (imageIndex < 0) ? -1 : imageIndex;
        }

        /// <summary>
        ///     Returns true if the column header is not initialized
        /// </summary>
        public bool IsEmpty
        {
            get { return myPercentage == 0f && myWidth == 0; }
        }

        /// <summary>
        ///     The text displayed in the column header
        /// </summary>
        public string Text
        {
            get { return myText; }
        }

        /// <summary>
        ///     The ending percentage for the column.
        ///     A percentage of 0 indicates a fixed-size column.
        ///     In an array of VirtualTreeColumnHeader structures,
        ///     the first column with a percentage should have a percentage
        ///     &gt; 0, the percentages must be increasing for percentage
        ///     based columns, and the final column with a non-zero percentage
        ///     must have percentage equal to 1.
        /// </summary>
        public float Percentage
        {
            get { return myPercentage; }
        }

        internal void SetPercentage(float percentage)
        {
            myPercentage = percentage;
        }

        /// <summary>
        ///     The fixed-size or minimum width for this column. This is the value
        ///     used when the column headers were initialized and is invariant over
        ///     the lifetime of the column headers if the nonAdjustable parameter of
        ///     the constructor used to create this header was true. The value
        ///     returned by Width does not correlate to the current width of a
        ///     given column except in the case of a non-proportional adjustable column.
        /// </summary>
        public int Width
        {
            get { return Math.Abs(myWidth); }
        }

        internal void SetWidth(int width)
        {
            // The adjustable setting is derived from the sign of the width and
            // cannot be changed. Preserve this setting, and don't allow it to be
            // changed with SetWidth.
            width = Math.Max(Math.Abs(width), MinimumPixelWidth);
            myWidth = (myWidth < 0) ? -width : width;
        }

        /// <summary>
        ///     Returns true if the width of this column can be adjusted
        /// </summary>
        public bool IsColumnAdjustable
        {
            get { return myWidth > 0; }
        }

        /// <summary>
        ///     The index of the image for this header, or -1 for no image.
        /// </summary>
        public int ImageIndex
        {
            get { return myImageIndex; }
        }

        /// <summary>
        ///     The style for this header
        /// </summary>
        public VirtualTreeColumnHeaderStyles Style
        {
            get { return myStyle; }
        }

        internal void SetAppearanceFields(string headerText, VirtualTreeColumnHeaderStyles style, int imageIndex)
        {
            myText = headerText;
            myStyle = style;
            myImageIndex = (imageIndex < 0) ? -1 : imageIndex;
        }

        #region Equals override and related functions

        /// <summary>
        ///     Do not compare VirtualTreeColumnHeader objects
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     Returns base.GetHashCode
        /// </summary>
        public override int GetHashCode()
        {
            // We're forced to override this with the Equals override.
            return base.GetHashCode();
        }

        /// <summary>
        ///     Do not compare VirtualTreeColumnHeader objects
        /// </summary>
        public static bool operator ==(VirtualTreeColumnHeader operand1, VirtualTreeColumnHeader operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     Do not compare VirtualTreeColumnHeader objects
        /// </summary>
        public static bool Compare(VirtualTreeColumnHeader operand1, VirtualTreeColumnHeader operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     Do not compare VirtualTreeColumnHeader objects
        /// </summary>
        public static bool operator !=(VirtualTreeColumnHeader operand1, VirtualTreeColumnHeader operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return true;
        }

        #endregion // Equals override and related functions
    }

}
