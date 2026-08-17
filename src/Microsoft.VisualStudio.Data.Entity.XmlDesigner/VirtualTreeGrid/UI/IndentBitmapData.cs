// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     A structure to enable derived controls to set custom colors
    ///     and bitmap images for the indent region of the tree control.
    /// </summary>
    internal struct IndentBitmapData
    {
        private GraphicsPath myPlusPath;
        private GraphicsPath myMinusPath;
        private GraphicsPath myBoxPath;
        private bool myDisposePaths;

        /// <summary>
        ///     The color used to draw the background. Defaults to
        ///     SystemColors.Window.
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        ///     The color used to draw the lines. Defaults to
        ///     SystemColors.GrayText.
        /// </summary>
        public Color LineColor { get; set; }

        /// <summary>
        ///     The dash style used to draw the background. Defaults to
        ///     DashStyle.Dot.
        /// </summary>
        public DashStyle LineStyle { get; set; }

        /// <summary>
        ///     The color used to draw the lines. Defaults to
        ///     SystemColors.GrayText.
        /// </summary>
        public Color BoxColor { get; set; }

        /// <summary>
        ///     The color used to draw the lines. Defaults to
        ///     SystemColors.WindowText.
        /// </summary>
        public Color PlusMinusColor { get; set; }

        /// <summary>
        ///     The path used to draw the plus sign. This path is drawn with a brush
        ///     using Graphics.FillPath.
        /// </summary>
        public GraphicsPath PlusPath
        {
            get { return myPlusPath; }
        }

        /// <summary>
        ///     The path used to draw the minus sign. This path is drawn with a brush
        ///     using Graphics.FillPath.
        /// </summary>
        public GraphicsPath MinusPath
        {
            get { return myMinusPath; }
        }

        /// <summary>
        ///     The path used to draw the box around the plus and minus signs. This is draw
        ///     with a pen using DrawPath.
        /// </summary>
        public GraphicsPath BoxPath
        {
            get { return myBoxPath; }
        }

        /// <summary>
        ///     Points which represent the icon for tree expander in expanded state.
        /// </summary>
        public Point[] ExpandedIconPoints { get; set; }

        /// <summary>
        ///     Points which represent the icon for tree expander in un-expanded state.
        /// </summary>
        public Point[] UnexpandedIconPoints { get; set; }

        /// <summary>
        ///     Set the graphics paths used to draw the bitmap images. All paths should be centered
        ///     at (0, 0), not anchored there.
        /// </summary>
        /// <param name="plusPath">The path used to draw the plus sign</param>
        /// <param name="minusPath">The path used to draw the minus sign</param>
        /// <param name="boxPath">The path used to draw the box around the plus/minus sign</param>
        /// <param name="disposePaths">
        ///     Should the paths be disposed in the Dispose method? Set to true
        ///     if the path was created or cloned for this structure, false if the paths are cached.
        /// </param>
        public void SetPaths(GraphicsPath plusPath, GraphicsPath minusPath, GraphicsPath boxPath, bool disposePaths)
        {
            var oldDisposePaths = myDisposePaths;
            myDisposePaths = disposePaths;
            SetPath(ref myPlusPath, plusPath, oldDisposePaths);
            SetPath(ref myMinusPath, minusPath, oldDisposePaths);
            SetPath(ref myBoxPath, boxPath, oldDisposePaths);
        }

        private static void SetPath(ref GraphicsPath oldPath, GraphicsPath newPath, bool disposeOldPath)
        {
            if (disposeOldPath && (oldPath != null))
            {
                oldPath.Dispose();
            }
            oldPath = newPath;
        }

        /// <summary>
        ///     Dispose resources held by this structure.
        /// </summary>
        public void Dispose()
        {
            if (myDisposePaths)
            {
                myDisposePaths = false;
                myPlusPath?.Dispose();
                myMinusPath?.Dispose();
                myBoxPath?.Dispose();
            }
        }

        #region Equals override and related functions

        /// <summary>
        ///     Equals override. Defers to Compare function.
        /// </summary>
        /// <param name="obj">An item to compare to this object</param>
        /// <returns>True if the items are equal</returns>
        public override bool Equals(object obj)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     GetHashCode override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // We're forced to override this with the Equals override.
            return base.GetHashCode();
        }

        /// <summary>
        ///     Equals operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>Always returns false, there is no need to compare IndentBitmapData structures</returns>
        public static bool operator ==(IndentBitmapData operand1, IndentBitmapData operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     Compare two IndentBitmapData structures
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>Always returns false, there is no need to compare IndentBitmapData structures</returns>
        public static bool Compare(IndentBitmapData operand1, IndentBitmapData operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return false;
        }

        /// <summary>
        ///     Not equal operator. Defers to Compare.
        /// </summary>
        /// <param name="operand1">Left operand</param>
        /// <param name="operand2">Right operand</param>
        /// <returns>Always returns true, there is no need to compare IndentBitmapData structures</returns>
        public static bool operator !=(IndentBitmapData operand1, IndentBitmapData operand2)
        {
            Debug.Assert(false); // There is no need to compare these
            return true;
        }

        #endregion // Equals override and related functions
    }

}
