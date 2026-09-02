// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments for requesting information on item visibility.
    /// </summary>
    internal sealed class QueryItemVisibleEventArgs : EventArgs
    {
        private bool myIsVisible;
        private readonly int myRow;

        internal QueryItemVisibleEventArgs(int row)
        {
            myRow = row;
        }

        /// <summary>
        ///     The row to test
        /// </summary>
        /// <value></value>
        public int Row
        {
            get { return myRow; }
        }

        /// <summary>
        ///     Tests whether the item is visible
        /// </summary>
        /// <value>Event handler should set to true if visible. Cannot explicitly be set to false.</value>
        public bool IsVisible
        {
            get { return myIsVisible; }
            set
            {
                if (value)
                {
                    myIsVisible = value;
                }
            }
        }
    }

}
