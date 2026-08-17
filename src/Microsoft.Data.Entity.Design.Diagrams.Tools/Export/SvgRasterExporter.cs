// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Dsl.View.Export;
using SkiaSharp;
using Svg.Skia;

namespace Microsoft.Data.Entity.Tools.Export
{

    /// <summary>
    /// Produces raster images by rasterising the SVG the renderer generates.
    /// </summary>
    /// <remarks>
    /// The shell implementation calls <c>Diagram.CreateBitmap</c>, which resolves <c>SVsUIShell</c> and cannot run
    /// outside Visual Studio. Since the DSL SDK is closed source its GDI+ painting cannot be reused, so this host
    /// renders the diagram to SVG - the same output as <c>--format svg</c> - and rasterises that instead. Images will
    /// therefore not be pixel identical to the ones Visual Studio produces.
    /// </remarks>
    internal sealed class SvgRasterExporter : IRasterExporter
    {

        #region Public Methods

        /// <summary>
        /// Renders the diagram to SVG and writes it to <paramref name="options"/> as a raster image.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options, including the destination path and image format.</param>
        /// <exception cref="InvalidOperationException">The generated SVG could not be rasterised.</exception>
        public void Export(EntityDesignerSurface diagram, DiagramExportOptions options)
        {
            if (diagram is null)
            {
                throw new ArgumentNullException(nameof(diagram));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var svg = new SvgExporter().GenerateSvg(diagram, options.TransparentBackground, options.ShowTypes);

            using (var document = new SKSvg())
            {
                // Load rather than FromSvg: the generated markup carries a BOM and an XML declaration.
                using (var stream = new MemoryStream(new UTF8Encoding(false).GetBytes(svg)))
                {
                    if (document.Load(stream) is null)
                    {
                        throw new InvalidOperationException("The generated SVG could not be parsed for rasterisation.");
                    }
                }

                using (var output = File.Create(options.FilePath))
                {
                    if (!document.Save(output, SKColors.Empty, GetFormat(options.Format), quality: 100, scaleX: 1f, scaleY: 1f))
                    {
                        throw new InvalidOperationException($"The diagram could not be written as {options.Format}.");
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Maps an export format to the matching Skia encoding.
        /// </summary>
        /// <param name="format">The requested export format.</param>
        /// <returns>The Skia encoded image format to write.</returns>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not a raster format Skia can write.</exception>
        private static SKEncodedImageFormat GetFormat(ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.Png:
                    return SKEncodedImageFormat.Png;

                case ExportFormat.Jpeg:
                    return SKEncodedImageFormat.Jpeg;

                case ExportFormat.Bmp:
                    return SKEncodedImageFormat.Bmp;

                case ExportFormat.Gif:
                    return SKEncodedImageFormat.Gif;

                default:
                    // Skia cannot encode TIFF.
                    throw new NotSupportedException($"{format} images cannot be produced outside Visual Studio.");
            }
        }

        #endregion

    }

}
