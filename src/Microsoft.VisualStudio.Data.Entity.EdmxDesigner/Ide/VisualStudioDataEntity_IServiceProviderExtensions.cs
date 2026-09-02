// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Modeling.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide
{

    /// <summary>
    ///     Typed lookups for the Visual Studio services this designer uses.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         These replace a static <c>Services</c> class that held its own ambient
    ///         <see cref="IServiceProvider" />. Because <see cref="IEdmPackage" /> is itself an
    ///         <see cref="IServiceProvider" />, <c>PackageManager.Package.GetSolution()</c> reads much as the old
    ///         ambient lookup did, but a document view, a doc data or a test double can answer just as well — the
    ///         receiver is the provider, not a global hiding behind one.
    ///     </para>
    ///     <para>
    ///         Most Visual Studio services are registered under an <c>SVs*</c> service type and returned as a
    ///         separate <c>IVs*</c> interface, which is why these are not simply
    ///         <c>GetService&lt;IVsSomething&gt;()</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var rdt = PackageManager.Package.GetRunningDocumentTable();
    ///     var solution = serviceProvider.GetSolution();
    ///     </code>
    /// </example>
    internal static class VisualStudioDataEntity_IServiceProviderExtensions
    {

        #region Public Methods

        /// <summary>
        ///     Gets the selection service the designer tracks the active document view with.
        /// </summary>
        /// <param name="serviceProvider">The provider to ask.</param>
        /// <returns>The service, or <see langword="null" /> if it is not available.</returns>
        internal static IMonitorSelectionService GetMonitorSelectionService(this IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService(typeof(IMonitorSelectionService)) as IMonitorSelectionService;
        }

        /// <summary>
        ///     Gets the menu command service used to add and find designer commands.
        /// </summary>
        /// <param name="serviceProvider">The provider to ask.</param>
        /// <returns>The service, or <see langword="null" /> if it is not available.</returns>
        internal static OleMenuCommandService GetOleMenuCommandService(this IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
        }

        /// <summary>
        ///     Gets the running document table.
        /// </summary>
        /// <param name="serviceProvider">The provider to ask.</param>
        /// <returns>The service, or <see langword="null" /> if it is not available.</returns>
        internal static IVsRunningDocumentTable GetRunningDocumentTable(this IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        }

        /// <summary>
        ///     Gets the solution.
        /// </summary>
        /// <param name="serviceProvider">The provider to ask.</param>
        /// <returns>The service, or <see langword="null" /> if it is not available.</returns>
        /// <remarks>
        ///     Queries by <see cref="IVsSolution" /> rather than <c>SVsSolution</c>, preserving exactly what the
        ///     <c>Services</c> class it replaced did. Most services need the <c>SVs*</c> type; if this one turns
        ///     out to return null in the shell, that is a pre-existing defect rather than a regression here.
        /// </remarks>
        internal static IVsSolution GetSolution(this IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService(typeof(IVsSolution)) as IVsSolution;
        }

        /// <summary>
        ///     Gets the solution build manager.
        /// </summary>
        /// <param name="serviceProvider">The provider to ask.</param>
        /// <returns>The service, or <see langword="null" /> if it is not available.</returns>
        internal static IVsSolutionBuildManager2 GetSolutionBuildManager(this IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
        }

        #endregion

    }

}
