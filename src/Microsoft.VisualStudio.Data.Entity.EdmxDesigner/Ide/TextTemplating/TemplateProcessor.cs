// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.ModelWizard.Engine;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide.TextTemplating
{
    /// <summary>
    ///     Runs a T4 template through Visual Studio's text templating service.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This was <c>TemplateActivity</c>, a Windows Workflow <c>NativeActivity</c>. The workflow context supplied
    ///         nothing but the template path and the parameter bag, both of which are now ordinary arguments.
    ///     </para>
    ///     <para>
    ///         Template inputs travel through <see cref="CallContext" /> because that is the channel the VS text
    ///         templating host exposes to a running template; the slots are always freed, even when the template throws.
    ///     </para>
    /// </remarks>
    public class TemplateProcessor
    {
        #region Fields

        private const string AssemblyDirectiveFormat = @"<#@ assembly name=""{0}"" #>";
        private static readonly Regex _assemblyDirectiveRegex = new Regex(@"<#@\s*assembly\s+name=""(.*\$\(.*\).*)""\s*#>");

        private readonly string _displayName;
        private readonly string _edmxPath;

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a <see cref="TemplateProcessor" />.
        /// </summary>
        /// <param name="edmxPath">The .edmx file the generation was launched from. Used to resolve project macros. May be <see langword="null" />.</param>
        /// <param name="displayName">A name for this step, used in error messages.</param>
        public TemplateProcessor(string edmxPath, string displayName)
        {
            _edmxPath = edmxPath;
            _displayName = displayName;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Processes a T4 template, making <paramref name="templateInputs" /> available to it.
        /// </summary>
        /// <param name="templatePath">The template's file path, which may contain project-based macros such as <c>$(DevEnvDir)</c>.</param>
        /// <param name="templateInputs">Values the template reads from <see cref="CallContext" />.</param>
        /// <returns>The output of processing the template.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the template path is not set, the text templating service is unavailable, or the template
        ///     reports errors.
        /// </exception>
        public string Process(string templatePath, IDictionary<string, object> templateInputs)
        {
            if (String.IsNullOrWhiteSpace(templatePath))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorTemplatePathNotSet, _displayName));
            }

            var inputs = templateInputs ?? new Dictionary<string, object>();

            // The EDMX path is offered to every template, not just the ones that ask for it.
            if (_edmxPath is not null
                && !inputs.ContainsKey(EdmParameterBag.ParameterName.EdmxPath.ToString()))
            {
                inputs.Add(EdmParameterBag.ParameterName.EdmxPath.ToString(), _edmxPath);
            }

            foreach (var inputName in inputs.Keys)
            {
                CallContext.LogicalSetData(inputName, inputs[inputName]);
            }

            try
            {
                return ProcessTemplate(templatePath);
            }
            finally
            {
                // We have to make sure we clear the CallContext data slots we set
                foreach (var inputName in inputs.Keys)
                {
                    CallContext.FreeNamedDataSlot(inputName);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Process a T4 template using Visual Studio's text templating service, given a path that could contain macros (i.e. "$(DevEnvDir)\...").
        ///     NOTE: Template paths that are not files or are UNC paths are not allowed
        /// </summary>
        /// <param name="templatePath">Template's file path which may contain project-based macros</param>
        /// <returns>The output of processing the template.</returns>
        private string ProcessTemplate(string templatePath)
        {
            // Attempt to resolve the full template path if it contains any macros, using
            // the edmx path to get the project and using the project's defined macros.
            Project project = null;
            if (!String.IsNullOrEmpty(_edmxPath))
            {
                ThreadHelper.JoinableTaskFactory.Run(async () => {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    project = VSHelpers.GetProjectForDocument(_edmxPath);
                });
            }

            // Resolve and validate the template file path
            DatabaseGenerationEngine.PathValidationErrorMessages errorMessages = new DatabaseGenerationEngine.PathValidationErrorMessages
                {
                    NullFile = String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorTemplatePathNotSet, _displayName),
                    NonValid = String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorTemplatePathNotValid, _displayName),
                    ParseError = String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ExceptionParsingTemplateFilePath, _displayName),
                    NonFile = String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorTemplatePathNonFile, _displayName),
                    NotInProject = String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorTemplateFileNotInProject, _displayName),
                    NonExistant = String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_TemplateFileNotExists, _displayName)
                };

            var templateFileInfo = DatabaseGenerationEngine.ResolveAndValidatePath(
                project,
                templatePath,
                errorMessages);

            var resolvedTemplatePath = templateFileInfo.FullName;

            // Callers catch any IO exceptions and wrap them in a friendly message
            var templateContents = File.ReadAllText(resolvedTemplatePath);

            // Since we are leveraging the VS T4 Host, we will have to ask the environment how to resolve any other assemblies.
            // The VS T4 Host only resolves: (a) rooted paths (b) GAC'd dlls and (c) Referenced dlls in the project, if a hierarchy is provided
            // This is a little risky here, but we will use a strict regular expression to replace the assembly references with resolved paths
            templateContents = Regex.Replace(
                templateContents, _assemblyDirectiveRegex.ToString(), match =>
                    {
                        Debug.Assert(
                            match.Groups.Count == 2,
                            "If we have a match, we should only ever have two groups, the last of which is the assembly path");
                        if (match.Groups.Count == 2)
                        {
                            var resolvedAssemblyPath = match.Groups[1].Value;

                            // project can be null if we are running via tests. In this case, the custom macros will
                            // be used
                            resolvedAssemblyPath = VsUtils.ResolvePathWithMacro(
                                project, resolvedAssemblyPath,
                                new Dictionary<string, string>
                                    {
                                        { VsUtils.DevEnvDirMacroName, VsUtils.GetVisualStudioInstallDir() },
                                        { ExtensibleFileManager.EFTOOLS_USER_MACRONAME, ExtensibleFileManager.UserEFToolsDir.FullName },
                                        { ExtensibleFileManager.EFTOOLS_VS_MACRONAME, ExtensibleFileManager.VSEFToolsDir.FullName }
                                    });
                            return String.Format(CultureInfo.InvariantCulture, AssemblyDirectiveFormat, resolvedAssemblyPath);
                        }
                        return match.Value;
                    });

            ITextTemplating textTemplatingService = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(STextTemplating)) as ITextTemplating;
            Debug.Assert(textTemplatingService != null, "ITextTemplating could not be found from the IServiceProvider");
            if (textTemplatingService is null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.DatabaseCreation_ErrorTextTemplatingServiceNotFound, resolvedTemplatePath));
            }

            // Process the template, keeping track of errors
            TemplateCallback templateCallback = new TemplateCallback();

            textTemplatingService.BeginErrorSession();
            var templateOutput = textTemplatingService.ProcessTemplate(resolvedTemplatePath, templateContents, templateCallback, null);
            if (textTemplatingService.EndErrorSession())
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.TemplateErrorsEncountered, resolvedTemplatePath,
                        templateCallback.ErrorStringBuilder));
            }

            return templateOutput;
        }

        #endregion
    }
}
