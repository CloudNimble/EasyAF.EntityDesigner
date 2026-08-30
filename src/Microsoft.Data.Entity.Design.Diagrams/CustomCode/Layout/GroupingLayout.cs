// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Data.Entity.Design.Diagrams.View;
using EntityDesignerDiagramConstant = Microsoft.Data.Entity.Design.Edmx.Designer.EntityDesignerDiagramConstant;
using ModelConstants = Microsoft.Data.Entity.Design.Edmx.ModelConstants;
using ViewModelAssociation = Microsoft.Data.Entity.Design.Diagrams.ViewModel.Association;
using ViewModelScalarProperty = Microsoft.Data.Entity.Design.Diagrams.ViewModel.ScalarProperty;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     Works out which shapes on a diagram belong together, so a layout can place them together.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var groups = GroupingLayout.Group(entityShapes);
    ///     foreach (var pair in groups)
    ///     {
    ///         Console.WriteLine($"{pair.Key.Name} belongs to {pair.Value}");
    ///     }
    ///     </code>
    /// </example>
    /// <remarks>
    ///     Engine-agnostic, and deliberately outside both engines: <see cref="DslLayoutEngine" /> ignores it and
    ///     <see cref="MsAglLayoutEngine" /> turns its answer into MSAGL clusters.
    ///     <para>
    ///     The job here is a good first guess across thousands of schemas of wildly varying quality, not to be
    ///     right about any one of them. Every threshold below is a starting value, and every strategy has
    ///     <see cref="GroupingStrategy.Structural" /> beneath it, so a badly tuned number degrades the answer
    ///     rather than failing. What makes that acceptable is that the guess is written back to the EDMX where a
    ///     person can see it and correct it, and a correction is never overwritten. See
    ///     specs/diagram-layout-engines.md.
    ///     </para>
    /// </remarks>
    internal static class GroupingLayout
    {

        #region Fields

        /// <summary>
        ///     The share of entities that must carry a non-default fill colour before the colouring is read as a
        ///     grouping rather than a highlight.
        /// </summary>
        internal const double FillColorCoverage = 0.5;

        /// <summary>
        ///     The fewest edges an entity can have and still be a hub, however small the model is.
        /// </summary>
        /// <remarks>
        ///     Without a floor, a model whose median degree is one makes a hub of anything with four edges, and a
        ///     model whose median is zero makes a hub of everything.
        /// </remarks>
        internal const int HubDegreeFloor = 4;

        /// <summary>
        ///     How many times the median degree an entity needs before it counts as a hub.
        /// </summary>
        internal const double HubDegreeMultiple = 3.0;

        /// <summary>
        ///     The fewest distinct non-default fill colours that can be read as a grouping.
        /// </summary>
        /// <remarks>
        ///     One colour against the default is a highlight - "look at these" - and says nothing about how the
        ///     rest of the model divides up.
        /// </remarks>
        internal const int MinimumDistinctFillColors = 2;

        /// <summary>
        ///     The share of entities that must reference an entity before it counts as a tenant root.
        /// </summary>
        internal const double TenantRootFraction = 0.5;

        /// <summary>
        ///     The suffixes that mark an entity as a candidate reference table.
        /// </summary>
        private static readonly string[] ReferenceTableSuffixes = ["Types", "Type"];

        #endregion

        #region Public Methods

        /// <summary>
        ///     Chooses a strategy for <paramref name="entities" /> and reports which one it picked.
        /// </summary>
        /// <param name="entities">The entities to examine.</param>
        /// <returns>The strategy whose evidence is present.</returns>
        /// <remarks>
        ///     Separated from <see cref="Group" /> so the choice can be asserted on its own. A grouping that comes
        ///     out wrong is usually a strategy chosen wrongly rather than a strategy executed wrongly.
        /// </remarks>
        internal static GroupingStrategy Detect(IReadOnlyList<Entity> entities)
        {
            if (entities is null || entities.Count == 0)
            {
                return GroupingStrategy.Structural;
            }

            if (entities.Any(entity => !string.IsNullOrWhiteSpace(entity.GroupName)))
            {
                return GroupingStrategy.Explicit;
            }

            var coloured = entities.Where(entity => !entity.HasDefaultFillColor).ToList();
            if (coloured.Select(entity => entity.FillColor).Distinct().Count() >= MinimumDistinctFillColors
                && (double)coloured.Count / entities.Count >= FillColorCoverage)
            {
                return GroupingStrategy.FillColor;
            }

            if (entities.Any(entity => entity.IsHub))
            {
                return GroupingStrategy.HubAffinity;
            }

            return GroupingStrategy.Structural;
        }

        /// <summary>
        ///     Assigns every shape a group name.
        /// </summary>
        /// <param name="shapes">The shapes to group.</param>
        /// <returns>
        ///     A group name for every shape given, never <see langword="null" /> and never with a
        ///     <see langword="null" /> or empty name in it.
        /// </returns>
        /// <remarks>
        ///     <see cref="Detect" /> picks the strategy, one of the four grouping methods runs, and
        ///     <c>PlaceReferenceTablesWithOwners</c> then runs regardless of which one it was - a colour grouping
        ///     wants its uncoloured lookup tables pulled to their owners just as much as a structural one does.
        /// </remarks>
        /// <summary>
        ///     Groups shapes by the <c>GroupName</c> already recorded on them, detecting nothing and writing
        ///     nothing.
        /// </summary>
        /// <param name="shapes">The shapes to group.</param>
        /// <returns>
        ///     A map containing only the shapes that carry a non-empty <c>GroupName</c>, each to that name.
        ///     Shapes with no name are absent, so a caller leaves them ungrouped.
        /// </returns>
        /// <remarks>
        ///     Used when grouping is on but name generation is off: the user gets clusters for the names they
        ///     typed and nothing invented on top. See specs/diagram-layout-engines.md.
        /// </remarks>
        internal static IReadOnlyDictionary<EntityTypeShape, string> GroupByExisting(IReadOnlyList<EntityTypeShape> shapes)
        {
            var groups = new Dictionary<EntityTypeShape, string>();

            if (shapes is null)
            {
                return groups;
            }

            foreach (var shape in shapes)
            {
                var name = shape?.ModelShape?.GroupName.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    groups[shape] = name.Trim();
                }
            }

            return groups;
        }

        internal static IReadOnlyDictionary<EntityTypeShape, string> Group(IReadOnlyList<EntityTypeShape> shapes)
        {
            var groups = new Dictionary<EntityTypeShape, string>();

            if (shapes is null || shapes.Count == 0)
            {
                return groups;
            }

            var entities = BuildEntities(shapes);
            var strategy = Detect(entities);

            switch (strategy)
            {
                case GroupingStrategy.Explicit:
                    GroupByExplicitName(entities);
                    break;

                case GroupingStrategy.FillColor:
                    GroupByFillColor(entities);
                    break;

                case GroupingStrategy.HubAffinity:
                    GroupByHubAffinity(entities);
                    break;

                default:
                    GroupByStructure(entities);
                    break;
            }

            PlaceReferenceTablesWithOwners(entities);

            foreach (var entity in entities)
            {
                groups[entity.Shape] = string.IsNullOrWhiteSpace(entity.Group) ? entity.Name : entity.Group;
            }

            return groups;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Gathers everything the strategies below need from the diagram, once.
        /// </summary>
        /// <remarks>
        ///     Tenant roots are worked out here and their edges then left out of every neighbour set. An entity
        ///     half the model points at says almost nothing about where any of those entities belong, and treating
        ///     its edges as evidence pulls the whole diagram adjacent to it.
        /// </remarks>
        internal static List<Entity> BuildEntities(IReadOnlyList<EntityTypeShape> shapes)
        {
            var entities = shapes.Select(shape => new Entity(shape)).ToList();
            var byShape = entities.ToDictionary(entity => entity.Shape);

            foreach (var entity in entities)
            {
                foreach (var link in entity.Shape.ConnectedLinks)
                {
                    if (link.FromShape is not EntityTypeShape from
                        || link.ToShape is not EntityTypeShape to
                        || ReferenceEquals(from, to))
                    {
                        continue;
                    }

                    var otherShape = ReferenceEquals(from, entity.Shape) ? to : from;
                    if (byShape.TryGetValue(otherShape, out var other))
                    {
                        entity.Neighbours.Add(other);
                    }
                }
            }

            foreach (var entity in entities)
            {
                entity.IsTenantRoot = entities.Count > 2
                    && (double)entity.Neighbours.Count / (entities.Count - 1) > TenantRootFraction;
            }

            foreach (var entity in entities)
            {
                entity.Peers.UnionWith(entity.Neighbours.Where(neighbour => !neighbour.IsTenantRoot));
            }

            // The median is taken over the peer degrees, so that the tenant roots removed above cannot inflate
            // the bar every other entity is measured against.
            var median = Median(entities.Where(entity => !entity.IsTenantRoot).Select(entity => entity.Peers.Count));
            var threshold = Math.Max(HubDegreeFloor, (int)Math.Ceiling(median * HubDegreeMultiple));

            foreach (var entity in entities)
            {
                entity.IsHub = !entity.IsTenantRoot
                    && !entity.IsReferenceTableCandidate
                    && entity.Peers.Count >= threshold;
            }

            return entities;
        }

        /// <summary>
        ///     Names a group after the entity the rest of it centres on.
        /// </summary>
        /// <param name="members">The entities the group was formed from.</param>
        /// <returns>The name of the most connected member, or an empty string when there are none.</returns>
        /// <remarks>
        ///     Every group is named this way, whichever strategy produced it, because the name has one job: to
        ///     tell the reader what is in the group. "Post" does that; a colour does not, and a hex colour does
        ///     not even say which swatch it was.
        ///     <para>
        ///     Connections are counted **within the group**, not across the model, which is what makes this the
        ///     table the others centre on rather than just the busiest table that happens to be in here. Ties go
        ///     to the alphabetically first name, so the same model always produces the same names.
        ///     </para>
        ///     <para>
        ///     Names stay distinct without being checked, because every strategy draws its names from a set of
        ///     entities no other group can draw from: colour groups from the coloured entities, and the
        ///     structural leftovers from the entities no colour group claimed.
        ///     </para>
        /// </remarks>
        private static string NameFor(IReadOnlyCollection<Entity> members)
        {
            if (members.Count == 0)
            {
                return string.Empty;
            }

            var within = new HashSet<Entity>(members);

            return members
                .OrderByDescending(entity => entity.Peers.Count(peer => within.Contains(peer)))
                .ThenByDescending(entity => entity.Peers.Count)
                .ThenBy(entity => entity.Name, StringComparer.Ordinal)
                .First()
                .Name;
        }

        /// <summary>
        ///     Fills in the entities a strategy left ungrouped, from the company they keep.
        /// </summary>
        /// <remarks>
        ///     Repeated until nothing more changes, so a group spreads outwards one ring at a time rather than
        ///     depending on the order the entities happen to be in. Anything still ungrouped afterwards is in a
        ///     part of the graph with no grouped entity in it at all, and falls to its structural component.
        /// </remarks>
        private static void GroupByAffinity(IReadOnlyList<Entity> entities)
        {
            bool changed;

            do
            {
                changed = false;

                foreach (var entity in entities.Where(entity => string.IsNullOrWhiteSpace(entity.Group)))
                {
                    var vote = entity.Peers
                        .Where(peer => !string.IsNullOrWhiteSpace(peer.Group))
                        .GroupBy(peer => peer.Group)
                        .OrderByDescending(group => group.Count())
                        .ThenByDescending(group => group.Max(peer => peer.Peers.Count))
                        .ThenBy(group => group.Key, StringComparer.Ordinal)
                        .FirstOrDefault();

                    if (vote is null)
                    {
                        continue;
                    }

                    entity.Group = vote.Key;
                    changed = true;
                }
            }
            while (changed);

            GroupByStructure(entities.Where(entity => string.IsNullOrWhiteSpace(entity.Group)).ToList());
        }

        /// <summary>
        ///     Uses the group names already in the file, and infers one for every shape that has none.
        /// </summary>
        private static void GroupByExplicitName(IReadOnlyList<Entity> entities)
        {
            foreach (var entity in entities)
            {
                entity.Group = entity.GroupName?.Trim();
            }

            GroupByAffinity(entities);
        }

        /// <summary>
        ///     Groups by fill colour, leaving default-coloured shapes to be pulled in by their neighbours.
        /// </summary>
        /// <remarks>
        ///     Colour carries role and proximity carries usage, and a diagram can say both at once - the model
        ///     this was designed against had lookup tables in one colour and subsystems in others. Grouping every
        ///     default-coloured shape together would collect them into one block away from everything using them,
        ///     so they are inferred from their neighbours instead.
        /// </remarks>
        private static void GroupByFillColor(IReadOnlyList<Entity> entities)
        {
            var byColour = entities
                .Where(entity => !entity.HasDefaultFillColor)
                .GroupBy(entity => entity.FillColor.ToArgb());

            foreach (var colour in byColour)
            {
                var members = colour.ToList();
                var name = NameFor(members);

                foreach (var entity in members)
                {
                    entity.Group = name;
                }
            }

            GroupByAffinity(entities);
        }

        /// <summary>
        ///     Seeds a group from each hub and attaches everything else to the hub it leans towards.
        /// </summary>
        private static void GroupByHubAffinity(IReadOnlyList<Entity> entities)
        {
            foreach (var entity in entities.Where(entity => entity.IsHub))
            {
                entity.Group = entity.Name;
            }

            GroupByAffinity(entities);
        }

        /// <summary>
        ///     Groups by which entities are reachable from which, naming each group after its busiest member.
        /// </summary>
        /// <remarks>
        ///     The floor beneath every other strategy, and the fallback each of them uses for whatever it could
        ///     not place. Uses peers rather than neighbours, so a tenant root does not collapse the whole model
        ///     into a single component.
        /// </remarks>
        private static void GroupByStructure(IReadOnlyList<Entity> entities)
        {
            var remaining = new HashSet<Entity>(entities);

            foreach (var start in entities)
            {
                if (!remaining.Remove(start))
                {
                    continue;
                }

                var component = new List<Entity> { start };
                var queue = new Queue<Entity>();
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    foreach (var peer in queue.Dequeue().Peers)
                    {
                        if (remaining.Remove(peer))
                        {
                            component.Add(peer);
                            queue.Enqueue(peer);
                        }
                    }
                }

                var name = NameFor(component);

                foreach (var entity in component)
                {
                    entity.Group = name;
                }
            }
        }

        /// <summary>
        ///     Returns the median of <paramref name="values" />, or zero when there are none.
        /// </summary>
        private static double Median(IEnumerable<int> values)
        {
            var ordered = values.OrderBy(value => value).ToList();

            if (ordered.Count == 0)
            {
                return 0;
            }

            var middle = ordered.Count / 2;

            return ordered.Count % 2 == 1
                ? ordered[middle]
                : (ordered[middle - 1] + ordered[middle]) / 2.0;
        }

        /// <summary>
        ///     Moves each lookup table into the group of whatever uses it.
        /// </summary>
        /// <remarks>
        ///     Runs after every strategy rather than being one of its own. A lookup table's own connections say
        ///     only that it is a lookup table; where it belongs on a diagram is decided by what reads it.
        ///     <para>
        ///     An entity qualifies when its name ends in Type or Types, an owner has a <c>*</c> to <c>1</c> or
        ///     <c>0..1</c> association to it, and that owner has a scalar property named for it. All three
        ///     together, because any one alone catches ordinary entities: plenty of real tables end in Type
        ///     without being lookups.
        ///     </para>
        ///     <para>
        ///     A lookup table whose group is named in the file is left where the file put it. This pass is a
        ///     guess about where an entity is most useful, and a guess does not get to overrule an instruction.
        ///     </para>
        /// </remarks>
        private static void PlaceReferenceTablesWithOwners(IReadOnlyList<Entity> entities)
        {
            var byShape = entities.ToDictionary(entity => entity.Shape);

            var movable = entities.Where(
                entity => entity.IsReferenceTableCandidate && string.IsNullOrWhiteSpace(entity.GroupName));

            foreach (var entity in movable)
            {
                var owners = new List<Entity>();

                foreach (var link in entity.Shape.ConnectedLinks.OfType<AssociationConnector>())
                {
                    if (link.ModelElement is not ViewModelAssociation association)
                    {
                        continue;
                    }

                    var lookupIsSource = ReferenceEquals(association.SourceEntityType, entity.Shape.TypedModelElement);

                    var lookupMultiplicity = lookupIsSource ? association.SourceMultiplicity : association.TargetMultiplicity;
                    var ownerMultiplicity = lookupIsSource ? association.TargetMultiplicity : association.SourceMultiplicity;

                    if (ownerMultiplicity != ModelConstants.Multiplicity_Many
                        || (lookupMultiplicity != ModelConstants.Multiplicity_One
                            && lookupMultiplicity != ModelConstants.Multiplicity_ZeroOrOne))
                    {
                        continue;
                    }

                    var ownerShape = ReferenceEquals(link.FromShape, entity.Shape) ? link.ToShape : link.FromShape;

                    if (ownerShape is EntityTypeShape shape
                        && byShape.TryGetValue(shape, out var owner)
                        && owner.ScalarPropertyNames.Contains(entity.Name + "Id"))
                    {
                        owners.Add(owner);
                    }
                }

                var group = owners
                    .Where(owner => !string.IsNullOrWhiteSpace(owner.Group))
                    .GroupBy(owner => owner.Group)
                    .OrderByDescending(candidates => candidates.Count())
                    .ThenByDescending(candidates => candidates.Max(owner => owner.Peers.Count))
                    .ThenBy(candidates => candidates.Key, StringComparer.Ordinal)
                    .FirstOrDefault();

                if (group is not null)
                {
                    entity.Group = group.Key;
                }
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///     One shape and everything the grouping needs to know about it.
        /// </summary>
        /// <remarks>
        ///     Not a data model of its own - it holds references to the real shapes and is thrown away when the
        ///     grouping finishes. It exists so the facts each strategy reads are gathered once rather than walked
        ///     out of the diagram again per strategy.
        /// </remarks>
        internal sealed class Entity
        {

            #region Properties

            /// <summary>
            ///     The colour this shape is filled with in the EDMX.
            /// </summary>
            /// <remarks>
            ///     Read from the EDMX shape rather than the diagram shape, whose fill is adjusted for the current
            ///     Visual Studio theme. What matters here is the colour a person chose.
            /// </remarks>
            public Color FillColor { get; }

            /// <summary>
            ///     The group this entity has been placed in, filled in as the grouping runs.
            /// </summary>
            public string Group { get; set; }

            /// <summary>
            ///     The group already recorded in the EDMX, empty when there is none.
            /// </summary>
            public string GroupName { get; }

            /// <summary>
            ///     Whether this entity's fill colour is the one every shape starts with.
            /// </summary>
            public bool HasDefaultFillColor { get; }

            /// <summary>
            ///     Whether this entity is central enough to organize others around.
            /// </summary>
            public bool IsHub { get; set; }

            /// <summary>
            ///     Whether this entity's name marks it as a possible lookup table.
            /// </summary>
            /// <remarks>
            ///     A candidate only. <c>PlaceReferenceTablesWithOwners</c> requires two further pieces of
            ///     evidence before treating it as one.
            /// </remarks>
            public bool IsReferenceTableCandidate { get; }

            /// <summary>
            ///     Whether more than half the model points at this entity.
            /// </summary>
            public bool IsTenantRoot { get; set; }

            /// <summary>
            ///     The entity's name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            ///     Every entity this one is connected to.
            /// </summary>
            public HashSet<Entity> Neighbours { get; } = [];

            /// <summary>
            ///     <see cref="Neighbours" /> with the tenant roots removed - the connections that carry meaning
            ///     about where this entity belongs.
            /// </summary>
            public HashSet<Entity> Peers { get; } = [];

            /// <summary>
            ///     The names of this entity's scalar properties.
            /// </summary>
            public HashSet<string> ScalarPropertyNames { get; }

            /// <summary>
            ///     The shape being grouped.
            /// </summary>
            public EntityTypeShape Shape { get; }

            #endregion

            #region Constructors

            /// <summary>
            ///     Reads everything that does not depend on the other entities off <paramref name="shape" />.
            /// </summary>
            /// <param name="shape">The shape to describe.</param>
            public Entity(EntityTypeShape shape)
            {
                Shape = shape;
                Name = shape.TypedModelElement?.Name ?? string.Empty;

                var modelShape = shape.ModelShape;

                FillColor = modelShape?.FillColor.Value ?? EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor;
                GroupName = modelShape?.GroupName.Value ?? string.Empty;
                HasDefaultFillColor = FillColor.ToArgb() == EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor.ToArgb();

                IsReferenceTableCandidate = ReferenceTableSuffixes.Any(
                    suffix => Name.Length > suffix.Length && Name.EndsWith(suffix, StringComparison.Ordinal));

                ScalarPropertyNames = new HashSet<string>(
                    shape.TypedModelElement?.Properties.OfType<ViewModelScalarProperty>().Select(property => property.Name)
                        ?? [],
                    StringComparer.Ordinal);
            }

            #endregion

        }

        #endregion

    }

}
