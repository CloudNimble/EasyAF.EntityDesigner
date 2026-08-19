// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.Entity.Design.Diagrams.Layout;
using Microsoft.Data.Entity.Design.Diagrams.Tools.Commands.Root;
using Microsoft.Data.Entity.Design.Diagrams.Tools.Export;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Export.Raster;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Export;

namespace Microsoft.Data.Entity.Design.Diagrams.Tools
{

    /// <summary>
    /// Entry point for the Entity Designer command line tool.
    /// </summary>
    class Program
    {

        /// <summary>
        /// Builds the host and runs the requested command.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The process exit code.</returns>
        public static Task<int> Main(string[] args)
        {
            DisableAssertDialogs();

            return Host.CreateDefaultBuilder()
                // RWM: If this is not set, it won't find appsettings.json.
                //      https://github.com/dotnet/sdk/issues/9730#issuecomment-433724425
                .UseContentRoot(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName)
                .ConfigureServices((context, services) =>
                {
                    // Diagram.CreateBitmap needs Visual Studio, so this host rasterises the generated SVG instead.
                    services.AddSingleton<IRasterExporter, SvgRasterExporter>();
                    services.AddSingleton<ExportManager>();

                    // Layout engines. Registration order is the priority order - LayoutEngineManager takes the
                    // first as its default, and IEnumerable<T> resolves in the order registered here.
                    services.AddSingleton<LayoutEngineBase, DslLayoutEngine>();
                    services.AddSingleton<LayoutEngineBase, MsAglLayoutEngine>();
                    services.AddSingleton<LayoutEngineManager>();
                })
                .RunCommandLineApplicationAsync<EntityDesignerRootCommand>(args);
        }

        /// <summary>
        /// Routes assertion failures to standard error instead of a modal dialog.
        /// </summary>
        /// <remarks>
        /// The designer assemblies are full of Debug.Assert calls that assume a developer is sitting in front of
        /// Visual Studio. In a Debug build of a command line tool the default listener puts a dialog on screen and
        /// waits, which hangs the process - and would hang a build agent indefinitely. Writing to stderr keeps the
        /// diagnostic without blocking. Release builds compile the asserts away entirely.
        /// </remarks>
        private static void DisableAssertDialogs()
        {
            foreach (var listener in Trace.Listeners.OfType<DefaultTraceListener>())
            {
                listener.AssertUiEnabled = false;
            }
        }

    }

}
