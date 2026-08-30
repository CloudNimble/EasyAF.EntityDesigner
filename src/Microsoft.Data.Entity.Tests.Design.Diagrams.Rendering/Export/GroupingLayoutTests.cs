// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Diagrams.Layout;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Headless;
using Microsoft.Data.Entity.Design.Diagrams.View;
using ConnectorMode = Microsoft.Data.Entity.Design.Edmx.Designer.ConnectorMode;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EntityTypeShape = Microsoft.Data.Entity.Design.Diagrams.View.EntityTypeShape;

namespace Microsoft.Data.Entity.Tests.Design.Diagrams.Rendering.Export
{
    /// <summary>
    ///     Covers <see cref="GroupingLayout" /> choosing a strategy and assigning group names.
    /// </summary>
    [TestClass]
    public class GroupingLayoutTests
    {
        public TestContext TestContext { get; set; }

        private static readonly Color Content = Color.FromArgb(0x33, 0x99, 0xFF);
        private static readonly Color Media = Color.FromArgb(0x00, 0x99, 0x66);

        [TestMethod]
        public void Detect_returns_Explicit_when_any_shape_names_a_group()
        {
            var builder = MediaModel().Shape("Post", groupName: "Publishing");

            WithShapes(builder, shapes =>
            {
                var strategy = GroupingLayout.Detect(GroupingLayout.BuildEntities(shapes));

                strategy.Should().Be(
                    GroupingStrategy.Explicit, "a name someone typed outranks anything inferred from the schema");
            });
        }

        [TestMethod]
        public void Detect_returns_FillColor_when_enough_shapes_carry_distinct_colours()
        {
            WithShapes(ColouredMediaModel(), shapes =>
            {
                var strategy = GroupingLayout.Detect(GroupingLayout.BuildEntities(shapes));

                strategy.Should().Be(GroupingStrategy.FillColor);
            });
        }

        [TestMethod]
        public void Detect_ignores_a_single_colour_as_a_highlight()
        {
            var builder = MediaModel()
                .Shape("Post", Content)
                .Shape("Comment", Content)
                .Shape("Show", Content)
                .Shape("Episode", Content);

            WithShapes(builder, shapes =>
            {
                var strategy = GroupingLayout.Detect(GroupingLayout.BuildEntities(shapes));

                strategy.Should().NotBe(
                    GroupingStrategy.FillColor, "one colour against the default says 'look at these', not 'these are the groups'");
            });
        }

        [TestMethod]
        public void Group_gives_every_shape_a_name()
        {
            WithShapes(ColouredMediaModel(), shapes =>
            {
                var groups = GroupingLayout.Group(shapes);

                groups.Should().HaveCount(shapes.Count);

                foreach (var shape in shapes)
                {
                    groups[shape].Should().NotBeNullOrWhiteSpace($"'{shape.Name}' has to be placed somewhere");
                }
            });
        }

        [TestMethod]
        public void Group_names_a_colour_group_after_the_entity_it_centres_on()
        {
            WithShapes(ColouredMediaModel(), shapes =>
            {
                var groups = GroupingLayout.Group(shapes);

                foreach (var name in groups.Values.Distinct().OrderBy(name => name, StringComparer.Ordinal))
                {
                    TestContext.WriteLine($"  group: {name}");
                }

                groups.Values.Should().OnlyContain(
                    name => shapes.Any(shape => shape.TypedModelElement.Name == name),
                    "every group name has to be an entity the reader can go and look at");
            });
        }

        [TestMethod]
        public void Group_names_a_colour_group_after_its_most_connected_member_not_its_busiest()
        {
            // Zebra holds the group together: three of its four members hang off it. Alpha has more
            // associations than Zebra does, but three of them leave the group, so it says nothing about what
            // this group is. Alpha is also alphabetically first, so neither of the tie-breaks can produce the
            // right answer by accident.
            WithShapes(SplitAffinityModel(), shapes =>
            {
                var groups = GroupingLayout.Group(shapes);

                foreach (var pair in groups.OrderBy(pair => pair.Value, StringComparer.Ordinal))
                {
                    TestContext.WriteLine($"  {pair.Key.TypedModelElement.Name} -> {pair.Value}");
                }

                groups[Find(shapes, "Alpha")].Should().Be(
                    "Zebra", "the name should be the table the others centre on, counted inside the group");
            });
        }

