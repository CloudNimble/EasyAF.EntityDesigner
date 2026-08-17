// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Dsl;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.Dsl.View
{
    /// <summary>
    ///     Tests for the transaction helpers on <see cref="EntityDesignerSurface" />.
    /// </summary>
    /// <remarks>
    ///     These cover the behaviour that is easy to lose when the transaction boilerplate is centralised: that a
    ///     transaction is tagged with the diagram id, and that work reporting no change is rolled back rather than
    ///     committed. Committing an unchanged transaction still pushes an entry onto the undo stack, which is why
    ///     the conditional overload exists.
    /// </remarks>
    [TestClass]
    public class EntityDesignerSurfaceTransactionTests
    {
        [TestMethod]
        public void InDiagramTransaction_commits_the_work()
        {
            using (var store = CreateStore())
            {
                var surface = CreateSurface(store);

                surface.InDiagramTransaction("Set title", () => surface.Title = "committed");

                surface.Title.Should().Be("committed");
            }
        }

        [TestMethod]
        public void InDiagramTransaction_commits_when_the_work_reports_a_change()
        {
            using (var store = CreateStore())
            {
                var surface = CreateSurface(store);

                surface.InDiagramTransaction(
                    "Set title",
                    () =>
                    {
                        surface.Title = "committed";

                        return true;
                    });

                surface.Title.Should().Be("committed");
            }
        }

        [TestMethod]
        public void InDiagramTransaction_rolls_back_when_the_work_reports_no_change()
        {
            using (var store = CreateStore())
            {
                var surface = CreateSurface(store);
                surface.InDiagramTransaction("Set title", () => surface.Title = "original");

                surface.InDiagramTransaction(
                    "Set title again",
                    () =>
                    {
                        surface.Title = "should be rolled back";

                        return false;
                    });

                surface.Title.Should().Be("original");
            }
        }

        [TestMethod]
        public void InDiagramTransaction_tags_the_transaction_with_the_diagram_id()
        {
            using (var store = CreateStore())
            {
                var surface = CreateSurface(store);
                object diagramId = null;

                surface.InDiagramTransaction(
                    "Inspect context",
                    () => store.TransactionManager.CurrentTransaction.Context.ContextInfo.TryGetValue(
                        EfiTransactionOriginator.TransactionOriginatorDiagramId, out diagramId));

                diagramId.Should().Be(TestDiagramId);
            }
        }

        [TestMethod]
        public void InDiagramTransaction_rejects_a_null_action()
        {
            using (var store = CreateStore())
            {
                var surface = CreateSurface(store);

                var act = () => surface.InDiagramTransaction("Nothing", (Action)null);

                act.Should().Throw<ArgumentNullException>();
            }
        }

        /// <summary>
        ///     A recognisable diagram id, so the tag assertion cannot pass on an empty default.
        /// </summary>
        private const string TestDiagramId = "test-diagram-id";

        /// <summary>
        ///     Creates a store with the Entity Designer domain model loaded.
        /// </summary>
        /// <remarks>
        ///     Safe without synchronization only because this assembly sets <c>UsesDslStore</c> and is therefore
        ///     built with <c>[assembly: DoNotParallelize]</c>. See Directory.Build.props for why the Modeling SDK
        ///     cannot be driven from more than one thread in a process.
        /// </remarks>
        private static Store CreateStore()
        {
            return new Store(typeof(MicrosoftDataEntityDesignDomainModel));
        }

        /// <summary>
        ///     Creates a surface in its own partition, matching how the serialization helper builds one.
        /// </summary>
        private static EntityDesignerSurface CreateSurface(Store store)
        {
            EntityDesignerSurface surface = null;

            using (var transaction = store.TransactionManager.BeginTransaction("Create surface", true))
            {
                surface = new EntityDesignerSurface(new Partition(store)) { DiagramId = TestDiagramId };
                transaction.Commit();
            }

            return surface;
        }
    }
}
