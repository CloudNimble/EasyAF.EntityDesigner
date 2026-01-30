// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using EnvDTE;
using WizardResources = Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Properties.Resources;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.VisualStudio.Shell.Design;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    internal static class ConfigFileHelper
    {
        // <summary>
        //     Updates app. or web.config to include connection strings, registers the build provider
        //     for WebSite projects and the assembly for WebApp projects
        // </summary>
        internal static void UpdateConfig(ModelBuilderSettings settings)
        {
            var metadataFileNames = 
                ConnectionManager.GetMetadataFileNamesFromArtifactFileName(settings.Project, settings.ModelPath, PackageManager.Package);

            if (settings.GenerationOption == ModelGenerationOption.GenerateFromDatabase
                || settings.GenerationOption == ModelGenerationOption.GenerateDatabaseScript)
            {
                if (settings.SaveConnectionStringInAppConfig)
                {
                    UpdateConfig(metadataFileNames, settings);
                }
            }

            // regardless of GenerationOption we always need to register the build
            // provider for web site projects and the assembly for web app projects
            if (settings.VSApplicationType == VisualStudioProjectSystem.Website)
            {
                var containingProject = settings.Project;

                RegisterBuildProvidersInWebConfig(containingProject);

                // Ensure that System.Data.Entity.Design reference assemblies are added in the web.config.
                // Get the correct assembly name based on target framework
                IVsFrameworkMultiTargeting targetInfo = PackageManager.Package.GetService(typeof(SVsFrameworkMultiTargeting)) as IVsFrameworkMultiTargeting;
                Debug.Assert(targetInfo != null, "Unable to get IVsFrameworkMultiTargeting from service provider");
                if (targetInfo != null && PackageManager.Package.GetService(typeof(SVsSmartOpenScope)) is IVsSmartOpenScope openScope)
                {
                    var targetFrameworkMoniker = VsUtils.GetTargetFrameworkMonikerForProject(containingProject, PackageManager.Package);
                    VsTargetFrameworkProvider provider = new VsTargetFrameworkProvider(targetInfo, targetFrameworkMoniker, openScope);
                    var dataEntityDesignAssembly = provider.GetReflectionAssembly(new AssemblyName("System.Data.Entity.Design"));
                    if (dataEntityDesignAssembly != null)
                    {
                        RegisterAssemblyInWebConfig(containingProject, dataEntityDesignAssembly.FullName);
                    }
                }
            }
        }

        private static void UpdateConfig(ICollection<string> metadataFiles, ModelBuilderSettings settings)
        {
            var statusMessage =
                VsUtils.IsWebProject(settings.VSApplicationType)
                    ? String.Format(CultureInfo.CurrentCulture, WizardResources.Engine_WebConfigSuccess, VsUtils.WebConfigFileName)
                    : String.Format(CultureInfo.CurrentCulture, WizardResources.Engine_AppConfigSuccess, VsUtils.AppConfigFileName);

                try
                {
                    var manager = PackageManager.Package.ConnectionManager;
                    manager.AddConnectionString(
                        settings.Project,
                        settings.VSApplicationType,
                        metadataFiles,
                        settings.AppConfigConnectionPropertyName,
                        settings.AppConfigConnectionString,
                        settings.RuntimeProviderInvariantName);
                }
                catch (Exception e)
                {
                    statusMessage =
                        String.Format(
                            CultureInfo.CurrentCulture,
                            VsUtils.IsWebProject(settings.VSApplicationType)
                                ? WizardResources.Engine_WebConfigException
                                : WizardResources.Engine_AppConfigException,
                            e.Message);
                }
                VsUtils.LogOutputWindowPaneMessage(settings.Project, statusMessage);
        }

        private static void RegisterBuildProvidersInWebConfig(Project project)
        {
            Debug.Assert(project != null, "project is null");

            var statusMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    WizardResources.Engine_WebConfigBPSuccess,
                    VsUtils.WebConfigFileName);

            try
            {
                VsUtils.RegisterBuildProviders(project);
            }
            catch (Exception e)
            {
                statusMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    WizardResources.Engine_WebConfigBPException,
                    e.Message);
            }

            VsUtils.LogOutputWindowPaneMessage(project, statusMessage);
        }

        private static void RegisterAssemblyInWebConfig(Project project, string assemblyFullName)
        {
            Debug.Assert(project != null, "project is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(assemblyFullName), "invalid assembly name");

            var statusMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    WizardResources.Engine_WebConfigAssemblySuccess,
                    assemblyFullName);

            try
            {
                VsUtils.RegisterAssembly(project, assemblyFullName);
            }
            catch (Exception e)
            {
                statusMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    WizardResources.Engine_WebConfigAssemblyException,
                    assemblyFullName, e.Message);
            }

            VsUtils.LogOutputWindowPaneMessage(project, statusMessage);
        }
    }
}