// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Diagrams.Layout;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Headless;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.Diagrams.Rendering.Export
{
    /// <summary>
    ///     Covers <see cref="MsAglLayoutEngine" /> placing shapes and routing connectors with no Visual Studio
    ///     present.
    /// </summary>
    [TestClass]
    public class MsAglLayoutEngineTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Layout_places_every_shape_without_overlapping_any_other()
        {
            var edmxPath = TestEdmx.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(edmxPath, layoutManager: CreateManager()))
                {
                    var shapes = loaded.Diagram.NestedChildShapes.OfType<EntityTypeShape>().ToList();
                    shapes.Should().HaveCountGreaterThanOrEqualTo(2, "the model needs at least two shapes to overlap");

                    loaded.Diagram.AutoLayoutDiagram();

                    foreach (var shape in shapes)
                    {
                        var bounds = shape.AbsoluteBounds;
                        TestContext.WriteLine(
                            $"  {shape.Name}: {bounds.X:F3},{bounds.Y:F3} {bounds.Width:F3}x{bounds.Height:F3}");

                        bounds.Width.Should().BeGreaterThan(0, "layout must not collapse a shape");
                        bounds.Height.Should().BeGreaterThan(0, "layout must not collapse a shape");
                    }

                    foreach (var pair in Pairs(shapes))
                    {
                        Overlaps(pair.Item1.AbsoluteBounds, pair.Item2.AbsoluteBounds).Should().BeFalse(
                            $"'{pair.Item1.Name}' and '{pair.Item2.Name}' must not be placed on top of each other");
                    }
                }
            }
            finally
            {
                File.Delete(edmxPath);
            }
        }

        [TestMethod]
        public void Layout_routes_every_connector_and_marks_it_manually_routed()
        {
            var edmxPath = TestEdmx.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(edmxPath, layoutManager: CreateManager()))
                {
                    var connectors = loaded.Diagram.NestedChildShapes.OfType<BinaryLinkShape>().ToList();
                    connectors.Should().NotBeEmpty("the model has at least one association");

                    loaded.Diagram.AutoLayoutDiagram();

                    foreach (var connector in connectors)
                    {
                        var points = connector.EdgePoints;
                        TestContext.WriteLine($"  connector: {points?.Count.ToString() ?? "<null>"} points");

                        connector.ManuallyRouted.Should().BeTrue(
                            "the engine persists its own routes rather than leaving them to the Modeling SDK");
                        points.Should().NotBeNull();
                        points.Count.Should().BeGreaterThanOrEqualTo(2, "a route needs a start and an end");
                    }
                }
            }
            finally
            {
                File.Delete(edmxPath);
            }
        }

        [TestMethod]
        public void Layout_moves_shapes_off_the_positions_the_edmx_supplied()
        {
            var edmxPath = TestEdmx.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(edmxPath, layoutManager: CreateManager()))
                {
                    var shapes = loaded.Diagram.NestedChildShapes.OfType<EntityTypeShape>().ToList();
                    var before = shapes.Select(s => s.AbsoluteBounds.Location).ToList();

                    loaded.Diagram.AutoLayoutDiagram();

                    var after = shapes.Select(s => s.AbsoluteBounds.Location).ToList();

                    after.Should().NotEqual(before, "the engine must actually place the shapes, not leave the file's positions");
                }
            }
            finally
            {
                File.Delete(edmxPath);
            }
        }

        [TestMethod]
        public void RouteConnectors_redraws_the_connector_without_moving_any_shape()
        {
            var edmxPath = TestEdmx.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(edmxPath, layoutManager: CreateManager()))
                {
                    var shapes = loaded.Diagram.NestedChildShapes.OfType<EntityTypeShape>().ToList();
                    var connector = loaded.Diagram.NestedChildShapes.OfType<BinaryLinkShape>().First();

                    loaded.Diagram.AutoLayoutDiagram();

                    // Clear the route first, so points coming back proves the redraw actually ran rather than the
                    // call being dropped by the laying-out guard.
                    ClearRoute(connector);
                    var positionsBefore = shapes.Select(s => s.AbsoluteBounds).ToList();

                    loaded.Diagram.RerouteConnectors(new[] { connector });

                    connector.EdgePoints.Should().NotBeNull();
                    connector.EdgePoints.Count.Should().BeGreaterThanOrEqualTo(
                        2, "the redraw must produce a drawable route for the connector");

                    var positionsAfter = shapes.Select(s => s.AbsoluteBounds).ToList();
                    positionsAfter.Should().Equal(
                        positionsBefore, "a redraw re-routes only the connector and must never move a shape");
                }
            }
            finally
            {
                File.Delete(edmxPath);
            }
        }

        [TestMethod]
        public void RouteConnectors_leaves_the_manually_routed_flag_exactly_as_it_found_it()
        {
            var edmxPath = TestEdmx.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(edmxPath, layoutManager: CreateManager()))
                {
                    var connector = loaded.Diagram.NestedChildShapes.OfType<BinaryLinkShape>().First();

                    loaded.Diagram.AutoLayoutDiagram();

                    // A redraw repaints the route; whose route it is - human or engine - is not its call to change.
                    foreach (var flag in new[] { false, true })
                    {
                        SetManuallyRouted(connector, flag);
                        ClearRoute(connector);

                        loaded.Diagram.RerouteConnectors(new[] { connector });

                        connector.EdgePoints.Count.Should().BeGreaterThanOrEqualTo(2, "the redraw ran");
                        connector.ManuallyRouted.Should().Be(flag, "a redraw must never touch the provenance flag");
                    }
                }
            }
            finally
            {
                File.Delete(edmxPath);
            }
        }

        /// <summary>
        ///     A manager whose only engine is the MSAGL one, so <c>AutoLayoutDiagram</c> exercises it.
        /// </summary>
        private static LayoutEngineManager CreateManager()
        {
            return new LayoutEngineManager([new MsAglLayoutEngine()]);
        }

        /// <summary>
        ///     Sets a connector's <c>ManuallyRouted</c> flag inside its own store transaction.
        /// </summary>
        private static void SetManuallyRouted(BinaryLinkShape connector, bool value)
        {
            using (var tx = connector.Store.TransactionManager.BeginTransaction("Set ManuallyRouted"))
            {
                connector.ManuallyRouted = value;
                tx.Commit();
            }
        }

        /// <summary>
        ///     Empties a connector's route inside its own store transaction, so a later redraw has to rebuild it.
        /// </summary>
        private static void ClearRoute(BinaryLinkShape connector)
        {
            using (var tx = connector.Store.TransactionManager.BeginTransaction("Clear route"))
            {
                connector.EdgePoints = new EdgePointCollection();
                tx.Commit();
            }
        }

        /// <summary>
        ///     Whether two shape rectangles share any area.
        /// </summary>
        private static bool Overlaps(RectangleD first, RectangleD second)
        {
            return first.Left < second.Right
                && second.Left < first.Right
                && first.Top < second.Bottom
                && second.Top < first.Bottom;
        }

        /// <summary>
        ///     Every unordered pair drawn from <paramref name="shapes" />.
        /// </summary>
        private static IEnumerable<(EntityTypeShape, EntityTypeShape)> Pairs(IReadOnlyList<EntityTypeShape> shapes)
        {
            for (var i = 0; i < shapes.Count; i++)
            {
                for (var j = i + 1; j < shapes.Count; j++)
                {
                    yield return (shapes[i], shapes[j]);
                }
            }
        }
    }
}
