// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Dsl;
using Microsoft.Data.Entity.Design.Dsl.Rules;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Dsl.ViewModel;
using Microsoft.VisualStudio.Modeling;

namespace Microsoft.Data.Entity.Design.Renderer.Headless
{
    /// <summary>
    ///     Hosts a DSL <see cref="Store" /> configured to build and route Entity Designer diagrams with no Visual
    ///     Studio shell, no document window and no <c>DiagramClientView</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In Visual Studio the doc data performs this setup. Reproducing it outside the shell needs four things
    ///         that are easy to miss, because getting any of them wrong produces an empty diagram rather than an error:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             All eight diagram rules ship <c>InitiallyDisabled</c>, so <c>EnableDiagramRules</c> must be called
    ///             before any diagram data enters the store. Without it nothing creates shapes.
    ///         </item>
    ///         <item>
    ///             <see cref="EntityType_AddRule" /> and <see cref="Association_AddRule" /> push view model edits back
    ///             into the EDMX through the editing context. That round trip is not wanted here, so they are disabled.
    ///         </item>
    ///         <item>
    ///             The diagram lives in its own <see cref="Partition" />, matching <c>CreateDiagramHelper</c>.
    ///         </item>
    ///         <item>
    ///             Transactions must not be opened as serializing. That flag suppresses the fixup rules that create
    ///             shapes and connectors.
    ///         </item>
    ///     </list>
    /// </remarks>
    internal sealed class HeadlessDiagramStore : IDisposable
    {
        /// <summary>
        ///     The rules that translate view model edits back into the EDMX.
        /// </summary>
        /// <remarks>
        ///     Every one of these routes through <c>ViewModelChangeContext</c> or a command processor to mutate the
        ///     underlying model. Rendering only reads, and there is no document to write back to, so leaving them on
        ///     produces failures that have nothing to do with drawing: additions are silently discarded, and
        ///     <c>EntityType_ChangeRule</c> throws a name conflict when the translator assigns an entity the name it
        ///     already has in the model.
        /// </remarks>
        private static readonly Type[] RoundTripRuleTypes =
        [
            typeof(AssociationConnector_AddRule),
            typeof(AssociationConnector_ChangeRule),
            typeof(AssociationConnector_DeleteRule),
            typeof(Association_AddRule),
            typeof(EntityTypeShape_AddRule),
            typeof(EntityTypeShape_ChangeRule),
            typeof(EntityTypeShape_DeleteRule),
            typeof(EntityType_AddRule),
            typeof(EntityType_ChangeRule),
            typeof(InheritanceConnector_AddRule),
            typeof(InheritanceConnector_ChangeRule),
            typeof(InheritanceConnector_DeleteRule),
            typeof(Inheritance_AddRule),
            typeof(Inheritance_DeleteRule),
            typeof(NavigationProperty_AddRule),
            typeof(NavigationProperty_ChangeRule),
            typeof(Property_AddRule),
            typeof(Property_ChangeRule),
            typeof(ScalarProperty_ChangeRule)
        ];

        private bool _isDisposed;

        /// <summary>
        ///     Gets the diagram shapes and connectors are created on, or null until <see cref="AttachDiagram" /> runs.
        /// </summary>
        public EntityDesignerSurface Diagram { get; private set; }

        /// <summary>
        ///     Gets the partition holding the diagram and its shapes.
        /// </summary>
        public Partition DiagramPartition { get; }

        /// <summary>
        ///     Gets the partition holding the view model elements.
        /// </summary>
        public Partition ModelPartition { get; }

        /// <summary>
        ///     Gets the store backing the diagram.
        /// </summary>
        public Store Store { get; }

        /// <summary>
        ///     Creates a store with the diagram rules configured for headless use.
        /// </summary>
        public HeadlessDiagramStore()
        {
            Store = new Store(typeof(MicrosoftDataEntityDesignDomainModel));

            // Must happen before any diagram data is loaded - see the remarks on this class.
            MicrosoftDataEntityDesignDomainModel.EnableDiagramRules(Store);

            foreach (var ruleType in RoundTripRuleTypes)
            {
                Store.RuleManager.DisableRule(ruleType);
            }

            ModelPartition = Store.DefaultPartition;
            DiagramPartition = new Partition(Store);
        }

        /// <summary>
        ///     Creates the diagram and binds it to <paramref name="viewModel" />.
        /// </summary>
        /// <param name="viewModel">The view model the diagram presents.</param>
        /// <returns>The diagram that was created.</returns>
        public EntityDesignerSurface AttachDiagram(EntityDesignerViewModel viewModel)
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            Transact(
                "Create diagram",
                () => Diagram = new EntityDesignerSurface(DiagramPartition) { ModelElement = viewModel });

            return Diagram;
        }

        /// <summary>
        ///     Runs <paramref name="action" /> inside a non-serializing transaction, so diagram fixup can run.
        /// </summary>
        /// <param name="name">The transaction name.</param>
        /// <param name="action">The work to perform.</param>
        public void Transact(string name, Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            using (var transaction = Store.TransactionManager.BeginTransaction(name))
            {
                action();
                transaction.Commit();
            }
        }

        /// <summary>
        ///     Disposes the underlying store.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Store?.Dispose();
        }
    }
}
