// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     The location a tooltip is displayed
    /// </summary>
    internal enum ToolTipType
    {
        /// <summary>
        ///     The tooltip for a hover in the text area. Ignore if the standard text should be used
        /// </summary>
        Default = 0x0000,

        /// <summary>
        ///     The tip text to show for an icon hover
        /// </summary>
        Icon = 0x0001,

        /// <summary>
        ///     The tip text to show for a state icon hover
        /// </summary>
        StateIcon = 0x0002
    }

}