        [TestMethod]
        public void Group_places_a_reference_table_with_the_entity_that_uses_it()
        {
            // PostType is connected to three entities. Two of them are in the media group, so grouping by
            // company alone would put it there; only Post owns it, through a * to 1 association and a
            // PostTypeId property. The reference-table pass is the only thing that can tell those apart.
            WithShapes(ColouredMediaModel(), shapes =>
            {
                var groups = GroupingLayout.Group(shapes);

                foreach (var pair in groups.OrderBy(pair => pair.Value, StringComparer.Ordinal))
                {
                    TestContext.WriteLine($"  {pair.Key.Name} -> {pair.Value}");
                }

                var postType = Find(shapes, "PostType");
                var post = Find(shapes, "Post");
                var show = Find(shapes, "Show");

                groups[postType].Should().Be(
                    groups[post], "a lookup table belongs beside whatever reads it");
                groups[postType].Should().NotBe(
                    groups[show], "the two media associations are not ownership, so they must not win");
            });
        }

        [TestMethod]
        public void Layout_records_the_groups_it_detected_on_shapes_that_had_none()
        {
            WithLoaded(ColouredMediaModel().Grouping(enable: true, generateNames: true), loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();

                foreach (var shape in Shapes(loaded))
                {
                    shape.ModelShape.GroupName.Value.Should().NotBeNullOrWhiteSpace(
                        $"the guess for '{shape.Name}' has to reach the file for anyone to correct it");
                }
            });
        }

