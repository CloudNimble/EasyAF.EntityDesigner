// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.Explorer
{

    /// <summary>
    ///     Opens and closes the designer's diagrams, and reports which are open.
    /// </summary>
    /// <remarks>
    ///     Implemented by the document view, which owns the window frames the diagrams live in. Consumers work in
    ///     terms of diagram monikers so they do not need to know how a diagram maps onto a frame.
    /// </remarks>
    internal interface IDiagramManager
    {

        #region Properties

        /// <summary>
        ///     The diagram currently in focus, or <see langword="null" /> if none is.
        /// </summary>
        IViewDiagram ActiveDiagram { get; }

        /// <summary>
        ///     The first open diagram, or <see langword="null" /> if none are open.
        /// </summary>
        IViewDiagram FirstOpenDiagram { get; }

        /// <summary>
        ///     Every diagram currently open.
        /// </summary>
        IEnumerable<IViewDiagram> OpenDiagrams { get; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Closes every open diagram.
        /// </summary>
        void CloseAllDiagrams();

        /// <summary>
        ///     Closes the diagram identified by <paramref name="diagramMoniker" />.
        /// </summary>
        /// <param name="diagramMoniker">Identifies the diagram to close.</param>
        void CloseDiagram(string diagramMoniker);

        /// <summary>
        ///     Opens the diagram identified by <paramref name="diagramMoniker" />.
        /// </summary>
        /// <param name="diagramMoniker">Identifies the diagram to open.</param>
        /// <param name="openInNewTab">
        ///     <see langword="true" /> to open in a new tab, leaving any current diagram open.
        /// </param>
        void OpenDiagram(string diagramMoniker, bool openInNewTab);

        #endregion

    }

}
