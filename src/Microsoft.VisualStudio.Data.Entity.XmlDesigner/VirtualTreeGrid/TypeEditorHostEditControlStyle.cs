// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid
{

    /// <summary>
    ///     Values passed to TypeEditorHost.Create to indicate the style of the edit
    ///     box to show in the text area.
    /// </summary>
    internal enum TypeEditorHostEditControlStyle
    {
        /// <summary>
        ///     Displays a live edit box for the given value.
        /// </summary>
        Editable = 0,

        /// <summary>
        ///     Leaves the region where a live edit box would normally be transparent
        ///     to let the backend control draw the text region.
        /// </summary>
        TransparentEditRegion = 1,

        /// <summary>
        ///     Places an instruction label with GrayText in the area the text region would
        ///     normally be.
        /// </summary>
        InstructionLabel = 2,

        /// <summary>
        ///     Displays a read-only edit box for the given value.
        /// </summary>
        ReadOnlyEdit = 3,
    }

}
