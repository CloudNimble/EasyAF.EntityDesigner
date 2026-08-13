// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Renderer.Headless;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.Renderer.Export
{
    /// <summary>
    ///     Covers loading an EDMX into a laid out, routed diagram with no Visual Studio present.
    /// </summary>
    [TestClass]
    public class EdmxDiagramLoaderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Load_produces_a_diagram_with_positioned_shapes_and_routed_connectors()
        {
            var edmxPath = TestEdmx.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(edmxPath))
                {
                    var shapes = loaded.Diagram.NestedChildShapes.OfType<EntityTypeShape>().ToList();
                    var connectors = loaded.Diagram.NestedChildShapes.OfType<AssociationConnector>().ToList();

                    TestContext.WriteLine($"Diagram '{loaded.DiagramName}': {shapes.Count} shapes, {connectors.Count} connectors");

                    foreach (var shape in shapes)
                    {
                        TestContext.WriteLine(
                            $"  shape {shape.AbsoluteBounds.X:F3},{shape.AbsoluteBounds.Y:F3} "
                            + $"{shape.AbsoluteBounds.Width:F3}x{shape.AbsoluteBounds.Height:F3}");
                    }

                    shapes.Should().HaveCount(2, "the test model has two entities on the diagram");
                    connectors.Should().HaveCount(1, "the test model has one association");

                    // Positions come from the EDMX Diagrams section, not from auto layout.
                    shapes.Select(s => s.AbsoluteBounds.X).Should().Contain(0.75);
                    shapes.Select(s => s.AbsoluteBounds.X).Should().Contain(3.5);

                    var edgePoints = connectors[0].EdgePoints;
                    TestContext.WriteLine($"  edgePoints: {edgePoints?.Count}");

                    edgePoints.Should().NotBeNull();
                    edgePoints.Count.Should().BeGreaterThanOrEqualTo(
                        2, "the connector must be routed for SvgConnectorRenderer to trace it");
                }
            }
            finally
            {
                File.Delete(edmxPath);
            }
        }

        [TestMethod]
        public void Load_renders_a_model_whose_database_provider_is_not_installed()
        {
            // The SSDL names a provider that is not registered on the build agent. Rendering must not require one:
            // it needs a conceptual model and a diagram, not a working database connection.
            var edmxPath = TestEdmx.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(edmxPath))
                {
                    loaded.Diagram.NestedChildShapes.OfType<EntityTypeShape>().Should().NotBeEmpty();
                }
            }
            finally
            {
                File.Delete(edmxPath);
            }
        }
    }
}