        [TestMethod]
        public void Grouping_off_generates_no_names_even_with_generation_on()
        {
            // Rule: generation is subordinate to grouping. With grouping off there is nothing to generate for,
            // whatever the generate flag says. See specs/diagram-layout-engines.md.
            WithLoaded(ColouredMediaModel().Grouping(enable: false, generateNames: true), loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();

                foreach (var shape in Shapes(loaded))
                {
                    shape.ModelShape.GroupName.Value.Should().BeNullOrWhiteSpace(
                        $"'{shape.Name}' must keep no group while grouping is off");
                }
            });
        }

        [TestMethod]
        public void Grouping_on_with_generation_off_writes_no_names()
        {
            // Rule: grouping on, generation off clusters by names already in the file and invents none.
            var builder = ColouredMediaModel()
                .Shape("Post", groupName: "Publishing")
                .Grouping(enable: true, generateNames: false);

            WithLoaded(builder, loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();

                Find(Shapes(loaded), "Post").ModelShape.GroupName.Value.Should().Be(
                    "Publishing", "a name already in the file is what the clustering uses");

                foreach (var shape in Shapes(loaded).Where(shape => shape.TypedModelElement.Name != "Post"))
                {
                    shape.ModelShape.GroupName.Value.Should().BeNullOrWhiteSpace(
                        $"'{shape.TypedModelElement.Name}' had no group and generation is off, so none should be written");
                }
            });
        }

        [TestMethod]
        public void Group_leaves_a_reference_table_where_the_file_puts_it()
        {
            // PostType is the one entity the reference-table pass would otherwise move, so naming its group is
            // the case where a detected group and a stored one genuinely disagree.
            var builder = ColouredMediaModel().Shape("PostType", groupName: "Taxonomy");

            WithShapes(builder, shapes =>
            {
                var groups = GroupingLayout.Group(shapes);

                groups[Find(shapes, "PostType")].Should().Be(
                    "Taxonomy", "a guess about where a lookup table is most useful must not overrule an instruction");
            });
        }

        [TestMethod]
        public void Layout_leaves_a_group_name_already_in_the_file_alone()
        {
            var builder = ColouredMediaModel()
                .Shape("PostType", groupName: "Taxonomy")
                .Grouping(enable: true, generateNames: true);

            WithLoaded(builder, loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();

                Find(Shapes(loaded), "PostType").ModelShape.GroupName.Value.Should().Be(
                    "Taxonomy", "a name the user set is what every later layout has to honour");
            });
        }

        [TestMethod]
        public void ClearGroupNames_drops_the_names_in_the_file_including_ones_the_user_set()
        {
            // Grouping and generation both on, so clearing re-derives - the case where the reset has to be shown
            // to override even names the user set.
            var builder = ColouredMediaModel()
                .Shape("PostType", groupName: "Taxonomy")
                .Grouping(enable: true, generateNames: true);

            WithLoaded(builder, loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();
                Shapes(loaded).Should().Contain(
                    shape => shape.ModelShape.GroupName.Value == "Taxonomy", "the model starts with a name in it");

                loaded.Diagram.ClearGroupNames();

                // Clearing is a change to a layout input, so the diagram arranges again on the way out and the
                // grouping is detected from scratch. The names come back - freshly derived, and no longer the
                // ones that were there. That is what a reset means here; there is no state where the file has
                // no groups in it and stays that way.
                foreach (var shape in Shapes(loaded))
                {
                    TestContext.WriteLine($"  {shape.TypedModelElement.Name} -> {shape.ModelShape.GroupName.Value}");
                }

                Shapes(loaded).Should().NotContain(
                    shape => shape.ModelShape.GroupName.Value == "Taxonomy",
                    "a reset that left the user's names behind would not be a reset");
            });
        }

        [TestMethod]
        public void Clearing_group_names_with_generation_off_leaves_them_cleared()
        {
            // Rule: clearing re-lays out, but with generation off nothing comes back. There is a stable state
            // here where the file has no group names and keeps it. See specs/diagram-layout-engines.md.
            var builder = ColouredMediaModel()
                .Shape("PostType", groupName: "Taxonomy")
                .Grouping(enable: true, generateNames: false);

            WithLoaded(builder, loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();

                loaded.Diagram.ClearGroupNames();

                foreach (var shape in Shapes(loaded))
                {
                    shape.ModelShape.GroupName.Value.Should().BeNullOrWhiteSpace(
                        $"'{shape.TypedModelElement.Name}' must stay cleared while generation is off");
                }
            });
        }

        [TestMethod]
        public void Changing_a_group_name_rearranges_the_diagram_on_its_own()
        {
            // No AutoLayoutDiagram call anywhere in this test. Editing the attribute is the whole trigger, which
            // is what the property window does and what was missing.
            WithLoaded(ColouredMediaModel().Grouping(enable: true, generateNames: true), loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();

                var shapes = Shapes(loaded);
                var before = shapes.ToDictionary(shape => shape, shape => shape.AbsoluteBounds.Location);

                Edit(loaded, Find(shapes, "Comment").ModelShape.GroupName, "Moderation");

                shapes.Select(shape => shape.AbsoluteBounds.Location)
                    .Should().NotEqual(
                        shapes.Select(shape => before[shape]),
                        "a layout setting the user can change but cannot see the effect of is worse than no setting");
            });
        }

        [TestMethod]
        public void Changing_the_connector_mode_rearranges_the_diagram_on_its_own()
        {
            WithLoaded(ColouredMediaModel(), loaded =>
            {
                loaded.Diagram.AutoLayoutDiagram();

                var connector = loaded.Diagram.NestedChildShapes.OfType<BinaryLinkShape>().First();
                var before = connector.EdgePoints.Cast<EdgePoint>().Select(point => point.Point).ToList();

                Edit(loaded, loaded.Diagram.ModelDiagram.ConnectorMode, ConnectorMode.Straight);

                var after = connector.EdgePoints.Cast<EdgePoint>().Select(point => point.Point).ToList();

                after.Should().NotEqual(before, "straight lines are not the orthogonal route that was there before");
            });
        }

        /// <summary>
        ///     Writes a value the way the property window does, through the model rather than the diagram.
        /// </summary>
        private static void Edit<T>(LoadedDiagram loaded, DefaultableValue<T> attribute, T value)
        {
            var cpc = new CommandProcessorContext(
                loaded.Diagram.ModelElement.EditingContext,
                EfiTransactionOriginator.PropertyWindowOriginatorId,
                "Test edit");

            CommandProcessor.InvokeSingleCommand(cpc, new UpdateDefaultableValueCommand<T>(attribute, value));
        }

        /// <summary>
        ///     A small media model: two subsystems, a lookup table, and a tenant root everything points at.
        /// </summary>
        /// <remarks>
        ///     PostType is deliberately attached to the media entities as a dependent, so it has neighbours
        ///     there without being owned by them.
        /// </remarks>
        private static TestEdmxBuilder MediaModel()
        {
            return new TestEdmxBuilder()
                .AddEntity("Brand")
                .AddEntity("Post", "Title")
                .AddEntity("Comment", "Body")
                .AddEntity("Show", "Name")
                .AddEntity("Episode", "Number")
                .AddEntity("PostType")
                .Relate(principal: "Brand", dependent: "Post")
                .Relate(principal: "Brand", dependent: "Show")
                .Relate(principal: "Brand", dependent: "Episode")
                .Relate(principal: "Brand", dependent: "Comment")
                .Relate(principal: "Post", dependent: "Comment")
                .Relate(principal: "Show", dependent: "Episode")
                .Relate(principal: "PostType", dependent: "Post")
                .Relate(principal: "Show", dependent: "PostType")
                .Relate(principal: "Episode", dependent: "PostType");
        }

        /// <summary>
        ///     Two colour groups where one member's associations mostly leave its own group.
        /// </summary>
        /// <remarks>
        ///     Nine entities so that Alpha's four associations stay under the tenant-root threshold; at eight it
        ///     would be treated as one and dropped out of everybody's evidence, which is a different test.
        /// </remarks>
        private static TestEdmxBuilder SplitAffinityModel()
        {
            return new TestEdmxBuilder()
                .AddEntity("Zebra")
                .AddEntity("Alpha")
                .AddEntity("Beta")
                .AddEntity("Gamma")
                .AddEntity("Widget")
                .AddEntity("Gadget")
                .AddEntity("Doohickey")
                .AddEntity("Thing")
                .AddEntity("Item")
                .Relate(principal: "Zebra", dependent: "Alpha")
                .Relate(principal: "Zebra", dependent: "Beta")
                .Relate(principal: "Zebra", dependent: "Gamma")
                .Relate(principal: "Alpha", dependent: "Widget")
                .Relate(principal: "Alpha", dependent: "Gadget")
                .Relate(principal: "Alpha", dependent: "Doohickey")
                .Relate(principal: "Widget", dependent: "Thing")
                .Relate(principal: "Widget", dependent: "Item")
                .Shape("Zebra", Content)
                .Shape("Alpha", Content)
                .Shape("Beta", Content)
                .Shape("Gamma", Content)
                .Shape("Widget", Media)
                .Shape("Gadget", Media)
                .Shape("Doohickey", Media)
                .Shape("Thing", Media)
                .Shape("Item", Media);
        }

        /// <summary>
        ///     <see cref="MediaModel" /> with the two subsystems coloured and the lookup table left default.
        /// </summary>
        private static TestEdmxBuilder ColouredMediaModel()
        {
            return MediaModel()
                .Shape("Post", Content)
                .Shape("Comment", Content)
                .Shape("Show", Media)
                .Shape("Episode", Media);
        }

        private static EntityTypeShape Find(IEnumerable<EntityTypeShape> shapes, string name)
        {
            return shapes.Single(shape => shape.TypedModelElement.Name == name);
        }

        private static List<EntityTypeShape> Shapes(LoadedDiagram loaded)
        {
            return loaded.Diagram.NestedChildShapes.OfType<EntityTypeShape>().ToList();
        }

        /// <summary>
        ///     Writes the model, loads it, hands the diagram to <paramref name="assert" /> and cleans up.
        /// </summary>
        private static void WithLoaded(TestEdmxBuilder builder, Action<LoadedDiagram> assert)
        {
            var path = builder.Write();

            try
            {
                using (var loaded = EdmxDiagramLoader.Load(
                    path, layoutManager: new LayoutEngineManager([new DslLayoutEngine(), new MsAglLayoutEngine()])))
                {
                    assert(loaded);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        ///     As <see cref="WithLoaded" />, for assertions that only need the shapes.
        /// </summary>
        private static void WithShapes(TestEdmxBuilder builder, Action<List<EntityTypeShape>> assert)
        {
            WithLoaded(builder, loaded => assert(Shapes(loaded)));
        }
    }
}
