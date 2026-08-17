// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package
{

    /// <summary>
    ///     The Entity Data Model package's own surface: the windows, managers and caches it owns.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is what the designer's host provides, as distinct from the services Visual Studio provides.
    ///         Because it derives from <see cref="IXmlDesignerPackage" /> it is also an
    ///         <see cref="System.IServiceProvider" />, so it can answer the service lookups in
    ///         <see cref="VisualStudioDataEntity_IServiceProviderExtensions" />.
    ///     </para>
    ///     <para>
    ///         Reached through <see cref="PackageManager.Package" />. See specs/layer-map.md for why nothing below
    ///         the shell layer should be asking for it.
    ///     </para>
    /// </remarks>
    internal interface IEdmPackage : IXmlDesignerPackage
    {

        #region Properties

        /// <summary>
        ///     Cache of the aggregate project type GUIDs, which are expensive to query repeatedly.
        /// </summary>
        AggregateProjectTypeGuidCache AggregateProjectTypeGuidCache { get; }

        /// <summary>
        ///     The designer's menu commands.
        /// </summary>
        IEntityDesignCommandSet CommandSet { get; }

        /// <summary>
        ///     Tracks the database connections the designer's models are built against.
        /// </summary>
        ConnectionManager ConnectionManager { get; }

        /// <summary>
        ///     The Model Browser tool window.
        /// </summary>
        ExplorerWindow ExplorerWindow { get; }

        /// <summary>
        ///     Whether the package was loaded by a command line build rather than by the IDE.
        /// </summary>
        /// <remarks>
        ///     Lets code that would otherwise show UI or assume a live shell take a quieter path.
        /// </remarks>
        bool IsBuildingFromCommandLine { get; }

        /// <summary>
        ///     The Mapping Details tool window.
        /// </summary>
        MappingDetailsWindow MappingDetailsWindow { get; }

        /// <summary>
        ///     Listens for project and file changes that affect open models.
        /// </summary>
        ModelChangeEventListener ModelChangeEventListener { get; }

        /// <summary>
        ///     Errors captured during model generation, held until they can be shown.
        /// </summary>
        ModelGenErrorCache ModelGenErrorCache { get; }

        /// <summary>
        ///     The model manager owning the artifacts this package has open.
        /// </summary>
        new EntityDesignModelManager ModelManager { get; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Tells the designer that a document was renamed, so it can re-point anything holding the old name.
        /// </summary>
        /// <param name="oldFileName">The document's previous full path.</param>
        /// <param name="newFileName">The document's new full path.</param>
        void OnFileNameChanged(string oldFileName, string newFileName);

        #endregion

    }

}
