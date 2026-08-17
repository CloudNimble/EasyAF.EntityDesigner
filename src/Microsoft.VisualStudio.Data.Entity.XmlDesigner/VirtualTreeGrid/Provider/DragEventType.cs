// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     An enum specifying the type of drag event being handled in IBranch.OnDragEvent.
    /// </summary>
    internal enum DragEventType
    {
        /// <summary>
        ///     An item has been dropped on the specified branch item
        /// </summary>
        Drop,

        /// <summary>
        ///     An drag operation has entered the specified branch item
        /// </summary>
        Enter,

        /// <summary>
        ///     An drag operation has entered the specified branch item.
        ///     The args parameter is null for this type of event.
        /// </summary>
        Leave,

        /// <summary>
        ///     An drag operation is over the specified branch item
        /// </summary>
        Over,
    }

}
