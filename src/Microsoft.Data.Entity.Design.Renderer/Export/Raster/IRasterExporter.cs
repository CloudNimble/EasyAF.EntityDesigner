// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityDesigner.View;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    /// <summary>
    ///     Writes a diagram to a raster image file.
    /// </summary>
    /// <remarks>
    ///     There are two implementations because the best available renderer differs by host, not because the output
    ///     is meant to be interchangeable. Inside Visual Studio, <c>ShellRasterExporter</c> calls
    ///     <c>Diagram.CreateBitmap</c> so the image matches what the designer has always produced. That method
    ///     resolves <c>SVsUIShell</c> and cannot run outside the shell, and the DSL SDK is closed source so its GDI+
    ///     painting cannot be lifted out. A command line host therefore supplies its own implementation that
    ///     rasterises the SVG the renderer generates.
    /// </remarks>
    internal interface IRasterExporter
    {
        /// <summary>
        ///     Writes <paramref name="diagram" /> to the file named by <paramref name="options" />.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options, including the destination path and image format.</param>
        void Export(EntityDesignerDiagram diagram, DiagramExportOptions options);
    }
}
