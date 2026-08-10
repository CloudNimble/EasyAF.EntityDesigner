// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Dsl.View.Export
{
    /// <summary>
    /// Orchestrates diagram export operations by selecting and invoking
    /// the appropriate exporter based on the requested format.
    /// </summary>
    internal class ExportManager
    {
        private readonly SvgExporter _svgExporter;
        private readonly MermaidExporter _mermaidExporter;
        private readonly IRasterExporter _rasterExporter;

        /// <summary>
        /// Initializes a new instance of the ExportManager class supporting only the vector formats.
        /// </summary>
        public ExportManager()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ExportManager class with a raster exporter.
        /// </summary>
        /// <param name="rasterExporter">
        /// Produces raster images. When null, only SVG and Mermaid are available and requesting a raster format
        /// throws <see cref="NotSupportedException"/>.
        /// </param>
        /// <remarks>
        /// There is no raster default because every implementation carries a dependency this assembly should not
        /// take on. Visual Studio supplies <c>ShellRasterExporter</c>, which calls <c>Diagram.CreateBitmap</c> so
        /// images match what the designer has always produced, and needs a running shell. The command line tool
        /// supplies one built on a rasteriser it references itself. See <see cref="IRasterExporter"/>.
        /// </remarks>
        public ExportManager(IRasterExporter rasterExporter)
        {
            _svgExporter = new SvgExporter();
            _mermaidExporter = new MermaidExporter();
            _rasterExporter = rasterExporter;
        }

        /// <summary>
        /// Exports the diagram using the specified options.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options specifying format, path, and settings.</param>
        public void Export(EntityDesignerDiagram diagram, DiagramExportOptions options)
        {
            if (diagram is null)
            {
                throw new ArgumentNullException("diagram");
            }

            if (options is null)
            {
                throw new ArgumentNullException("options");
            }

            switch (options.Format)
            {
                case ExportFormat.Svg:
                    _svgExporter.Export(diagram, options);
                    break;

                case ExportFormat.Mermaid:
                    _mermaidExporter.Export(diagram, options);
                    break;

                case ExportFormat.Png:
                case ExportFormat.Jpeg:
                case ExportFormat.Bmp:
                case ExportFormat.Gif:
                case ExportFormat.Tiff:
                    if (_rasterExporter is null)
                    {
                        throw new NotSupportedException(
                            $"Export format {options.Format} needs a raster exporter, and this host did not supply one.");
                    }

                    _rasterExporter.Export(diagram, options);
                    break;

                default:
                    throw new NotSupportedException(
                        string.Format("Export format {0} is not supported.", options.Format));
            }
        }

        /// <summary>
        /// Maps a file extension to the corresponding ExportFormat.
        /// </summary>
        /// <param name="extension">The file extension including the leading dot (e.g., ".svg").</param>
        /// <returns>The corresponding ExportFormat.</returns>
        public static ExportFormat GetFormatFromExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                throw new ArgumentNullException("extension");
            }

            switch (extension.ToLowerInvariant())
            {
                case ".svg":
                    return ExportFormat.Svg;
                case ".mmd":
                    return ExportFormat.Mermaid;
                case ".png":
                    return ExportFormat.Png;
                case ".jpg":
                case ".jpeg":
                    return ExportFormat.Jpeg;
                case ".bmp":
                    return ExportFormat.Bmp;
                case ".gif":
                    return ExportFormat.Gif;
                case ".tif":
                case ".tiff":
                    return ExportFormat.Tiff;
                default:
                    throw new NotSupportedException(
                        string.Format("File extension {0} is not supported.", extension));
            }
        }
    }
}
