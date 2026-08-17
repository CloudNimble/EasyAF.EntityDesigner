// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Values used with VirtualTreeHitInfo and VirtualTreeExtendedHitInfo
    ///     to indicate where on a given item the hit occurred.
    /// </summary>
    [Flags]
    internal enum VirtualTreeHitTargets
    {
        /// <summary>
        ///     Indicates an empty structure
        /// </summary>
        Uninitialized = 0,

        /// <summary>
        ///     The mouse is currently not on an item row or column
        /// </summary>
        NoWhere = 1,

        /// <summary>
        ///     The hit location is over the item's icon
        /// </summary>
        OnItemIcon = 2,

        /// <summary>
        ///     The hit location is on the items text region
        /// </summary>
        OnItemLabel = 4,

        /// <summary>
        ///     The hit location is over the indent region for the item
        /// </summary>
        OnItemIndent = 8,

        /// <summary>
        ///     The hit location is over the item. OnItem is a mask field that combines OnItemLabel, OnItemIcon, OnItemStateIcon
        /// </summary>
        OnItem = OnItemIcon | OnItemLabel | OnItemStateIcon,

        /// <summary>
        ///     The hit location is over the item's button
        /// </summary>
        OnItemButton = 0x10,

        /// <summary>
        ///     The hit location is to the right of the item label
        /// </summary>
        OnItemRight = 0x20,

        /// <summary>
        ///     The hit location is over the item's state icon (the state icon is generally used for checkboxes)
        /// </summary>
        OnItemStateIcon = 0x40,

        /// <summary>
        ///     The hit location is to the left of the item
        /// </summary>
        OnItemLeft = 0x80,

        /// <summary>
        ///     The hit location is over the item region. OnItemRegion is a mask field that combines OnItem, OnItemButton, OnItemRight, OnItemIndex, OnItemLeft.
        /// </summary>
        OnItemRegion = OnItem | OnItemButton | OnItemRight | OnItemIndent | OnItemLeft,

        /// <summary>
        ///     The hit location is above the client area
        /// </summary>
        Above = 0x100,

        /// <summary>
        ///     The hit location is below the client area
        /// </summary>
        Below = 0x200,

        /// <summary>
        ///     The hit location is to the right of the client area
        /// </summary>
        ToRight = 0x400,

        /// <summary>
        ///     The hit location is to the left of the client area
        /// </summary>
        ToLeft = 0x800,

        /// <summary>
        ///     The hit location is outside the client area. OutsideClientArea is mask field that combines Above, Below, ToRight, ToLeft.
        /// </summary>
        OutsideClientArea = Above | Below | ToRight | ToLeft,

        /// <summary>
        ///     The hit location is over a blank tree coordinate, but the VirtualTreeHitInfo has information
        ///     about the resolved blank expansion anchor. Use the RawRow and RawColumn properties to get the
        ///     unmodified hit information.
        /// </summary>
        OnBlankItem = 0x1000,

        /// <summary>
        ///     May be combined with OnItemStateIcon to indicate that the state icon is displayed
        ///     as hot-tracked.  Currently only supported for standard checkboxes.
        /// </summary>
        StateIconHotTracked = 0x2000
    }

}
