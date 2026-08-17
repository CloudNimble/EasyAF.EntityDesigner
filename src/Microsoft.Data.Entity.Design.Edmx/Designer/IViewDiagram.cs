// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Model.Designer
{
    /// <summary>
    ///     A diagram that can bring an <see cref="EFElement" /> into view.
    /// </summary>
    /// <remarks>
    ///     Implemented by the DSL designer surface and consumed by the Visual Studio shell, so it lives in the
    ///     model, which both can see. It was previously declared in the Visual Studio project, which meant the
    ///     designer took a dependency on the shell in order to describe itself — a base type pointing at the layer
    ///     above it. See specs/dsl-shell-decoupling.md.
    /// </remarks>
    internal interface IViewDiagram
    {

        #region Properties

        /// <summary>
        ///     Identifier of the diagram this view presents.
        /// </summary>
        string DiagramId { get; }

        #endregion

        #region Methods

        /// <summary>
        ///     Brings <paramref name="efElement" /> into view, adding it to the diagram if it is not already shown.
        /// </summary>
        /// <param name="efElement">The element to show.</param>
        void AddOrShowEFElementInDiagram(EFElement efElement);

        #endregion

    }
}
