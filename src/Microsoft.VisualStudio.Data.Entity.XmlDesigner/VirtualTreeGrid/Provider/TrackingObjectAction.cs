// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Enumeration for values return by IBranch.LocateObject called with the
    ///     ObjectStyle.TrackingObject style.
    /// </summary>
    internal enum TrackingObjectAction
    {
        /// <summary>
        ///     The object could not be tracked.
        /// </summary>
        NotTracked = 0,

        /// <summary>
        ///     The object occurs at this level in the tree.
        /// </summary>
        ThisLevel = 1,

        /// <summary>
        ///     The object occurs at deeper level in the tree
        /// </summary>
        NextLevel = 2,

        /// <summary>
        ///     The object could not be tracked at this level. Return the coordinate for
        ///     the most recent IBranch.LocateObject that returned NextLevel.
        /// </summary>
        NotTrackedReturnParent = 3,
    }

}
