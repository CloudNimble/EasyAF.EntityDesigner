// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Event arguments generated for a context menu event. Users can get the item
    ///     clicked on by using ScreenToClient to convert to client coordinates, followed
    ///     by the VirtualTreeControl.HitInfo method to get the item that was clicked on,
    ///     followed by VirtualTreeControl.Tree.GetItemInfo method with the Row and NativeColumn
    ///     properties of the HitInfo structure.
    /// </summary>
    internal class ContextMenuEventArgs : EventArgs
    {
        private readonly int myX;
        private readonly int myY;

        /// <summary>
        ///     Create new context menu arguments at the given screen coordinates
        /// </summary>
        /// <param name="x">The x (horizontal) position in screen coordinates</param>
        /// <param name="y">The y (vertical) position in screen coordinates</param>
        public ContextMenuEventArgs(int x, int y)
        {
            myX = x;
            myY = y;
        }

        /// <summary>
        ///     The x (horizontal) position in screen coordinates
        /// </summary>
        public int X
        {
            get { return myX; }
        }

        /// <summary>
        ///     The y (vertical) position in screen coordinates
        /// </summary>
        public int Y
        {
            get { return myY; }
        }
    }

}
