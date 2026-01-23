// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using System;

    /// <summary>
    /// Orchestrates diagram export operations by selecting and invoking
    /// the appropriate exporter based on the requested format.
    /// </summary>
    internal class ExportManager
    {
        private readonly SvgExporter _svgExporter;
        private readonly MermaidExporter _mermaidExporter;
        private readonly RasterExporter _rasterExporter;

        /// <summary>
        /// Initializes a new instance of the ExportManager class.
        /// </summary>
        public ExportManager()
        {
            _svgExporter = new SvgExporter();
            _mermaidExporter = new MermaidExporter();
            _rasterExporter = new RasterExporter();
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
