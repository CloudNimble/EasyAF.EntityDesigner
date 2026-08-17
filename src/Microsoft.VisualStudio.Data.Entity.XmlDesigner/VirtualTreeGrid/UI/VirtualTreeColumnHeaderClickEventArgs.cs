// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Drawing;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Event arguments describing a column header click. Includes all of the
    ///     click types from the VirtualTreeColumnHeaderClickStyle enum.
    /// </summary>
    internal class VirtualTreeColumnHeaderClickEventArgs : EventArgs
    {
        private readonly int myColumn;
        private readonly VirtualTreeColumnHeader myHeader;
        private readonly VirtualTreeColumnHeaderClickStyle myClickStyle;
        private readonly VirtualTreeHeaderControl myHeaderControl;
        private readonly Point myMousePosition;

        /// <summary>
        ///     Construct a new VirtualTreeColumnHeaderClickEventArgs object
        /// </summary>
        /// <param name="control">The header control that was clicked</param>
        /// <param name="clickStyle">The style of click</param>
        /// <param name="header">A copy of the header structure</param>
        /// <param name="column">The native index of the column that was clicked</param>
        /// <param name="mousePosition">The event position</param>
        public VirtualTreeColumnHeaderClickEventArgs(
            VirtualTreeHeaderControl control, VirtualTreeColumnHeaderClickStyle clickStyle, VirtualTreeColumnHeader header, int column,
            Point mousePosition)
        {
            myHeaderControl = control;
            myColumn = column;
            myHeader = header;
            myClickStyle = clickStyle;
            Handled = false;
            myMousePosition = mousePosition;
        }

        /// <summary>
        ///     The index of the column header clicked. The returned value is relative
        ///     to the natural order specified in SetColumnHeaders.
        /// </summary>
        public int Column
        {
            get { return myColumn; }
        }

        /// <summary>
        ///     A copy of the header being clicked on. Modifying this structure
        ///     will not change the current copy
        /// </summary>
        public VirtualTreeColumnHeader ColumnHeader
        {
            get { return myHeader; }
        }

        /// <summary>
        ///     The position of the mouse when clicked, in screen coordinates.
        /// </summary>
        public Point MousePosition
        {
            get { return myMousePosition; }
        }

        /// <summary>
        ///     Get the style of the click.
        /// </summary>
        public VirtualTreeColumnHeaderClickStyle ClickStyle
        {
            get { return myClickStyle; }
        }

        /// <summary>
        ///     Mark the event as having been handled. Used to skip
        ///     default processing for the event and to signal other listeners
        ///     to not respond.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     The header control for this tree
        /// </summary>
        public VirtualTreeHeaderControl HeaderControl
        {
            get { return myHeaderControl; }
        }
    }

}
