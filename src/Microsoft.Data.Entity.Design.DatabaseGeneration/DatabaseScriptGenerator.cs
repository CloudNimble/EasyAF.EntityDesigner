// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators;
using Microsoft.Data.Entity.Design.DatabaseGeneration.Properties;
using Microsoft.Data.Entity.Design.EntityFramework;

namespace Microsoft.Data.Entity.Design.DatabaseGeneration
{
    /// <summary>
    ///     Generates the store model, mappings, and database script for a conceptual model.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This replaces the Windows Workflow graph that the Generate Database Wizard used to deserialize from
    ///         <c>TablePerTypeStrategy.xaml</c>. That graph was a two-step <c>Sequence</c> — infer SSDL and MSL from the
    ///         CSDL, then render DDL from the SSDL — so the workflow runtime bought sequencing that a method already
    ///         expresses. Removing it frees this assembly from <c>System.Activities</c>, which has no .NET Core successor.
    ///     </para>
    ///     <para>
    ///         The generators remain replaceable through <see cref="ISchemaGenerator" /> and <see cref="IDdlGenerator" />,
    ///         but they are now supplied as objects rather than named by assembly-qualified type name in a XAML file.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var generator = new DatabaseScriptGenerator(new SsdlToDdlGenerator());
    ///     DatabaseScript script = generator.Generate(edmItemCollection, existingSsdl, parameters);
    ///     Console.WriteLine(script.Ddl);
    ///     </code>
    /// </example>
    public sealed class DatabaseScriptGenerator
    {
        #region Fields

        private readonly IDdlGenerator _ddlGenerator;
        private readonly ISchemaGenerator _mslGenerator;
        private readonly ISchemaGenerator _ssdlGenerator;

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a generator that uses the in-box <see cref="CsdlToSsdl" /> and <see cref="CsdlToMsl" /> generators.
        /// </summary>
        /// <param name="ddlGenerator">Renders DDL from the inferred store model.</param>
        public DatabaseScriptGenerator(IDdlGenerator ddlGenerator)
            : this(ddlGenerator, new CsdlToSsdl(), new CsdlToMsl())
        {
        }

        /// <summary>
        ///     Creates a generator with explicit schema generators.
        /// </summary>
        /// <param name="ddlGenerator">Renders DDL from the inferred store model.</param>
        /// <param name="ssdlGenerator">Infers the store model from the conceptual model.</param>
        /// <param name="mslGenerator">Infers the mappings from the conceptual model.</param>
        /// <exception cref="ArgumentNullException">Thrown when any generator is <see langword="null" />.</exception>
        public DatabaseScriptGenerator(IDdlGenerator ddlGenerator, ISchemaGenerator ssdlGenerator, ISchemaGenerator mslGenerator)
        {
            if (ddlGenerator is null)
            {
                throw new ArgumentNullException(nameof(ddlGenerator));
            }

            if (ssdlGenerator is null)
            {
                throw new ArgumentNullException(nameof(ssdlGenerator));
            }

            if (mslGenerator is null)
            {
                throw new ArgumentNullException(nameof(mslGenerator));
            }

            _ddlGenerator = ddlGenerator;
            _ssdlGenerator = ssdlGenerator;
            _mslGenerator = mslGenerator;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Generates the store model, mappings, and database script for the supplied conceptual model.
        /// </summary>
        /// <param name="edmItemCollection">The conceptual model to generate a database for.</param>
        /// <param name="existingSsdl">The store model currently recorded in the .edmx file, used to drop stale objects. May be empty.</param>
        /// <param name="edmParameterBag">The parameters the generators need.</param>
        /// <returns>The generated SSDL, MSL, and DDL.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="edmParameterBag" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the conceptual model is missing, the target Entity Framework version is absent or unrecognized, or
        ///     the inferred store model fails validation.
        /// </exception>
        public DatabaseScript Generate(EdmItemCollection edmItemCollection, string existingSsdl, EdmParameterBag edmParameterBag)
        {
            if (edmItemCollection is null)
            {
                throw new InvalidOperationException(Resources.ErrorCouldNotFindCSDL);
            }

            if (edmParameterBag is null)
            {
                throw new ArgumentNullException(nameof(edmParameterBag));
            }

            var targetFrameworkVersion = edmParameterBag.GetParameter<Version>(EdmParameterBag.ParameterName.TargetVersion);
            if (targetFrameworkVersion is null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.TargetVersion.ToString()));
            }

            if (false == EntityFrameworkVersion.IsValidVersion(targetFrameworkVersion))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, Resources.ErrorNonValidTargetVersion, targetFrameworkVersion));
            }

            var ssdl = _ssdlGenerator.Generate(edmItemCollection, edmParameterBag);
            var msl = _mslGenerator.Generate(edmItemCollection, edmParameterBag);

            // Validate the SSDL, but catch any naming errors and throw a friendlier one
            var ssdlCollection = EdmExtension.CreateAndValidateStoreItemCollection(
                ssdl, targetFrameworkVersion, DependencyResolver.Instance, true);

            ValidateMapping(edmItemCollection, ssdlCollection, msl);

            var ddl = _ddlGenerator.Generate(ssdl, existingSsdl, edmParameterBag);

            return new DatabaseScript(ssdl, msl, ddl);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Asserts in Debug builds that the generated mappings bind the conceptual model to the inferred store model.
        /// </summary>
        /// <remarks>
        ///     Mapping errors here indicate a defect in the generators rather than bad user input, so this stays an
        ///     assert rather than becoming a thrown exception.
        /// </remarks>
        [Conditional("DEBUG")]
        private static void ValidateMapping(EdmItemCollection edmItemCollection, StoreItemCollection ssdlCollection, string msl)
        {
            EdmExtension.CreateStorageMappingItemCollection(
                edmItemCollection, ssdlCollection, msl, out IList<EdmSchemaError> mslErrors);
            if (mslErrors is null
                || mslErrors.Count == 0)
            {
                return;
            }

            var errorSb = new StringBuilder();
            errorSb.AppendLine("Encountered the following errors while validating the MSL:");
            foreach (var error in mslErrors)
            {
                errorSb.AppendLine(error.Message);
            }

            Debug.Fail(errorSb.ToString());
        }

        #endregion
    }
}
