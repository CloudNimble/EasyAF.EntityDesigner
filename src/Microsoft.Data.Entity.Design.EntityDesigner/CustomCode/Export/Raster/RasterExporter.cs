// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using Microsoft.VisualStudio.Modeling.Diagrams;

    /// <summary>
    /// Exports diagrams to raster image formats (PNG, JPEG, BMP, GIF, TIFF).
    /// </summary>
    internal class RasterExporter
    {
        /// <summary>
        /// Exports the diagram to a raster image file.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options specifying format, path, and settings.</param>
        public void Export(EntityDesignerDiagram diagram, DiagramExportOptions options)
        {
            if (diagram == null)
            {
                throw new ArgumentNullException("diagram");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var imageFormat = GetImageFormat(options.Format);
            var childShapes = diagram.NestedChildShapes;

            Bitmap bitmap = null;
            try
            {
                // Temporarily disable color theme for consistent export
                AssociationConnector.IsColorThemeSet = false;

                bitmap = diagram.CreateBitmap(
                    childShapes,
                    Diagram.CreateBitmapPreference.FavorSmallSizeOverClarity);
            }
            finally
            {
                AssociationConnector.ForceDrawOnWhiteBackground = false;
                AssociationConnector.IsColorThemeSet = false;
            }

            if (bitmap == null)
            {
                throw new InvalidOperationException("Failed to create bitmap from diagram.");
            }

            try
            {
                using (var fileStream = new FileStream(options.FilePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    bitmap.Save(fileStream, imageFormat);
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        /// <summary>
        /// Maps an ExportFormat to the corresponding ImageFormat.
        /// </summary>
        /// <param name="format">The export format.</param>
        /// <returns>The corresponding ImageFormat.</returns>
        private static ImageFormat GetImageFormat(ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.Png:
                    return ImageFormat.Png;
                case ExportFormat.Jpeg:
                    return ImageFormat.Jpeg;
                case ExportFormat.Bmp:
                    return ImageFormat.Bmp;
                case ExportFormat.Gif:
                    return ImageFormat.Gif;
                case ExportFormat.Tiff:
                    return ImageFormat.Tiff;
                default:
                    throw new NotSupportedException(
                        string.Format("Format {0} is not a raster format.", format));
            }
        }
    }
}
