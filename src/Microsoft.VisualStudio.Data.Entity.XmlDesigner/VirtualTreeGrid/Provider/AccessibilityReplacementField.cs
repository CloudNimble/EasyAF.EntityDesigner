// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     An enum describing different pieces of information that the
    ///     tree control can automatically add to your name and description fields
    ///     for accessibility readers. Some of this information is available from
    ///     the branch itself, while other pieces (such as the current position in
    ///     the tree) are not available to the provider, but the provide can specify
    ///     how they want this information to be merged into their accessibility strings
    ///     using the VirtualTreeAccessibilityData structure.
    /// </summary>
    internal enum AccessibilityReplacementField
    {
        /// <summary>
        ///     A replacement field is not specified
        /// </summary>
        None,

        /// <summary>
        ///     Insert the replacement text from the header for the current column
        /// </summary>
        ColumnHeader,

        /// <summary>
        ///     Insert the base 0 number of the global row for this item
        /// </summary>
        GlobalRow0,

        /// <summary>
        ///     Insert the base 1 number of the global row for this item
        /// </summary>
        GlobalRow1,

        /// <summary>
        ///     Insert the base 0 'row n' text of the global row for this item
        /// </summary>
        GlobalRowText0,

        /// <summary>
        ///     Insert the base 1 'row n' text of the global row for this item
        /// </summary>
        GlobalRowText1,

        /// <summary>
        ///     Insert the base 0 number of the global column for this item
        /// </summary>
        GlobalColumn0,

        /// <summary>
        ///     Insert the base 1 number of the global column for this item
        /// </summary>
        GlobalColumn1,

        /// <summary>
        ///     Insert the base 0 'column n' text of the global column for this item
        /// </summary>
        GlobalColumnText0,

        /// <summary>
        ///     Insert the base 1 'column n' text of the global column for this item
        /// </summary>
        GlobalColumnText1,

        /// <summary>
        ///     Insert the base 0 'row n column m' text for this item
        /// </summary>
        GlobalRowAndColumnText0,

        /// <summary>
        ///     Insert the base 1 'row n column m' text for this item
        /// </summary>
        GlobalRowAndColumnText1,

        /// <summary>
        ///     Insert the base 0 number of the local row for this item
        /// </summary>
        LocalRow0,

        /// <summary>
        ///     Insert the base 0 number of the local row for this item
        /// </summary>
        LocalRow1,

        /// <summary>
        ///     Insert the base 0 'row n' text of the local row for this item
        /// </summary>
        LocalRowText0,

        /// <summary>
        ///     Insert the base 1 'row n' text of the local row for this item
        /// </summary>
        LocalRowText1,

        /// <summary>
        ///     Insert the base 1 'row n of total' text of the local row for this item
        /// </summary>
        LocalRowOfTotal,

        /// <summary>
        ///     Insert the number of direct descendants below this item.
        /// </summary>
        ChildRowCount,

        /// <summary>
        ///     Insert the 'n child rows' text for the number of direct descendants below this item.
        /// </summary>
        ChildRowCountText,

        /// <summary>
        ///     Insert the number of cells attached to this item, including the first column.
        /// </summary>
        ColumnCount,

        /// <summary>
        ///     Insert the 'n columns' text of cells attached to this item, including the first column.
        /// </summary>
        ColumnCountText,

        /// <summary>
        ///     Insert the text retrieved from IBranch.GetText(row, column)
        /// </summary>
        DisplayText,

        /// <summary>
        ///     Insert the text retrieved from IBranch.GetTipText(row, column, ToolTipType.Icon)
        /// </summary>
        ImageTipText,

        /// <summary>
        ///     Insert the text associated with the ImageDescriptions array in the  VirtualTreeAccessibilityData.
        ///     The index to use is retrieved from VirtualTreeDisplayData.Image, and the text must correspond
        ///     to an image list returned by that structure. If a custom image list is not returned, the image
        ///     data is retrieved from the control instead of the ImageDescriptions property.
        /// </summary>
        PrimaryImageText,

        /// <summary>
        ///     Similar to PrimaryImageText, except overlay indices are also recognized.
        ///     The index to use is retrieved from VirtualTreeDisplayData.Image and combined in a delimited
        ///     list with the text for any specified overlay indices.
        /// </summary>
        PrimaryImageAndOverlaysText,

        /// <summary>
        ///     Insert the text associated with the StateImageDescriptions array in the  VirtualTreeAccessibilityData.
        ///     The index to use is retrieved from VirtualTreeDisplayData.Image, and the text must correspond
        ///     to an image list returned by that structure. If a custom image list is not returned, the image
        ///     data is retrieved from the control instead of the ImageDescriptions property.
        /// </summary>
        StateImageText,
    }

}
