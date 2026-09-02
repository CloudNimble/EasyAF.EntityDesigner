// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     An enum specifying why the IBranch.OnStartDrag method
    ///     is called. Both Drag/Drop and Copy/Paste operations are coordinated through the various
    ///     drag methods. This flag allows the branch implementer to proffer different
    ///     data objects for the different operations.
    /// </summary>
    internal enum DragReason
    {
        /// <summary>
        ///     The data will be used for a Drag/Drop operation
        /// </summary>
        DragDrop,

        /// <summary>
        ///     The data is being retrieved in response to a Copy command
        /// </summary>
        Copy,

        /// <summary>
        ///     The data is being retrieved in response to a Cut command
        /// </summary>
        Cut,

        /// <summary>
        ///     The status of the Copy command is being retrieved.
        /// </summary>
        CanCopy,

        /// <summary>
        ///     The status of the Cut command is being retrieved.
        /// </summary>
        CanCut
    }

}
