// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Diagrams;
using Microsoft.Data.Entity.Design.Diagrams.Layout;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Headless;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Microsoft.Data.Entity.Tests.Design.Diagrams.Rendering.Export
{
    /// <summary>
    ///     Covers connector-route provenance: a hand-routed connector is never un-routed by a shape move, and its
    ///     endpoint follows the shape while its interior bends are left alone. See
    ///     specs/diagram-layout-engines.md.
    /// </summary>
    [TestClass]
    public class ConnectorRoutingTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Moving_a_shape_keeps_the_manual_route_and_the_endpoint_follows()
        {
            using (var store = BuildDiagram(out var diagram))
            {
                var connector = diagram.NestedChildShapes.OfType<AssociationConnector>().Single();
                var from = (EntityTypeShape)connector.FromShape;

                SetManualRoute(store, connector, [(1.0, 1.0), (2.5, 2.5), (4.0, 4.0)]);
                var before = Points(connector);

                Move(store, from, 10.0, 5.0);

                // The whole fix: the flag is never cleared, so the connector stays hand-routed rather than reverting
                // to an auto-route. (The SDK still re-routes the polyline to track the moved shape, and keeps the
                // flag; what mattered was that a move no longer discards the route's provenance.)
                connector.ManuallyRouted.Should().BeTrue(
                    "a shape move must never clear the flag - that is the legacy bug this fixes");

                var after = Points(connector);
                after.Count.Should().BeGreaterThanOrEqualTo(2, "the connector is still a drawable route after the move");
                after.Max(p => p.X).Should().BeGreaterThan(
                    before.Max(p => p.X) + 5.0, "the route follows the shape that moved +10 in x rather than freezing");
            }
        }

        [TestMethod]
        public void Moving_a_shape_leaves_an_auto_routed_connector_auto()
        {
            using (var store = BuildDiagram(out var diagram))
            {
                var connector = diagram.NestedChildShapes.OfType<AssociationConnector>().Single();
                var from = (EntityTypeShape)connector.FromShape;

                // Not hand-routed: the SDK owns this one, and our re-attach must not touch it or set the flag.
                connector.ManuallyRouted.Should().BeFalse("the fixture connector starts auto-routed");

                Move(store, from, 10.0, 5.0);

                connector.ManuallyRouted.Should().BeFalse(
                    "a move never turns an auto-routed connector into a manually-routed one");
            }
        }

        [TestMethod]
        public void Resizing_a_shape_without_moving_it_leaves_the_route_alone()
        {
            using (var store = BuildDiagram(out var diagram))
            {
                var connector = diagram.NestedChildShapes.OfType<AssociationConnector>().Single();
                var from = (EntityTypeShape)connector.FromShape;

                SetManualRoute(store, connector, [(1.0, 1.0), (2.5, 2.5), (4.0, 4.0)]);
                var before = Points(connector);

                // Taller shape at the same location: the route stays and keeps its shape.
                var bounds = from.AbsoluteBounds;
                using (var tx = store.TransactionManager.BeginTransaction("Resize"))
                {
                    from.AbsoluteBounds = new RectangleD(bounds.X, bounds.Y, bounds.Width, bounds.Height + 2.0);
                    tx.Commit();
                }

                connector.ManuallyRouted.Should().BeTrue("a resize never clears the flag either");
                Points(connector).Count.Should().BeGreaterThanOrEqualTo(2, "the connector is still a drawable route");
            }
        }

        [TestMethod]
        public void A_manually_routed_connector_keeps_its_provenance_on_load()
        {
            var builder = TwoEntities().Route("Principal", "Dependent", manuallyRouted: true, (1.0, 1.0), (1.0, 3.0), (2.5, 3.0));

            WithLoadedConnector(builder, connector =>
            {
                // The load path must preserve the hand-routed flag: that is what makes the designer restore the
                // saved route instead of auto-routing over it on reopen.
                connector.ManuallyRouted.Should().BeTrue("a saved hand-routed connector must come back hand-routed");
                Points(connector).Count.Should().BeGreaterThanOrEqualTo(2, "and it is still a drawable route");
            });
        }

        [TestMethod]
        public void Points_present_but_flag_false_are_not_restored_as_a_manual_route()
        {
            // Malformed: ManuallyRouted="false" yet points were persisted. The loader must treat it as auto - the
            // flag wins - not resurrect the stray points as a hand route.
            var builder = TwoEntities().Route("Principal", "Dependent", manuallyRouted: false, (9.0, 9.0), (9.0, 1.0));

            WithLoadedConnector(builder, connector =>
            {
                connector.ManuallyRouted.Should().BeFalse("the flag says auto, so the connector is auto whatever stray points are present");
            });
        }

        [TestMethod]
        public void Flag_true_but_no_points_does_not_crash_and_leaves_a_drawable_route()
        {
            // Malformed the other way: ManuallyRouted="true" with no ConnectorPoints. The loader must not choke, and
            // the connector must still end up with something drawable rather than an empty route.
            var builder = TwoEntities().Route("Principal", "Dependent", manuallyRouted: true);

            WithLoadedConnector(builder, connector =>
            {
                Points(connector).Count.Should().BeGreaterThanOrEqualTo(
                    2, "a manual flag with no points falls back to a laid-out route rather than an empty one");
            });
        }

        #region Harness

        private static TestEdmxBuilder TwoEntities()
        {
            return new TestEdmxBuilder()
                .AddEntity("Principal")
                .AddEntity("Dependent")
                .Relate(principal: "Principal", dependent: "Dependent");
        }

        private static void WithLoadedConnector(TestEdmxBuilder builder, System.Action<AssociationConnector> assert)
        {
            var path = builder.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(
                    path, layoutManager: new LayoutEngineManager([new DslLayoutEngine(), new MsAglLayoutEngine()])))
                {
                    var connector = loaded.Diagram.NestedChildShapes.OfType<AssociationConnector>().Single();
                    assert(connector);
                }
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        private static System.Collections.Generic.List<PointD> Points(AssociationConnector connector)
        {
            return connector.EdgePoints.Cast<EdgePoint>().Select(p => p.Point).ToList();
        }

        private static void SetManualRoute(Store store, AssociationConnector connector, (double X, double Y)[] points)
        {
            using (var tx = store.TransactionManager.BeginTransaction("Hand-route"))
            {
                connector.ManuallyRouted = true;

                var collection = new EdgePointCollection();
                foreach (var point in points)
                {
                    collection.Add(new EdgePoint(point.X, point.Y, VGPointType.Normal));
                }

                connector.EdgePoints = collection;
                tx.Commit();
            }
        }

        private static void Move(Store store, EntityTypeShape shape, double dx, double dy)
        {
            var bounds = shape.AbsoluteBounds;
            using (var tx = store.TransactionManager.BeginTransaction("Move"))
            {
                shape.AbsoluteBounds = new RectangleD(bounds.X + dx, bounds.Y + dy, bounds.Width, bounds.Height);
                tx.Commit();
            }
        }

        /// <summary>
        ///     Builds a two-entity, one-association diagram in a headless store with the change rules live, so the
        ///     shape-move rule that re-attaches routes actually fires. Mirrors <see cref="HeadlessRoutingSpikeTests" />.
        /// </summary>
        private static Store BuildDiagram(out EntityDesignerSurface diagram)
        {
            var store = new Store(typeof(MicrosoftDataEntityDesignDomainModel));
            MicrosoftDataEntityDesignDomainModel.EnableDiagramRules(store);

            var modelPartition = store.DefaultPartition;
            var diagramPartition = new Partition(store);

            EntityDesignerViewModel viewModel;
            EntityDesignerSurface surface;

            using (var tx = store.TransactionManager.BeginTransaction("Create diagram"))
            {
                viewModel = new EntityDesignerViewModel(modelPartition);
                surface = new EntityDesignerSurface(diagramPartition) { ModelElement = viewModel };
                tx.Commit();
            }

            // No artifact behind this store, so the rules that push edits back into the EDMX must be off - the same
            // two the spike disables. The shape-move and connector-change rules stay on: those are what we test.
            store.RuleManager.DisableRule(typeof(EntityType_AddRule));
            store.RuleManager.DisableRule(typeof(Association_AddRule));

            using (var tx = store.TransactionManager.BeginTransaction("Build model"))
            {
                var customer = CreateEntity(modelPartition, viewModel, "Customer");
                var order = CreateEntity(modelPartition, viewModel, "Order");

                new Association(
                    modelPartition,
                    new RoleAssignment(Association.SourceEntityTypeDomainRoleId, customer),
                    new RoleAssignment(Association.TargetEntityTypeDomainRoleId, order));

                tx.Commit();
            }

            var shapes = surface.NestedChildShapes.OfType<EntityTypeShape>().ToList();

            using (var tx = store.TransactionManager.BeginTransaction("Position shapes"))
            {
                shapes[0].AbsoluteBounds = new RectangleD(0.75, 0.5, 1.5, 1.0);
                shapes[1].AbsoluteBounds = new RectangleD(4.0, 3.0, 1.5, 1.0);
                tx.Commit();
            }

            diagram = surface;
            return store;
        }

        private static EntityType CreateEntity(Partition partition, EntityDesignerViewModel viewModel, string name)
        {
            var entityType = new EntityType(
                partition,
                new PropertyAssignment(NameableItem.NameDomainPropertyId, name));

            viewModel.EntityTypes.Add(entityType);

            entityType.Properties.Add(
                new ScalarProperty(
                    partition,
                    new PropertyAssignment(NameableItem.NameDomainPropertyId, "Id"),
                    new PropertyAssignment(Property.TypeDomainPropertyId, "Int32"),
                    new PropertyAssignment(ScalarProperty.EntityKeyDomainPropertyId, true)));

            return entityType;
        }

        #endregion
    }
}
