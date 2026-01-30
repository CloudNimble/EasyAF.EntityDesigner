// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EnvDTE;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using VSLangProj80;
using VsWebSite;

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    internal class DatabaseGenerationAssemblyLoader : IAssemblyLoader
    {
        private readonly bool _isWebsite;
        private readonly Dictionary<string, string> _assembliesInstalledUnderVisualStudio;
        private readonly Dictionary<string, Reference3> _projectReferenceLookup;
        private readonly Dictionary<string, AssemblyReference> _websiteReferenceLookup;

        internal DatabaseGenerationAssemblyLoader(Project project, string vsInstallPath)
        {
            _assembliesInstalledUnderVisualStudio = new Dictionary<string, string>
            {
                // For these DLLs we should use the version pre-installed under the VS directory,
                // not whatever reference the project may have
                { "ENTITYFRAMEWORK", Path.Combine(vsInstallPath, "EntityFramework.dll") },
                { "ENTITYFRAMEWORK.SQLSERVER", Path.Combine(vsInstallPath, "EntityFramework.SqlServer.dll") },
                { "ENTITYFRAMEWORK.SQLSERVERCOMPACT", Path.Combine(vsInstallPath, "EntityFramework.SqlServerCompact.dll") }
            };

            _projectReferenceLookup = [];
            _websiteReferenceLookup = [];
            if (project != null)
            {
                if (project.Object is VSProject2 vsProject)
                {
                    _isWebsite = false;
                    CacheProjectReferences(vsProject);
                }
                else if (project.Object is VSWebSite vsWebSite)
                {
                    _isWebsite = true;
                    CacheWebsiteReferences(vsWebSite);
                }
            }
        }

        private void CacheProjectReferences(VSProject2 vsProject)
        {
            foreach (Reference3 reference in vsProject.References)
            {
                if (_assembliesInstalledUnderVisualStudio.ContainsKey(reference.Identity.ToUpperInvariant()))
                {
                    // Ignore these DLLs - should be loaded using _assembliesInstalledUnderVisualStudio instead
                    continue;
                }

                if (reference.Resolved
                    && !string.IsNullOrEmpty(reference.Path)
                    && !_projectReferenceLookup.ContainsKey(reference.Identity))
                {
                    _projectReferenceLookup.Add(reference.Identity, reference);
                }
            }
        }

        private void CacheWebsiteReferences(VSWebSite vsWebSite)
        {
            foreach (AssemblyReference reference in vsWebSite.References)
            {
                // FullPath is non-empty for everything except GAC'd assemblies, which our schema context does not care about.
                // We will use this to determine the assembly name without path or extension, which is equivalent to the 'Identity'.
                var indexOfLastBackslash = reference.FullPath.LastIndexOf('\\');
                var startOfAssemblyName = indexOfLastBackslash != -1 ? indexOfLastBackslash + 1 : 0;
                var assemblyName = Path.GetFileNameWithoutExtension(reference.FullPath.Substring(startOfAssemblyName));

                if (_assembliesInstalledUnderVisualStudio.ContainsKey(assemblyName.ToUpperInvariant()))
                {
                    // Ignore these DLLs - should be loaded using _assembliesInstalledUnderVisualStudio instead
                    continue;
                }

                if (!_websiteReferenceLookup.ContainsKey(assemblyName))
                {
                    _websiteReferenceLookup.Add(assemblyName, reference);
                }
            }
        }

        #region IAssemblyLoader Members

        public Assembly LoadAssembly(string assemblyName)
        {
            var pathToLoad = GetAssemblyPath(assemblyName);
            if (!string.IsNullOrEmpty(pathToLoad))
            {
                if (File.Exists(pathToLoad))
                {
                    return Assembly.LoadFrom(pathToLoad);
                }
            }
            return null;
        }

        #endregion

        internal string GetAssemblyPath(string assemblyName)
        {
            if (_assembliesInstalledUnderVisualStudio.TryGetValue(assemblyName.ToUpperInvariant(), out string pathToAssembly))
            {
                return pathToAssembly;
            }

            if (_isWebsite)
            {
                if (_websiteReferenceLookup.TryGetValue(assemblyName, out AssemblyReference assemblyReference))
                {
                    return assemblyReference.FullPath;
                }
            }
            else
            {
                if (_projectReferenceLookup.TryGetValue(assemblyName, out Reference3 assemblyReference))
                {
                    return assemblyReference.Path;
                }
            }

            return null;
        }
    }
}
