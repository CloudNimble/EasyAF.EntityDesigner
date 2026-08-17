// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// Defines an EventArgs type that allows extenders of the Entity Designer to drive selection programmatically
    /// </summary>
    /// <example>
    /// <code>
    /// // Select the 'Name' property of the 'Customer' entity from inside a layer's tool window.
    /// ChangeEntityDesignerSelection?.Invoke(this,
    ///     new ChangeEntityDesignerSelectionEventArgs(ServiceProvider, new[] { "Name.Customer" }));
    /// </code>
    /// </example>
    /// <remarks>
    /// Raised through <see cref="IEntityDesignerLayer.ChangeEntityDesignerSelection" /> when a layer wants the design surface to
    /// follow a selection the user made somewhere else, such as in the layer's own tool window. The identifiers and the service
    /// provider are read by the designer and are not part of the public surface of this type.
    /// </remarks>
    public class ChangeEntityDesignerSelectionEventArgs : EventArgs
    {

        #region Properties

        /// <summary>
        /// The service provider sited by the layer that is driving the selection.
        /// </summary>
        /// <value>The <see cref="IEntityDesignerLayer.ServiceProvider" /> of the layer that raised the event.</value>
        /// <remarks>
        /// The designer uses this to work out which layer asked for the selection change.
        /// </remarks>
        internal IServiceProvider LayerServiceProvider { get; private set; }

        /// <summary>
        /// The delimited identifiers of the objects to select.
        /// </summary>
        /// <value>One identifier per object that should become selected.</value>
        /// <remarks>
        /// Each identifier walks the hierarchy of the selection from the root, so a property is addressed as
        /// "PropertyName.EntityName" and an entity or association simply by its name.
        /// </remarks>
        internal IEnumerable<string> SelectionIdentifiers { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiate an ChangeEntityDesignerSelectionEventArgs. The 'SelectionIdentifier' in this case is a delimited string
        /// that corresponds to the hierarchy of the selection from the root. For example, to select a property 'SomeProperty' in
        /// entity type 'SomeEntity', the SelectionIdentifier would be: SomeProperty.SomeEntity.
        /// </summary>
        /// <param name="layerServiceProvider">Service Provider provided by the layer extension</param>
        /// <param name="selectionIdentifiers">A set of string identifiers to drive selection in the Entity Designer</param>
        public ChangeEntityDesignerSelectionEventArgs(IServiceProvider layerServiceProvider, IEnumerable<string> selectionIdentifiers)
        {
            LayerServiceProvider = layerServiceProvider;
            SelectionIdentifiers = selectionIdentifiers;
        }

        #endregion

    }

}
