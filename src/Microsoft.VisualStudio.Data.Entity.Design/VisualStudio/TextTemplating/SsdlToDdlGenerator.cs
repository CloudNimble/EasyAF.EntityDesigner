// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators;
using Microsoft.Data.Entity.Design.VersioningFacade;

namespace Microsoft.Data.Entity.Design.VisualStudio.TextTemplating
{
    /// <summary>
    ///     Renders data definition language from a store model by running a T4 template through Visual Studio's text
    ///     templating service.
    /// </summary>
    /// <remarks>
    ///     This was <c>SsdlToDdlActivity</c>, a Windows Workflow activity. It lives in this assembly rather than
    ///     Microsoft.Data.Entity.Design.DatabaseGeneration because it depends on the Visual Studio T4 host;
    ///     <see cref="IDdlGenerator" /> is the seam that keeps that dependency from reaching the generation assembly.
    /// </remarks>
    public sealed class SsdlToDdlGenerator : IDdlGenerator
    {
        #region Fields

        private const string DisplayName = "SsdlToDdl";

        #endregion

        #region Public Methods

        /// <summary>
        ///     Generates the DDL that drops the objects described by <paramref name="existingSsdl" /> and creates those
        ///     described by <paramref name="ssdl" />.
        /// </summary>
        /// <param name="ssdl">The store model to create objects for.</param>
        /// <param name="existingSsdl">The store model currently recorded in the .edmx file. May be empty.</param>
        /// <param name="edmParameterBag">Supplies the DDL template path, provider details, and target Entity Framework version.</param>
        /// <returns>The generated DDL script.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="edmParameterBag" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">Thrown when no DDL template path was supplied.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when a required parameter is missing, the target Entity Framework version is unrecognized, or the
        ///     template produces no output.
        /// </exception>
        public string Generate(string ssdl, string existingSsdl, EdmParameterBag edmParameterBag)
        {
            if (edmParameterBag is null)
            {
                throw new ArgumentNullException(nameof(edmParameterBag));
            }

            var ddlTemplatePath = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.DDLTemplatePath);
            if (String.IsNullOrWhiteSpace(ddlTemplatePath))
            {
                throw new ArgumentException(
                    String.Format(CultureInfo.CurrentCulture, Resources.DatabaseCreation_NoDDLTemplatePathSpecified, DisplayName));
            }

            var templateInputs = BuildTemplateInputs(ssdl, existingSsdl, edmParameterBag, ddlTemplatePath);

            var edmxPath = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.EdmxPath);
            var templateOutput = new TemplateProcessor(edmxPath, DisplayName).Process(ddlTemplatePath, templateInputs);

            if (String.IsNullOrEmpty(templateOutput))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorTemplateOutputNotSet, DisplayName));
            }

            return templateOutput;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Collects the values the DDL template reads, validating the ones it cannot run without.
        /// </summary>
        private static Dictionary<string, object> BuildTemplateInputs(
            string ssdl, string existingSsdl, EdmParameterBag edmParameterBag, string ddlTemplatePath)
        {
            var inputs = new Dictionary<string, object>
                {
                    { EdmConstants.ssdlOutputName, ssdl },
                    { EdmConstants.existingSsdlInputName, existingSsdl }
                };

            // Find and validate the TargetVersion parameter
            var targetFrameworkVersion = edmParameterBag.GetParameter<Version>(EdmParameterBag.ParameterName.TargetVersion);
            if (targetFrameworkVersion is null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.TargetVersion.ToString()));
            }

            if (false == EntityFrameworkVersion.IsValidVersion(targetFrameworkVersion))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorInvalidTargetVersion, targetFrameworkVersion));
            }

            inputs.Add(EdmParameterBag.ParameterName.TargetVersion.ToString(), targetFrameworkVersion);

            // Add the Provider invariant name
            var providerInvariantName = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.ProviderInvariantName);
            if (String.IsNullOrWhiteSpace(providerInvariantName))
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.ProviderInvariantName.ToString()));
            }

            inputs.Add(EdmParameterBag.ParameterName.ProviderInvariantName.ToString(), providerInvariantName);

            // Add the Database Schema Name
            var databaseSchemaName = edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.DatabaseSchemaName);
            if (String.IsNullOrWhiteSpace(databaseSchemaName))
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.DatabaseSchemaName.ToString()));
            }

            inputs.Add(EdmParameterBag.ParameterName.DatabaseSchemaName.ToString(), databaseSchemaName);

            // The provider manifest token, database name, and template path are all optional: some providers do not
            // supply a database name, and templates are free to ignore any of them.
            inputs.Add(
                EdmParameterBag.ParameterName.ProviderManifestToken.ToString(),
                edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.ProviderManifestToken));
            inputs.Add(
                EdmParameterBag.ParameterName.DatabaseName.ToString(),
                edmParameterBag.GetParameter<string>(EdmParameterBag.ParameterName.DatabaseName));
            inputs.Add(EdmParameterBag.ParameterName.DDLTemplatePath.ToString(), ddlTemplatePath);

            return inputs;
        }

        #endregion
    }
}
