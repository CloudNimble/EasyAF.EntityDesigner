// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Drawing;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI
{

    /// <summary>
    ///     The coordination interface that needs to be implemented
    ///     by a control that wants to be in-place activated in the
    ///     tree control. Controls should implement this interface
    ///     by defering to the implementation of the members on the
    ///     IInPlaceControlDefer interface to the VirtualTreeControl.InPlaceControlHelper,
    ///     and implementing the remaining methods themselves class.
    /// </summary>
    internal interface IVirtualTreeInPlaceControl : IVirtualTreeInPlaceControlDefer
    {
        /// <summary>
        ///     Contains control-specific code to select all text in the window.
        /// </summary>
        void SelectAllText();

        /// <summary>
        ///     Contains control-specific code to set the SelectionStart property
        ///     on the control.
        /// </summary>
        int SelectionStart { get; set; }

        /// <summary>
        ///     Contains control-specific code to set the MaxTextLength property.
        /// </summary>
        int MaxTextLength { get; set; }

        /// <summary>
        ///     Get the formatting rectangle of the text. See EM_GETRECT for the definition
        ///     of a formatting rectangle;
        /// </summary>
        Rectangle FormattingRectangle { get; }

        /// <summary>
        ///     Get the amount of extra space that the control should show to the right of the
        ///     text for editing. This can be used to hold a cursor in a live edit box, or a dropdown
        ///     button in a combo box.
        /// </summary>
        int ExtraEditWidth { get; }
    }

}
