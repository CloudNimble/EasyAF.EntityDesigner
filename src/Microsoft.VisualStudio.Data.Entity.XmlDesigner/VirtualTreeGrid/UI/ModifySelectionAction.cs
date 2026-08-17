// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     Used with the VirtualTreeControl.SetCurrentExtendedMultiSelectIndex to specify the
    ///     modification to make to the selection state of the new caret index.
    /// </summary>
    internal enum ModifySelectionAction
    {
        /// <summary>
        ///     Do not take any special action.
        /// </summary>
        None,

        /// <summary>
        ///     Toggle the selection state of the item
        /// </summary>
        Toggle,

        /// <summary>
        ///     The item should be selected
        /// </summary>
        Select,

        /// <summary>
        ///     The item should not be selected
        /// </summary>
        Clear,
    }

}
