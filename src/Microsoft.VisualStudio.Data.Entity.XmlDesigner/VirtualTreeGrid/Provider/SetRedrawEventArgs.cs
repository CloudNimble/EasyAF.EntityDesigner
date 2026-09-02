// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Event arguments for turning off redraw on a view. No requests should
    ///     be made to the ITree implementation when redraw is off.
    /// </summary>
    internal sealed class SetRedrawEventArgs : EventArgs
    {
        private readonly bool myRedrawOn;

        internal SetRedrawEventArgs(bool redrawOn)
        {
            myRedrawOn = redrawOn;
        }

        /// <summary>
        ///     Test whether redraw is on or off
        /// </summary>
        /// <value>true if redraw is turned on, false otherwise</value>
        public bool RedrawOn
        {
            get { return myRedrawOn; }
        }
    }

}
