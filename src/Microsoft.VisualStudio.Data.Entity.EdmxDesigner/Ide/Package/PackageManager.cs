// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package
{

    /// <summary>
    ///     Holds the loaded Entity Data Model package, and loads it on demand.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Visual Studio loads packages lazily, so an entry point such as an item template wizard or a single
    ///         file generator can run before this package exists. Those entry points call
    ///         <see cref="LoadEDMPackage(IServiceProvider)" /> to force it, after which
    ///         <see cref="Package" /> answers.
    ///     </para>
    ///     <para>
    ///         This is process wide mutable state. It is tolerable in the shell layer, where there is exactly one
    ///         package per process, and unacceptable below it — see specs/layer-map.md.
    ///     </para>
    /// </remarks>
    internal static class PackageManager
    {

        #region Fields

        private static IEdmPackage _package;

        #endregion

        #region Properties

        /// <summary>
        ///     The loaded Entity Data Model package.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The package has not been loaded. Reaching for it before then is a programming error, not a
        ///     condition to test for.
        /// </exception>
        /// <remarks>
        ///     This used to <c>Debug.Assert</c> and then return null regardless, which guarded nothing: Debug
        ///     builds put a modal dialog on screen — hanging any test run without a shell — while Release builds
        ///     returned null and left the caller to fail somewhere unrelated. Throwing fails the same way in both
        ///     configurations, at the point of the mistake, and says what to do about it.
        /// </remarks>
        public static IEdmPackage Package
        {
            get
            {
                if (_package is null)
                {
                    throw new InvalidOperationException(
                        "The Entity Data Model package has not been loaded. Call PackageManager.LoadEDMPackage "
                        + "before using PackageManager.Package.");
                }

                return _package;
            }
            set { _package = value; }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Loads the package through the global shell service, if it is not already loaded.
        /// </summary>
        internal static void LoadEDMPackage()
        {
            if (_package is null)
            {
                IVsShell vsShell = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsShell)) as IVsShell;
                LoadEDMPackage(vsShell);
            }
        }

        /// <summary>
        ///     Loads the package through <paramref name="serviceProvider" />, if it is not already loaded.
        /// </summary>
        /// <param name="serviceProvider">The provider to resolve the shell service from.</param>
        /// <remarks>
        ///     Falls back to the global shell service when <paramref name="serviceProvider" /> cannot supply one.
        /// </remarks>
        internal static void LoadEDMPackage(IServiceProvider serviceProvider)
        {
            if (_package is null)
            {
                IVsShell vsShell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
                if (vsShell is not null)
                {
                    LoadEDMPackage(vsShell);
                }
                else
                {
                    LoadEDMPackage();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Asks the shell to load the package by GUID, if it has not loaded it already.
        /// </summary>
        /// <param name="vsShell">The shell service.</param>
        /// <exception cref="InvalidOperationException">The shell reported that the package failed to load.</exception>
        private static void LoadEDMPackage(IVsShell vsShell)
        {
            Debug.Assert(vsShell != null, "unexpected null value for vsShell");
            IVsPackage package = null;
            if (vsShell is not null)
            {
                var packageGuid = PackageConstants.guidEscherPkg;
                var hr = vsShell.IsPackageLoaded(ref packageGuid, out package);
                if (NativeMethods.Failed(hr) || package is null)
                {
                    hr = vsShell.LoadPackage(ref packageGuid, out package);
                    if (NativeMethods.Failed(hr))
                    {
                        var msg = String.Format(CultureInfo.CurrentCulture, EdmxDesignerResources.PackageLoadFailureExceptionMessage, hr);
                        throw new InvalidOperationException(msg);
                    }
                }
            }
        }

        #endregion

    }

}
