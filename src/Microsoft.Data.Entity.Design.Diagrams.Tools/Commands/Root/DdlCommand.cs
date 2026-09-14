// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Diagrams.Tools.Commands.Root
{

    /// <summary>
    /// Generates a T-SQL database script from the conceptual model in an EDMX file.
    /// </summary>
    /// <remarks>
    /// Infers the store model and mappings from the conceptual model, then renders DDL with
    /// <see cref="DatabaseScriptGenerator"/>. Runs without Visual Studio or a T4 template.
    /// </remarks>
    /// <example>
    /// <code>
    /// edmx ddl Model.edmx
    /// edmx ddl Model.edmx --output Model.sql --database Northwind --schema dbo
    /// </code>
    /// </example>
    [Command(Name = "ddl", Description = "Generate a T-SQL database script from an EDMX conceptual model.")]
    internal class DdlCommand
    {

        #region Fields

        private static readonly XNamespace EdmxNamespace = "http://schemas.microsoft.com/ado/2009/11/edmx";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the generated database. Written as a <c>USE</c> statement when supplied.
        /// </summary>
        [Option("--database", Description = "Database name written as a USE statement. Omitted from the script when empty.")]
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the path of the EDMX file to generate a script from.
        /// </summary>
        [Argument(0, Description = "Path to the .edmx file.")]
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the file to write. Defaults to the input file name with a .sql extension.
        /// </summary>
        [Option("-o|--output", Description = "File to write (defaults to the input name with a .sql extension).")]
        public string Output { get; set; }

        /// <summary>
        /// Gets or sets the ADO.NET provider invariant name. Defaults to the SSDL Provider attribute when present.
        /// </summary>
        [Option("--provider", Description = "ADO.NET provider invariant name. Defaults to the SSDL Provider attribute.")]
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the provider manifest token. Defaults to the SSDL ProviderManifestToken attribute when present.
        /// </summary>
        [Option("--provider-manifest-token", Description = "Provider manifest token. Defaults to the SSDL ProviderManifestToken attribute.")]
        public string ProviderManifestToken { get; set; }

        /// <summary>
        /// Gets or sets the schema of the generated tables. Defaults to dbo.
        /// </summary>
        [Option("--schema", Description = "Database schema for generated tables. Defaults to dbo.")]
        public string Schema { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates the arguments and writes the generated script.
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

            try
            {
                EnsureProviderFactory(
                    "Microsoft.Data.SqlClient",
                    "Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient");
                EnsureProviderFactory(
                    "System.Data.SqlClient",
                    "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient");

                var document = XDocument.Load(inputPath);
                var runtime = document.Root?.Element(EdmxNamespace + "Runtime");
                var conceptual = runtime?.Element(EdmxNamespace + "ConceptualModels")?.Elements().FirstOrDefault();
                if (conceptual is null)
                {
                    console.Error.WriteLine("The EDMX file does not contain a conceptual model.");

                    return 1;
                }

                var storage = runtime.Element(EdmxNamespace + "StorageModels")?.Elements().FirstOrDefault();
                var provider = FirstNonEmpty(
                    Provider,
                    storage?.Attribute("Provider")?.Value);
                var providerManifestToken = FirstNonEmpty(
                    ProviderManifestToken,
                    storage?.Attribute("ProviderManifestToken")?.Value);
                if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerManifestToken))
                {
                    console.Error.WriteLine(
                        "A provider and provider manifest token are required. Supply --provider and --provider-manifest-token, or generate from an EDMX that already has a store model.");

                    return 1;
                }

                var schema = FirstNonEmpty(Schema, "dbo");
                var existingSsdl = storage is null ? string.Empty : storage.ToString();
                var edm = EdmExtension.CreateAndValidateEdmItemCollection(
                    conceptual.ToString(), new Version(3, 0, 0, 0));
                var parameters = new EdmParameterBag(
                    null,
                    null,
                    new Version(3, 0, 0, 0),
                    provider,
                    providerManifestToken,
                    null,
                    schema,
                    Database,
                    null,
                    inputPath);

                var script = new DatabaseScriptGenerator().Generate(edm, existingSsdl, parameters);
                var outputPath = Path.GetFullPath(
                    string.IsNullOrWhiteSpace(Output)
                        ? Path.ChangeExtension(inputPath, ".sql")
                        : Output);

                File.WriteAllText(outputPath, script.Ddl);
                console.Out.WriteLine($"Wrote database script to {outputPath}");

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
        /// Registers an ADO.NET provider factory when the runtime has no machine.config entry for it.
        /// </summary>
        private static void EnsureProviderFactory(string invariantName, string factoryTypeName)
        {
            try
            {
                DbProviderFactories.GetFactory(invariantName);
            }
            catch (ArgumentException)
            {
                var factoryType = Type.GetType(factoryTypeName, throwOnError: false);
                var instance = factoryType?.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                    as DbProviderFactory;
                if (instance is not null)
                {
                    DbProviderFactories.RegisterFactory(invariantName, instance);
                }
            }
        }

        /// <summary>
        /// Returns the first non-empty value.
        /// </summary>
        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => false == string.IsNullOrWhiteSpace(value));
        }

        #endregion

    }

}
