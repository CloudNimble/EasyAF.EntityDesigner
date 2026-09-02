// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Describes which fields in the VirtualTreeDisplayData should be set.
    ///     Note that values &lt;=0x40 correspond to TVIF_* flags
    /// </summary>
    [Flags]
    internal enum VirtualTreeDisplayMasks
    {
        /// <summary>
        ///     Set the Image fields
        /// </summary>
        Image = 0x0002,

        /// <summary>
        ///     Set the Image overlay fields
        /// </summary>
        ImageOverlays = 0x0004,

        /// <summary>
        ///     Set the StateImage fields
        /// </summary>
        StateImage = 0x0008,

        /// <summary>
        ///     Set the state fields
        /// </summary>
        State = 0x0010,

        /// <summary>
        ///     Set the SelectedImage field
        /// </summary>
        SelectedImage = 0x0020,

        /// <summary>
        ///     Set the ForceSelect information if applicable
        /// </summary>
        ForceSelect = 0x0080,

        /// <summary>
        ///     Set the BackColor and ForeColor fields are applicable
        /// </summary>
        Color = 0x0200,
    }

}
