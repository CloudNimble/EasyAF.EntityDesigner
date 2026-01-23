// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio
{
    using System;
    using System.Runtime.Versioning;
    using EnvDTE;

    internal class NetFrameworkVersioningHelper
    {
        private const string NetFrameworkMonikerIdentifier = ".NETFramework";
        private const string NetCoreMonikerIdentifier = ".NETCoreApp";
        private const string NetMonikerIdentifier = ".NET";

        public static readonly Version NetFrameworkVersion3_5 = new Version(3, 5);
        public static readonly Version NetFrameworkVersion4 = new Version(4, 0);
        public static readonly Version NetFrameworkVersion4_5 = new Version(4, 5);

        /// <summary>
        /// Gets the target .NET Framework version for the project.
        /// Returns null for modern .NET projects (.NET Core, .NET 5+).
        /// </summary>
        public static Version TargetNetFrameworkVersion(Project project, IServiceProvider serviceProvider)
        {
            var frameworkName = GetFrameworkName(project, serviceProvider);
            return frameworkName != null && frameworkName.Identifier == NetFrameworkMonikerIdentifier
                       ? frameworkName.Version
                       : null;
        }

        /// <summary>
        /// Checks if the project targets modern .NET (.NET Core 3.1+, .NET 5+, etc.)
        /// </summary>
        public static bool IsModernDotNetProject(Project project, IServiceProvider serviceProvider)
        {
            var frameworkName = GetFrameworkName(project, serviceProvider);
            if (frameworkName == null)
            {
                return false;
            }

            // Check for .NET 5+ (uses ".NET" identifier) or .NET Core (uses ".NETCoreApp" identifier)
            return frameworkName.Identifier == NetMonikerIdentifier ||
                   frameworkName.Identifier == NetCoreMonikerIdentifier;
        }

        /// <summary>
        /// Gets the target runtime version for the project, supporting both .NET Framework and modern .NET.
        /// For modern .NET projects, returns the major version (e.g., 8.0 for .NET 8).
        /// </summary>
        public static Version TargetRuntimeVersion(Project project, IServiceProvider serviceProvider)
        {
            var frameworkName = GetFrameworkName(project, serviceProvider);
            if (frameworkName == null)
            {
                return null;
            }

            // Return version for any supported runtime (.NET Framework, .NET Core, or .NET 5+)
            if (frameworkName.Identifier == NetFrameworkMonikerIdentifier ||
                frameworkName.Identifier == NetCoreMonikerIdentifier ||
                frameworkName.Identifier == NetMonikerIdentifier)
            {
                return frameworkName.Version;
            }

            return null;
        }

        /// <summary>
        /// Checks if the project targets any supported .NET runtime.
        /// Returns true for .NET Framework 3.5+ and all modern .NET versions.
        /// </summary>
        public static bool IsSupportedDotNetProject(Project project, IServiceProvider serviceProvider)
        {
            var frameworkName = GetFrameworkName(project, serviceProvider);
            if (frameworkName == null)
            {
                return false;
            }

            // Modern .NET projects are always supported
            if (frameworkName.Identifier == NetMonikerIdentifier ||
                frameworkName.Identifier == NetCoreMonikerIdentifier)
            {
                return true;
            }

            // .NET Framework 3.5+ is supported
            if (frameworkName.Identifier == NetFrameworkMonikerIdentifier)
            {
                return frameworkName.Version >= NetFrameworkVersion3_5;
            }

            return false;
        }

        private static FrameworkName GetFrameworkName(Project project, IServiceProvider serviceProvider)
        {
            var targetFrameworkMoniker = VsUtils.GetTargetFrameworkMonikerForProject(project, serviceProvider);

            if (!string.IsNullOrWhiteSpace(targetFrameworkMoniker))
            {
                try
                {
                    return new FrameworkName(targetFrameworkMoniker);
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }
    }
}
