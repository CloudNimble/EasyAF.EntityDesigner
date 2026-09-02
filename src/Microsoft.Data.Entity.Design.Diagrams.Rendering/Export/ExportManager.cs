// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.Rendering.Export.Mermaid;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Export.Raster;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Export.Svg;
using Microsoft.Data.Entity.Design.Diagrams.View;
using System;

namespace Microsoft.Data.Entity.Design.Diagrams.Rendering.Export
{

    /// <summary>
    /// Orchestrates diagram export operations by selecting and invoking
    /// the appropriate exporter based on the requested format.
    /// </summary>
    /// <remarks>
    /// This is the single entry point every host uses to export a diagram, so the format-to-exporter mapping lives
    /// here rather than being duplicated in the Visual Studio package and the command line tool.
    /// </remarks>
    internal class ExportManager
    {

        #region Fields

        /// <summary>
        /// Produces Mermaid class diagram text. Always available because it has no host dependencies.
        /// </summary>
        private readonly MermaidExporter _mermaidExporter;

        /// <summary>
        /// Produces raster images, or <see langword="null"/> when the host did not supply one.
        /// </summary>
        /// <remarks>
        /// Kept nullable on purpose: raster export is the only format that needs something this assembly cannot
        /// provide by itself, so hosts that cannot rasterise simply omit it instead of failing at construction.
        /// </remarks>
        private readonly IRasterExporter _rasterExporter;

        /// <summary>
        /// Produces SVG markup. Always available because it has no host dependencies.
        /// </summary>
        private readonly SvgExporter _svgExporter;

        #endregion

        #region Constructors

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

        #endregion

        #region Public Methods

        /// <summary>
        /// Exports the diagram using the specified options.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options specifying format, path, and settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="diagram"/> or <paramref name="options"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the requested format is a raster format and no <see cref="IRasterExporter"/> was supplied,
        /// or when the format is not recognised at all.
        /// </exception>
        /// <remarks>
        /// The two failure modes are deliberately distinct: a missing raster exporter is a host configuration
        /// problem the caller can fix, while an unrecognised format is a programming error.
        /// </remarks>
        public void Export(EntityDesignerSurface diagram, DiagramExportOptions options)
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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="extension"/> is null or empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when the extension does not map to a known format.</exception>
        /// <remarks>
        /// Comparison is done on the invariant lower-cased extension so that a user typing ".SVG" on a
        /// culture-specific machine still resolves to the same format.
        /// </remarks>
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

        #endregion

    }

}
