// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Dsl;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Dsl.ViewModel;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.Dsl.View.Export
{
    /// <summary>
    ///     Determines whether the DSL SDK will lay out and route a diagram with no Visual Studio shell and no
    ///     DiagramClientView.
    /// </summary>
    /// <remarks>
    ///     This is the gating question for rendering an EDMX to SVG or PNG headlessly. SvgConnectorRenderer consumes
    ///     BinaryLinkShape.EdgePoints - it traces a polyline that DSL has already routed, and never routes anything
    ///     itself. If DSL cannot produce EdgePoints outside the designer then a headless renderer has to route
    ///     connectors on its own, which is a substantially different piece of work.
    /// </remarks>
    [TestClass]
    public class HeadlessRoutingSpikeTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Dsl_creates_shapes_and_routes_connectors_without_a_shell()
        {
            var store = new Store(typeof(MicrosoftDataEntityDesignDomainModel));

            // All eight diagram rules ship InitiallyDisabled. The generated helper turns them on and its own doc
            // comment says it must be called before diagram data is loaded into the store.
            MicrosoftDataEntityDesignDomainModel.EnableDiagramRules(store);

            var modelPartition = store.DefaultPartition;
            var diagramPartition = new Partition(store);

            EntityDesignerViewModel viewModel;
            EntityDesignerSurface diagram;
            EntityType customer;
            EntityType order;

            // Deliberately NOT the (name, true) overload used by DslTestHelper - that second argument is
            // isSerializing, which suppresses the FixUpDiagram rules that create shapes and connectors.
            using (var tx = store.TransactionManager.BeginTransaction("Create diagram"))
            {
                viewModel = new EntityDesignerViewModel(modelPartition);

                diagram = new EntityDesignerSurface(diagramPartition)
                {
                    ModelElement = viewModel
                };

                tx.Commit();
            }

            // These push view model edits back into the EDMX through ViewModelChangeContext. There is no artifact
            // behind this store, so they must be off - the product disables them the same way in
            // EntityDesignerSurface when it is applying changes that came from the model side.
            store.RuleManager.DisableRule(typeof(global::Microsoft.Data.Entity.Design.Dsl.Rules.EntityType_AddRule));
            store.RuleManager.DisableRule(typeof(global::Microsoft.Data.Entity.Design.Dsl.Rules.Association_AddRule));

            // Separate transaction so the diagram is already committed and discoverable when FixUpDiagram runs.
            using (var tx = store.TransactionManager.BeginTransaction("Build model"))
            {
                customer = CreateEntity(modelPartition, viewModel, "Customer");
                order = CreateEntity(modelPartition, viewModel, "Order");

                new Association(
                    modelPartition,
                    new RoleAssignment(Association.SourceEntityTypeDomainRoleId, customer),
                    new RoleAssignment(Association.TargetEntityTypeDomainRoleId, order));

                tx.Commit();
            }

            viewModel.EntityTypes.Should().HaveCount(
                2, "the entities must actually attach to the view model or FixUpDiagram finds no parent and does nothing");

            var shapes = diagram.NestedChildShapes.OfType<EntityTypeShape>().ToList();
            var connectors = diagram.NestedChildShapes.OfType<AssociationConnector>().ToList();

            TestContext.WriteLine($"Shapes: {shapes.Count}, Connectors: {connectors.Count}");

            shapes.Should().HaveCount(2, "FixUpDiagram should create a shape per entity when the diagram exists");
            connectors.Should().HaveCount(1, "FixUpDiagram should create a connector for the association");

            // Place the shapes the way the EDMX Diagrams section would, then let DSL route between them.
            using (var tx = store.TransactionManager.BeginTransaction("Position shapes"))
            {
                shapes[0].AbsoluteBounds = new RectangleD(0.75, 0.5, 1.5, 1.0);
                shapes[1].AbsoluteBounds = new RectangleD(4.0, 3.0, 1.5, 1.0);
                tx.Commit();
            }

            var connector = connectors[0];

            using (var tx = store.TransactionManager.BeginTransaction("Route"))
            {
                connector.RecalculateRoute();
                tx.Commit();
            }

            var edgePoints = connector.EdgePoints;
            TestContext.WriteLine($"EdgePoints: {edgePoints?.Count.ToString() ?? "<null>"}");

            if (edgePoints != null)
            {
                foreach (EdgePoint point in edgePoints)
                {
                    TestContext.WriteLine($"  ({point.Point.X:F4}, {point.Point.Y:F4})");
                }
            }

            edgePoints.Should().NotBeNull();
            edgePoints.Count.Should().BeGreaterThanOrEqualTo(
                2, "a routed connector needs at least a start and an end point for SvgConnectorRenderer to trace");

            // A route that ignored the shapes would collapse to a single location.
            var first = edgePoints[0].Point;
            var last = edgePoints[edgePoints.Count - 1].Point;
            (Math.Abs(first.X - last.X) + Math.Abs(first.Y - last.Y)).Should().BeGreaterThan(
                0.1, "the endpoints should track the shapes that were placed apart from each other");
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
    }
}
