// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;

namespace Microsoft.Data.Entity.Tools.Commands.Root
{

    /// <summary>
    /// Root command for the Entity Designer command line tool.
    /// </summary>
    [Command(Name = "edmx", Description = "Entity Designer commands for working with EDMX files outside Visual Studio.")]
    [Subcommand(typeof(RenderCommand))]
    internal partial class EntityDesignerRootCommand
    {

        /// <summary>
        /// Shows help, since this is a parent command.
        /// </summary>
        /// <param name="app">The command line application.</param>
        /// <returns>Exit code.</returns>
        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

    }

}
