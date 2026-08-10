// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Data.Entity.Design.EntityDesigner.View.Export;
using Microsoft.Data.Entity.Design.Renderer.Headless;

namespace Microsoft.Data.Entity.Tools.Commands.Root
{

    /// <summary>
    /// Renders the diagrams in an EDMX file to an image or Mermaid document.
    /// </summary>
    /// <remarks>
    /// Runs without Visual Studio. Shape positions come from the EDMX Diagrams section and connector routes are
    /// produced by the DSL layout engine, so the output matches what the designer draws.
    /// </remarks>
    /// <example>
    /// <code>
    /// edmx render Model.edmx
    /// edmx render Model.edmx --output diagram.svg
    /// edmx render Model.edmx --format mermaid --show-types
    /// </code>
    /// </example>
    [Command(Name = "render", Description = "Render the diagrams in an EDMX file to SVG, PNG or Mermaid.")]
    internal class RenderCommand
    {

        #region Fields

        private readonly ExportManager _exportManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the command with the export manager configured for this host.
        /// </summary>
        /// <param name="exportManager">Writes the diagram in the requested format.</param>
        public RenderCommand(ExportManager exportManager)
        {
            _exportManager = exportManager ?? throw new ArgumentNullException(nameof(exportManager));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the diagram to render. Defaults to the first diagram in the file.
        /// </summary>
        [Option("-d|--diagram", Description = "Name of the diagram to render (defaults to the first one in the file).")]
        public string Diagram { get; set; }

        /// <summary>
        /// Gets or sets the format to render. Inferred from the output file extension when not supplied.
        /// </summary>
        [Option("-f|--format", Description = "Output format: svg, png, jpg, bmp, gif, tiff or mermaid. Inferred from --output when omitted.")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the path of the EDMX file to render.
        /// </summary>
        [Argument(0, Description = "Path to the .edmx file to render.")]
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the file to write. Defaults to the input file name with the format's extension.
        /// </summary>
        [Option("-o|--output", Description = "File to write (defaults to the input name with the format's extension).")]
        public string Output { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether property data types are shown alongside property names.
        /// </summary>
        [Option("--show-types", Description = "Show property data types alongside property names.")]
        public bool ShowTypes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the background is left transparent. Ignored by Mermaid.
        /// </summary>
        [Option("--transparent", Description = "Render with a transparent background (SVG and raster only).")]
        public bool Transparent { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates the arguments and renders the requested diagram.
        /// </summary>
        /// <param name="app">The command line application.</param>
        /// <param name="console">The console to write messages to.</param>
        /// <returns>Zero on success, otherwise a non-zero exit code.</returns>
        public int OnExecute(CommandLineApplication app, IConsole console)
        {
            if (string.IsNullOrWhiteSpace(Input))
            {
                console.Error.WriteLine("An .edmx file is required.");
                app.ShowHelp();

                return 1;
            }

            var inputPath = Path.GetFullPath(Input);
            if (!File.Exists(inputPath))
            {
                console.Error.WriteLine($"File not found: {inputPath}");

                return 1;
            }

            ExportFormat format;
            try
            {
                format = ResolveFormat();
            }
            catch (ArgumentException ex)
            {
                console.Error.WriteLine(ex.Message);

                return 1;
            }

            var outputPath = Path.GetFullPath(
                string.IsNullOrWhiteSpace(Output)
                    ? Path.ChangeExtension(inputPath, GetExtension(format))
                    : Output);

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(inputPath, Diagram))
                {
                    var options = new DiagramExportOptions
                    {
                        FilePath = outputPath,
                        Format = format,
                        ShowTypes = ShowTypes,
                        TransparentBackground = Transparent
                    };

                    _exportManager.Export(loaded.Diagram, options);

                    console.Out.WriteLine($"Rendered '{loaded.DiagramName}' to {outputPath}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                console.Error.WriteLine(ex.Message);

                return 1;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Gets the file extension that matches a format.
        /// </summary>
        private static string GetExtension(ExportFormat format)
        {
            return format == ExportFormat.Mermaid ? "mmd" : format.ToString().ToLowerInvariant();
        }

        /// <summary>
        ///     Works out which format to render, from the explicit option or the output file's extension.
        /// </summary>
        /// <returns>The format to render.</returns>
        /// <exception cref="ArgumentException">The requested format is not recognised.</exception>
        private ExportFormat ResolveFormat()
        {
            if (!string.IsNullOrWhiteSpace(Format))
            {
                if (Enum.TryParse(Format, ignoreCase: true, result: out ExportFormat parsed))
                {
                    return parsed;
                }

                throw new ArgumentException(
                    $"Unknown format '{Format}'. Valid formats: {string.Join(", ", Enum.GetNames(typeof(ExportFormat))).ToLowerInvariant()}.");
            }

            if (!string.IsNullOrWhiteSpace(Output))
            {
                var extension = Path.GetExtension(Output);

                if (string.Equals(extension, ".mmd", StringComparison.OrdinalIgnoreCase))
                {
                    return ExportFormat.Mermaid;
                }

                return ExportManager.GetFormatFromExtension(extension);
            }

            return ExportFormat.Svg;
        }

        #endregion

    }

}
