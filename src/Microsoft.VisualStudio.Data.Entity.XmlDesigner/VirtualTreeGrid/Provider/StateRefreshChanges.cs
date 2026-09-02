// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Values returned by IBranch.ToggleState to indicate the scope of
    ///     relative items that need to be correctly display the state change.
    /// </summary>
    [Flags]
    internal enum StateRefreshChanges
    {
        /// <summary>
        ///     No refresh required
        /// </summary>
        None = 0x0000,

        /// <summary>
        ///     Refresh toggled item
        /// </summary>
        Current = 0x0001,

        /// <summary>
        ///     Refresh children of toggled item
        /// </summary>
        Children = 0x0002,

        /// <summary>
        ///     Refresh parents of toggled item
        /// </summary>
        Parents = 0x0004,

        /// <summary>
        ///     Refresh children of all parents
        /// </summary>
        ParentsChildren = 0x0008,

        /// <summary>
        ///     Refresh entire tree
        /// </summary>
        Entire = 0x0010,
    };

}
