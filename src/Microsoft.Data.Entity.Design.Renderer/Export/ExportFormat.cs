// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    /// <summary>
    /// Supported diagram export formats.
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// Scalable Vector Graphics format.
        /// </summary>
        Svg,

        /// <summary>
        /// Portable Network Graphics format.
        /// </summary>
        Png,

        /// <summary>
        /// JPEG image format.
        /// </summary>
        Jpeg,

        /// <summary>
        /// Bitmap image format.
        /// </summary>
        Bmp,

        /// <summary>
        /// Graphics Interchange Format.
        /// </summary>
        Gif,

        /// <summary>
        /// Tagged Image File Format.
        /// </summary>
        Tiff,

        /// <summary>
        /// Mermaid diagram format (text-based).
        /// </summary>
        Mermaid
    }
}
