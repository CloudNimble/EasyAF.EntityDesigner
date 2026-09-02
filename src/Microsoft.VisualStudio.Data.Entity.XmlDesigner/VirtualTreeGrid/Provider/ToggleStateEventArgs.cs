// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments for the result of a state icon being clicked.
    /// </summary>
    internal sealed class ToggleStateEventArgs : EventArgs
    {
        private readonly int myRow;
        private readonly int myColumn;
        private readonly StateRefreshChanges myStateRefreshOptions;

        internal ToggleStateEventArgs(int row, int column, StateRefreshChanges stateRefreshOptions)
        {
            myRow = row;
            myColumn = column;
            myStateRefreshOptions = stateRefreshOptions;
        }

        /// <summary>
        ///     Row coordinate
        /// </summary>
        public int Row
        {
            get { return myRow; }
        }

        /// <summary>
        ///     Column coordinate
        /// </summary>
        public int Column
        {
            get { return myColumn; }
        }

        /// <summary>
        ///     The set of items (relative to the modified item) that need to be refreshed
        /// </summary>
        public StateRefreshChanges StateRefreshOptions
        {
            get { return myStateRefreshOptions; }
        }
    }

}
