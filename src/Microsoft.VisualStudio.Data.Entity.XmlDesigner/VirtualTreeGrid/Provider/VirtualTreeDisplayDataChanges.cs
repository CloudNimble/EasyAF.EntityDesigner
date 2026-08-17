// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Settings for the DisplayDataChanged notification. These
    ///     values are sent with the event to indicate which part of the
    ///     UI needs to be updated.
    /// </summary>
    [Flags]
    internal enum VirtualTreeDisplayDataChanges
    {
        /// <summary>
        ///     The text needs to be redrawn
        /// </summary>
        Text = 1,

        /// <summary>
        ///     The image needs to be redrawn
        /// </summary>
        Image = 2,

        /// <summary>
        ///     The state image needs to be redrawn
        /// </summary>
        StateImage = 4,

        /// <summary>
        ///     The button for the item needs to be redrawn
        /// </summary>
        ItemButton = 8,

        /// <summary>
        ///     All visible elements need to be redrawn
        /// </summary>
        VisibleElements = Text | Image | StateImage | ItemButton,

        /// <summary>
        ///     If an item is selected in this set, then the
        ///     selection needs to be refreshed. Include this
        ///     flag if visual elements outside the control may
        ///     need to be kept in sync with this item.
        /// </summary>
        DependentUIElements = 0x10,

        /// <summary>
        ///     Value of a cell has changed.  Setting this flag will
        ///     cause the control to fire a value or name change WinEvents,
        ///     as appropriate.
        /// </summary>
        AccessibleValue = 0x20,

        /// <summary>
        ///     Update all elements for this item. A combination
        ///     of VisibleElements and DependentUIElements
        /// </summary>
        All = VisibleElements | DependentUIElements | AccessibleValue,
    }

}
