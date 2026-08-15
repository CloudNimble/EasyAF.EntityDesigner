// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.VisualStudio.Modeling;
using System;

namespace Microsoft.Data.Entity.Design.Dsl
{
    /// <summary>
    ///     Values the host pushes into a <see cref="Store" /> before asking the designer to load it.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The designer needs a few things it cannot work out for itself — chiefly the
    ///         <see cref="EditingContext" /> that identifies which document is being loaded. Whoever owns the
    ///         document owns that context, so it is handed in rather than looked up. This is what lets the same
    ///         designer assembly load under Visual Studio and under the command line renderer.
    ///     </para>
    ///     <para>
    ///         The store's property bag is the channel because it is scoped to one document and already carries
    ///         load-time state. See specs/layer-map.md.
    ///     </para>
    /// </remarks>
    internal static class DesignerStoreProperties
    {

        #region Fields

        /// <summary>
        ///     Key under which the host stores the <see cref="EditingContext" /> for the document being loaded.
        /// </summary>
        internal const string EditingContextKey = "EntityDesigner.EditingContext";

        #endregion

        #region Public Methods

        /// <summary>
        ///     Reads the editing context the host pushed in for this store.
        /// </summary>
        /// <param name="store">The store being loaded.</param>
        /// <returns>The editing context for the document.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="store" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">No editing context was pushed in before the load.</exception>
        /// <example>
        ///     <code>
        ///     var context = DesignerStoreProperties.GetEditingContext(partition.Store);
        ///     </code>
        /// </example>
        internal static EditingContext GetEditingContext(Store store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (store.PropertyBag.TryGetValue(EditingContextKey, out var value)
                && value is EditingContext context)
            {
                return context;
            }

            throw new InvalidOperationException(
                $"No editing context was pushed into the store before loading. The host must set "
                + $"Store.PropertyBag[\"{EditingContextKey}\"] before the designer loads a document.");
        }

        /// <summary>
        ///     Pushes the editing context for the document about to be loaded into <paramref name="store" />.
        /// </summary>
        /// <param name="store">The store about to be loaded.</param>
        /// <param name="context">The editing context identifying the document.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="store" /> or <paramref name="context" /> is <see langword="null" />.
        /// </exception>
        internal static void SetEditingContext(Store store, EditingContext context)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            store.PropertyBag[EditingContextKey] = context;
        }

        #endregion

    }
}
