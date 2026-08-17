// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// This class allows the notion of a 'layer' in the Entity Designer. Layers can be turned off and on; they are composed of:
    /// (1) Simple Metadata about the feature
    /// (2) Commands that can be executed against the feature
    /// (3) Core property extensions
    /// (4) Simple event sinks for operations that occur in the designer
    /// (5) Basic selection mechanism drivers
    /// </summary>
    /// <example>
    /// A layer is discovered through MEF by exporting <see cref="IEntityDesignerLayer" />. Other extensions join the layer by
    /// also carrying <see cref="EntityDesignerLayerAttribute" /> with the same name, which makes them appear and disappear along
    /// with it.
    /// <code>
    /// [Export(typeof(IEntityDesignerLayer))]
    /// public class MyLayer : IEntityDesignerLayer
    /// {
    ///     public string Name =&gt; "My Layer";
    ///
    ///     public bool IsSealed =&gt; false;
    ///
    ///     public IServiceProvider ServiceProvider =&gt; _serviceProvider;
    ///
    ///     public IList&lt;IEntityDesignerExtendedProperty&gt; Properties =&gt; _properties;
    ///
    ///     public event EventHandler&lt;ChangeEntityDesignerSelectionEventArgs&gt; ChangeEntityDesignerSelection;
    ///
    ///     public void OnAfterLayerLoaded(XObject xObject) { /* open a tool window, cache state */ }
    ///
    ///     public void OnBeforeLayerUnloaded(XObject conceptualModelXObject) { /* tear that state down */ }
    ///
    ///     public void OnAfterTransactionCommitted(IEnumerable&lt;Tuple&lt;XObject, XObjectChange&gt;&gt; xmlChanges) { /* refresh */ }
    ///
    ///     public void OnSelectionChanged(XObject selection) { /* follow the designer selection */ }
    /// }
    ///
    /// // An extension that only applies while "My Layer" is enabled.
    /// [Export(typeof(IEntityDesignerExtendedProperty))]
    /// [EntityDesignerLayer("My Layer")]
    /// [EntityDesignerExtendedProperty(EntityDesignerSelection.ConceptualModelEntityType)]
    /// public class MyEntityProperties : IEntityDesignerExtendedProperty
    /// {
    ///     public object CreateProperty(XElement xElement, PropertyExtensionContext context) =&gt; new MyDescriptor(xElement, context);
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// A layer is a named group of designer extensions that the user can switch on and off as a unit. The designer adds a
    /// command to the designer's context menu for toggling the layer, and while the layer is disabled every extension tagged
    /// with its name through <see cref="EntityDesignerLayerAttribute" /> is filtered out and never invoked. Extensions with no
    /// layer name are always active.
    /// <para>
    /// Implement this interface when a set of related extensions should behave like one optional feature of the designer -
    /// typically one that owns a tool window and needs to stay in step with what the user has selected on the design surface.
    /// </para>
    /// </remarks>
    public interface IEntityDesignerLayer
    {

        #region Properties

        /// <summary>
        /// Determines where third-party property extensions can subscribe to this layer
        /// </summary>
        /// <value><see langword="true" /> if the layer is closed to third-party property extensions.</value>
        /// <remarks>
        /// Declared as part of the layer contract, but no code in this designer currently reads it, so setting it has no
        /// observable effect today. Implementations should still return a considered value in case the check is reinstated.
        /// </remarks>
        bool IsSealed { get; }

        /// <summary>
        /// The name of the layer
        /// </summary>
        /// <value>A name that uniquely identifies the layer.</value>
        /// <remarks>
        /// This is the text the designer uses for the context-menu command that enables and disables the layer, and it is the
        /// value other extensions pass to <see cref="EntityDesignerLayerAttribute" /> to join the layer.
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Core property extensions that are automatically subscribed to this feature.
        /// </summary>
        /// <value>The <see cref="IEntityDesignerExtendedProperty" /> implementations that belong to the layer.</value>
        /// <remarks>
        /// These are supplied directly by the layer rather than discovered through MEF, which is how a layer ships its own
        /// property extensions without having to export and tag each one separately.
        /// </remarks>
        IList<IEntityDesignerExtendedProperty> Properties { get; }

        /// <summary>
        /// A layer can provide its own service provider for selection purposes. Currently the limitation is that a layer can
        /// only proffer one sited service provider.
        /// </summary>
        /// <value>The service provider the layer sites, used to resolve the layer's own services.</value>
        /// <remarks>
        /// The same instance is what the layer passes back through
        /// <see cref="ChangeEntityDesignerSelectionEventArgs" /> when it drives selection, so the designer can tell which layer
        /// asked.
        /// </remarks>
        IServiceProvider ServiceProvider { get; }

        #endregion

        #region Events

        /// <summary>
        /// Change the selection on the entity designer. The selection identifier here corresponds to either 'EntityName',
        /// 'AssociationName', or 'EntityName.PropertyName'.
        /// </summary>
        /// <remarks>
        /// Raise this event to push a selection into the designer - for example when the user picks something in the layer's own
        /// tool window and the design surface should follow. It is the mirror image of <see cref="OnSelectionChanged(XObject)" />,
        /// which reports selection changes coming the other way.
        /// </remarks>
        event EventHandler<ChangeEntityDesignerSelectionEventArgs> ChangeEntityDesignerSelection;

        #endregion

        #region Public Methods

        /// <summary>
        /// Fired after the layer is loaded.
        /// </summary>
        /// <param name="xObject">the selected object in the active designer or conceptual model if nothing is selected.</param>
        /// <remarks>
        /// Called when the user enables the layer, or when a model is opened with the layer already enabled. This is where a
        /// layer opens its tool window and builds whatever state it keeps for the current model.
        /// </remarks>
        void OnAfterLayerLoaded(XObject xObject);

        /// <summary>
        /// Gets fired when a transaction is committed. A layer extension can take basic actions in this case such as reloading
        /// an owning tool window.
        /// </summary>
        /// <param name="xmlChanges">A list of changes made during the transaction.</param>
        /// <remarks>
        /// Each entry pairs the affected node with the kind of change that was made to it, which is enough to decide whether the
        /// layer's view is now stale. The model has already been updated by the time this is called.
        /// </remarks>
        void OnAfterTransactionCommitted(IEnumerable<Tuple<XObject, XObjectChange>> xmlChanges);

        /// <summary>
        /// Fired before the layer is unloaded.
        /// </summary>
        /// <param name="conceptualModelXObject">The conceptual model.</param>
        /// <remarks>
        /// Called when the user disables the layer or the model is closed. Undo whatever <see cref="OnAfterLayerLoaded(XObject)" />
        /// set up; the layer must not hold on to the model after this returns.
        /// </remarks>
        void OnBeforeLayerUnloaded(XObject conceptualModelXObject);

        /// <summary>
        /// Fired when selection is changed on the designer surface
        /// </summary>
        /// <param name="selection">The selected object in the active designer or conceptual model.</param>
        /// <remarks>
        /// Use this to keep a layer's tool window in step with the design surface. To drive the selection in the opposite
        /// direction, raise <see cref="ChangeEntityDesignerSelection" /> instead.
        /// </remarks>
        void OnSelectionChanged(XObject selection);

        #endregion

    }

}
