# Diagram layout engines

Swappable diagram layout, chosen per diagram in the property window and toggled from the designer's floating toolbar. The existing DSL layout stays as the default, `Legacy`; an MSAGL-backed engine is the opt-in `Modern` mode that groups related entities and persists explicit connector routes to the EDMX.

## Why

The out-of-the-box result is bad, and it is worst exactly where it matters most — a model someone just generated and is seeing for the first time.

`EntityDesignerSurface.AutoLayoutDiagram` is not an algorithm. It is three calls to the Modeling SDK's `AutoLayoutShapeElements` with different `VGRoutingStyle` / `PlacementValueStyle` pairs, wrapped in a `SaveLayoutFlags` disposable that freezes subsets between passes, and its comments still carry the original Microsoft bug numbers (`DD 40487`, `DD 40516`). It is a pile of workarounds for the SDK engine's behaviour.

Observed failure modes on a real 39-entity model:

- Reference tables scattered to the perimeter, far from the entities that use them, producing long edges across the whole canvas.
- Those long edges collapsing into dense parallel bundles that read as noise.
- Connectors passing behind shapes.
- Grouping information already present in the file — `FillColor` — ignored entirely by placement.

## What this is not

**It does not remove the GraphObject dependency.** That was investigated and the answer is no. GraphObject is not the auto-layout engine sitting behind one call; it is the geometry substrate under every DSL shape. `ShapeElement.CreateGraphLayoutObject`, `NodeShape.GraphNode` (wrapping `VGNode`), `LinkShape.GraphEdge` (wrapping `VGEdge`), `LinkShape.DefaultRoutingStyle`, `NodeShape.GetCompliantAnchorPoint(…, VGRoutingStyle)` and `BinaryLinkShapeBase.SetFixedFromValue(VGFixedCode)` all name GraphObject types in DSL's own public API. `Microsoft.VisualStudio.Modeling.Sdk.Diagrams.dll` therefore carries an assembly reference to it regardless of what our code calls.

`Microsoft.VisualStudio.Modeling.Sdk.Diagrams.GraphObject.dll` is a mixed-mode assembly with PE machine type `0x8664` — x64 only. `PlatformTarget=x64` in `Microsoft.Data.Entity.Design.Diagrams.Tools.csproj` is load-bearing and stays. Nothing in this document changes that, and no proposal should cite it as a benefit.

**It does not replace DSL routing in the default engine.** `DslLayoutEngine` is unchanged and `GraphEdge.RouteJIT` keeps handling interactive drags there. That is the *only* place DSL routing survives — Modern mode never touches it, at any stage. See *Routing and persistence*.

**It does not build a taxonomy of table roles.** Only reference tables are detected, because that is the one convention that holds across arbitrary schemas and has an obvious geometric payoff. Everything else is the user's to correct via `GroupName`.

## Architecture

```
LayoutEngineBase (abstract)
    Mode, DisplayName
    Layout(EntityDesignerSurface surface, IList shapes, ConnectorMode connectorMode)

    DslLayoutEngine      today's three-pass AutoLayoutShapeElements + Reroute, moved verbatim
    MsAglLayoutEngine    grouping -> MSAGL placement -> routing -> persisted routes

LayoutEngineManager
    IReadOnlyDictionary<LayoutMode, LayoutEngineBase>   injected, keyed by mode
    Resolve(LayoutMode) -> LayoutEngineBase             falls back to the first registered
```

Engines are keyed by `LayoutMode` rather than by a string, so the value in the file, the value in the property window and the value in the registry are the same thing and there is no mapping to keep in step. They hold no per-diagram state — see *Where the settings live*.

`EntityDesignerSurface.AutoLayoutDiagram(IList shapes)` becomes `LayoutManager.Resolve(LayoutMode).Layout(this, shapes, ConnectorMode)`, reading both settings off its own model diagram on every call. Its five existing callers are untouched:

| Caller | Trigger |
|---|---|
| `MicrosoftDataEntityDesignCommandSet.cs:1981` | Diagram → Layout menu |
| `MicrosoftDataEntityDesignDocView_PanZoom.cs` | floating toolbar button |
| `EntityModelToDslModelTranslatorStrategy.cs:630` | load, for shapes with no saved position |
| `EntityDesignerViewModel.cs:502` | model change |
| `AutoArrangeHelper.cs:93` | drag-drop from the Model Browser |

All three engine types live in `Microsoft.Data.Entity.Design.Diagrams` (layer 2, the Designer layer per `layer-map.md`).

## Grouping

`GroupingLayout` is engine-agnostic and sits beside the manager, not inside either engine. `DslLayoutEngine` ignores it; `MsAglLayoutEngine` turns its output into MSAGL `Cluster`s.

```
Group(IEnumerable<EntityTypeShape>) -> IReadOnlyDictionary<EntityTypeShape, string>

    Detect(shapes) -> GroupingStrategy { Explicit, FillColor, HubAffinity, Structural }
    GroupByExplicitName / GroupByFillColor / GroupByHubAffinity / GroupByStructure
    PlaceReferenceTablesWithOwners(shapes, assignment)      shared post-pass
```

`Group` is `Detect` + dispatch + post-pass. Each strategy is a separate method so each is independently testable, and `Detect` is testable on its own.

### Detect

In order; first match wins.

1. Any shape has a non-empty `GroupName` → **Explicit**
2. At least two distinct non-default `FillColor`s, covering at least 50% of entities → **FillColor**
3. At least one hub exists → **HubAffinity**
4. Otherwise → **Structural**

The `FillColor` threshold matters. One non-default colour is a highlight, not a grouping. The default comes from `EntityDesignerDiagramConstant.EntityTypeShapeDefaultFillColor`.

**A hub** is an entity whose degree — counting association edges, excluding reference tables and tenant roots — is at least three times the median degree of the model, with a floor of four edges so tiny models do not produce spurious hubs.

**A tenant root** is an entity referenced by more than half the entities in the model through associations. In a multi-tenant schema this is typically the tenant or account entity.

Both thresholds are starting values, exposed as constants and tuned against real models during implementation. They are the two numbers most likely to need adjustment, and every strategy has `Structural` beneath it as a floor, so a badly tuned threshold degrades rather than fails.

### The reference-table post-pass

Runs after every strategy, not as a strategy of its own. A colour grouping wants its uncoloured reference tables pulled to their owners just as much as a structural one does.

This is deliberate and was arrived at from a real model. In the motivating diagram the author had an AI colour reference tables one colour and subsystems separate colours — **two orthogonal axes at once**, so the eye reads role and subsystem simultaneously. Grouping strictly on colour would collect every reference table into one large cluster placed away from everything that uses it. Proximity carries usage; colour carries role. The post-pass is what keeps those separate.

`L` is a reference table for `E` when all of:

- `L.Name` ends in `Type` or `Types`
- an association `E → L` where `L`'s end is `1` or `0..1` and `E`'s end is `*`
- `E` has a scalar property named `{L.Name}Id`

`L` joins `E`'s group. With owners in several groups: most edges wins; tie broken by highest owner degree.

### Naming a group

Every group is named after the entity the rest of it centres on — the member with the most associations **to other members of the same group**, ties going to the alphabetically first name so a model always produces the same names.

This replaced naming colour groups after their colour, which was wrong twice over: a hex value does not say which swatch it was, and even a resolved colour name says nothing about what is in the group. The name has one job, which is to tell a reader what they are looking at, and the group names are also the only view anyone has of how the grouping came out.

Connections are counted inside the group rather than across the model on purpose. A table with many associations that mostly leave its group is busy, not central, and naming the group after it describes the model rather than the group.

Names come out distinct without being checked: every strategy draws its names from a set of entities no other group can draw from.

### Clusters in MSAGL

`MsAglLayoutEngine` turns each group into a `Cluster` under `graph.RootCluster`. Three things measured rather than assumed:

- **`LayoutHelpers.CalculateLayout` handles clusters itself.** It routes to `InitialLayoutByCluster` when the root has children; calling that class directly gives identical positions, so there is no reason to.
- **`RootCluster.UserData` must be non-null.** MSAGL looks every cluster up in `SugiyamaLayoutSettings.ClusterSettings` on the way in, the root included, and a null key throws out of `Dictionary.ContainsKey` before any layout runs. The root is named `<root>` purely to avoid that.
- **`PackingMethod` and `PackingAspectRatio` do nothing on the clustered path.** Clusters are placed by laying the root out like any other graph, so the shape of the result comes from how the groups connect, not from the packing settings.

On a six-group model whose groups form a tree, clustering took the diagram from a 5.68 aspect ratio to 1.97 and tightened each group, at the cost of about 60% more area. On a model whose groups form a *chain* it produced a single column — correct layered behaviour for a chain, and worth remembering before reading a narrow result as a bug.

Nothing is added for a single group: one cluster holding everything is the same arrangement with an extra layout pass over it.

### Hub affinity

Seeds groups from high-degree entities and attaches the rest by affinity. One caveat worth encoding: a **tenant root** — an entity referenced by a large fraction of the model through the same FK name — should have its edges down-weighted rather than up-weighted. Treating it as a hub pulls half the diagram adjacent to it and produces a hairball. Its presence on a table says almost nothing about where that table belongs.

## GroupName

A new optional attribute, per shape rather than per entity, so two diagrams over one model can group differently.

```xml
<EntityTypeShape EntityType="Model.Post" PointX="5" PointY="2" GroupName="Content" />
```

It mirrors `FillColor`, in three parts rather than the four that were planned:

1. `TEntityTypeShape` attribute in `Microsoft.Data.Entity.Design.Edmx_3.xsd`, beside `FillColor`. The XSD already carries four of our own attributes on `TDiagram` under a *"we need to move this new value to a new XSD version"* comment, so the precedent exists.
2. `Edmx.Designer.EntityTypeShape.GroupName` as `DefaultableValue<string>` with a nested `GroupNameDefaultableValue`, plus registration in `Children` and `MyAttributeNames`.
3. A `GroupName` property on `EFEntityTypeShapeDescriptor`, exactly as `FillColor` has one, so the value is editable in the property window when an entity is selected.

**The DSL domain property was dropped.** The original plan added one to `DslDefinition.dsl` and a write-back leg through `EntityTypeShapeChange`. Both turned out to be for nothing: `FillColor` needs a DSL property because the shape has to *draw* itself in that colour, and `GroupName` is never drawn. The layout reads it through `EntityTypeShape.ModelShape`, the cross-reference hop `EntityDesignerSurface` already uses everywhere, and the property window edits the EDMX object directly the way `FillColor` does. That leaves the generated DSL code untouched and needs no `msbuild /t:TransformAll`.

`EntityTypeShape.ModelShape` was added for this and mirrors `EntityDesignerSurface.ModelDiagram`, so getting from a view object to its EDMX object reads the same at either level.

### Write-back

After grouping, any shape with **no** `GroupName` gets its detected group written to the EDMX, through `EntityDesignerSurface.PersistGroupNames` — one transaction for the whole diagram, so a layout costs one undo rather than one per shape.

An existing value is never overwritten. That is what closes the loop: the first pass persists its guess, the user edits the names by hand, and every later run takes the `Explicit` path and honours them. The guess is visible and editable in the file rather than buried in code, which is the whole point — the code's job is a good first guess across thousands of schemas of varying quality, not to be right about any one of them.

"Never overwritten" has to hold for **placement**, not only for the file. The reference-table post-pass originally moved every lookup table to its owner's group including ones the user had named, so a shape kept its name in the EDMX while being laid out somewhere else. It now skips any candidate that already has a `GroupName`. The guard in `PersistGroupNames` is a second line of defence at the write boundary; with the post-pass fixed, no current path reaches it.

## Traversal

No new data model. `GroupingLayout` operates on real `EntityTypeShape`s.

An earlier draft proposed a `GroupingModel` projection for testability. It was dropped: the EDMX designer model and the DSL model already hold this data, and a third representation is worse than a slightly heavier test fixture. `HeadlessRoutingSpikeTests` proves a store with shapes and connectors can be built with no shell in about forty lines.

The layout engines should read the way `SvgExporter` reads — walk `diagram.NestedChildShapes`, type-test, then use `connector.EdgePoints` / `FromShape` / `ToShape`. One thing blocks that today. Getting from a shape to its connectors still requires the `ArrayList` dance in `AutoLayoutDiagram`:

```csharp
ArrayList allLinks = new ArrayList(entityTypeShape.FromRoleLinkShapes);
allLinks.AddRange(entityTypeShape.ToRoleLinkShapes);
```

So `EntityTypeShape` gains `ConnectedLinks`, returning `IEnumerable<BinaryLinkShape>`. Both engines use it, and the existing code drops its `ArrayList`.

## Routing and persistence

**Modern mode never uses the DSL line drawing routines. Not as a fallback, not as an intermediate step, not to isolate a variable while placement is evaluated. There is no version of this feature in which MSAGL places the shapes and DSL routes the connectors.**

The DSL routing is the reason this work exists. Shipping MSAGL placement on top of it would be measuring the new thing through the defect it replaces.

`MsAglLayoutEngine` always sets `ManuallyRouted = true` and assigns `EdgePoints`; routes are required to be persisted. The first version of the engine does this, the same as every version after it.

**None of the persistence is new code.** The path exists end to end:

```
AssociationConnector_ChangeRule       fires on EdgePoints + ManuallyRouted, TimeToFire.TopLevelCommit
  -> AssociationConnectorChange
    -> SetConnectorPointsCommand      writes <ConnectorPoint PointX PointY>
```

`InheritanceConnector_ChangeRule` mirrors it. Read-back exists too: `TranslateAssociationConnectors` and `TranslateInheritanceConnectors` load `ConnectorPoint`s into `EdgePoints` when `ManuallyRouted="true"`, and add the connector to `shapesToAutoLayout` when it is false.

Turning Modern mode off is also already implemented. Setting `ManuallyRouted = false` runs `SetConnectorPointsCommand` with an empty list, stripping every `<ConnectorPoint>` and handing routing back to DSL. The toggle maps directly onto the flag.

Two consequences to accept:

- **Connectors will not reroute when the user drags a shape**, because that is what `ManuallyRouted` means. Modern mode needs a defined answer here — see *Spike first*.
- **The EDMX grows.** A 39-entity model has roughly 113 connectors; at two to four points each that is 250–450 new elements, and a visible diff every time layout re-runs.

## Where the settings live

Both settings are attributes on `<Diagram>`, beside the `ZoomLevel` / `ShowGrid` / `SnapToGrid` / `DisplayType` that are already there, and both are exposed on `EFDiagramDescriptor` so they appear in the property window when the diagram is selected.

```xml
<Diagram DiagramId="…" Name="Diagram1" ZoomLevel="100" LayoutMode="Modern" ConnectorMode="Layered">
```

| Attribute | Values | Default |
|---|---|---|
| `LayoutMode` | `Legacy`, `Modern` | `Legacy` |
| `ConnectorMode` | `Legacy`, `Orthogonal`, `Layered`, `Curved`, `Straight` | `Legacy` |

`Legacy` is the default for both so that every file written before these existed keeps arranging exactly as it did.

`ConnectorMode.Legacy` pairs with `LayoutMode.Legacy` and with nothing else — it means the Modeling SDK draws the connectors, which is a consequence of the layout mode rather than a separate choice. Two things follow:

- `IsBrowsableConnectorMode()` hides the property unless the diagram is in modern layout, using the `IsBrowsableXxx` convention `ReflectedPropertyDescriptor` already looks for.
- `ConnectorModeConverter` drops `Legacy` from the dropdown, and the descriptor's getter reports `Orthogonal` while the stored value is `Legacy`, so the grid shows what the layout will actually do rather than a value that is really just "unset".

Per-diagram settings rule out a current engine on the manager. One `LayoutEngineManager` is registered for the package and serves every open document, so a single shared selection would be right for the last diagram touched and wrong for all the others. `LayoutEngineManager.Resolve(LayoutMode)` hands out an engine instead, falling back to the first registered when a mode is not recognized — the value comes out of a file a user can hand-edit, and an unknown mode should cost a diagram its preferred arrangement, not its ability to open.

For the same reason no engine carries per-diagram state. `LayoutEngineBase.Layout` takes the connector mode as an argument; `DslLayoutEngine` ignores it.

### Changing one has to rearrange the diagram

**Any property in the property window that feeds the layout re-runs the layout when it changes.** Not a special case for one or two of them — a setting the user can change but cannot see the effect of is worse than no setting at all, and the next one added would have the same problem.

`DiagramLayoutInput.Includes` is the single list of which attributes those are: `LayoutMode` and `ConnectorMode` on `Diagram`, `GroupName` on `EntityTypeShape`. Anything later added to the property window that the layout reads belongs in it.

`EntityDesignerViewModel.OnModelChangesCommitted` already walks every committed model change and already ends by calling `AutoLayoutDiagram` for shapes that need placing. The check hangs off that walk, and the re-layout off that same tail — outside the store transaction the walk opens, because a layout opens its own on both the store and the model.

Two things that follow, both load-bearing:

- **`AutoLayoutDiagram` ignores a call made while a layout is already running.** A layout writes group names and connector routes back to the model, those writes come back as model changes, and `GroupName` is on the list above — so without the guard the first layout would ask for a second. It would terminate (the second pass writes nothing) but it would do the work twice.
- **`GroupName` is hidden unless the diagram is in modern layout**, the same way `ConnectorMode` is. The legacy engine has no notion of groups, so editing it there would rearrange the diagram without using the value.

No debounce. The property grid commits a string edit on Enter or focus loss rather than per keystroke, so one edit is one change is one layout.

### Selecting the diagram

Clicking the diagram background did not reach `EFDiagramDescriptor` before this. `ConvertDslModelElementArrayToItemDescriptors` took the selected object's `ModelElement` — for the surface that is `EntityDesignerViewModel`, which is not cross-referenced to anything — so the lookup found nothing and the raw DSL diagram went to the property window. The surface itself *is* the cross-referenced presentation element, so it is now taken directly.

## The toolbar toggle

The floating toolbar already supports this. `FloatingZoomControl.xaml` has a `ToggleTemplate` with two-way `IsChecked`, `CommandTemplateSelector` dispatches on `MenuCommandDefinition.IsToggle`, and `MicrosoftDataEntityDesignDocView_PanZoom.cs` already registers `ShowGrid` and `SnapToGrid` toggles plus an `Auto Layout` button.

The "Modern Layout" toggle follows that pattern. It is a shortcut for the `LayoutMode` property rather than a second setting: toggling it calls `EntityDesignerSurface.PersistLayoutMode`, which writes the same attribute the property window writes — and therefore rearranges the diagram through the same path, for the same reason.

## Dependency

`Msagl` 1.2.1 — MIT, `netstandard2.0`, **zero dependencies**, owner Microsoft, author levnach. It is the core-only package; the older `AutomaticGraphLayout` id is the 2021 build. Pure managed geometry, no UI, no native code, so it loads under both the net48 VSIX and the net10 CLI.

What is used:

| Piece | For |
|---|---|
| `GeometryGraph`, `Cluster`, `RectangularClusterBoundary` | groups as first-class nested clusters |
| `LayoutHelpers.CalculateLayout` | placement |
| `RectilinearEdgeRouter` | orthogonal routing with obstacle avoidance |

### Choosing the placement algorithm

MSAGL offers several ways to decide where boxes go. The two that matter here:

- **Layered** — arranges boxes in rows with edges flowing one direction, like an org chart. If `Post` has a `PostTypeId`, `PostType` sits in a row above `Post`.
- **Force-directed** — a physics simulation where boxes repel and edges pull like springs. Produces organic clumps with no consistent direction.

Layered is the default. In a database the foreign key direction means something — this table depends on that one — and layered puts that on screen as "up means depended-on". Force-directed discards it.

**Swapping is a one-line change, not an architectural fork.** `LayoutHelpers.CalculateLayout(geometryGraph, settings, cancelToken)` dispatches on the runtime type of `settings`, and `SugiyamaLayoutSettings` (layered), `MdsLayoutSettings` (force-directed), `FastIncrementalLayoutSettings` and `RankingLayoutSettings` all derive from `LayoutAlgorithmSettings`. Everything around the call — building the graph, assigning groups to clusters, routing, writing positions back — is identical whichever is chosen.

So the algorithm is a settable **property** on `MsAglLayoutEngine`, defaulting to layered:

```csharp
public LayoutAlgorithmSettings Algorithm { get; set; } = MsAglConstants.Layered;
```

A pre-built, tuned `LayoutAlgorithmSettings` for each option lives as a constant on `MsAglConstants`, so defaults sit in one place and switching is assigning a different constant.

It is a property rather than a constructor argument because of how the engine is used. `LayoutEngineManager` holds one keyed instance of each engine for the life of the designer; taking the algorithm at construction would mean either an instance per algorithm or rebuilding the manager to change it. A property lets `edmx layout --algorithm` set it before invoking, and leaves room for a designer-side picker later without disturbing registration.

`RectilinearEdgeRouter` is the slow option — reports of it bogging down around 90 nodes. That is acceptable here precisely because Modern mode bakes once on an explicit user action rather than routing per frame, and `EntityDesignerSurface.BeginLongOperation` already exists to report it.

## The `edmx layout` command

Layout gets its own CLI subcommand. It does **not** become a flag on `edmx render`.

`render` reads an EDMX and writes a picture somewhere else; every one of its options is output-side (`--format`, `--output`, `--transparent`, `--show-types`). Layout rewrites the source file — positions, connector routes, `GroupName`. A flag that turns a read-only command into a mutating one is a trap, and nothing else works that way: Graphviz can emit computed coordinates but only via `-Tdot` into a new file, and `dotnet build` does not run `dotnet format`.

```
edmx layout <input.edmx> [--algorithm layered|force-directed] [--diagram <name>]
```

It runs the same `MsAglLayoutEngine` the designer runs, through the existing headless store, and saves the file. The CLI already uses `McMaster.Extensions.CommandLineUtils` with `[Subcommand(typeof(RenderCommand))]` on `EntityDesignerRootCommand`, so this is one attribute and one class.

This is also how algorithm comparisons get produced. Copy the model, run `edmx layout` over each copy with a different `--algorithm`, then `edmx render` each result to SVG. Each command keeps doing one thing, and the comparison yields both the laid-out EDMX files and the pictures.

`--algorithm` exists because the CLI is where comparison happens. The designer toolbar stays the plain on/off toggle; no algorithm picker ships in the UI unless a comparison shows more than one option is worth keeping.

## Testing

- `GroupingLayout` — `Detect` and each strategy tested independently, plus the reference-table post-pass. Fixtures built through the headless store, following `HeadlessRoutingSpikeTests`.
- `GroupName` — round-trip through EDMX: read, write-back-when-absent, never-overwrite-when-present.
- `MsAglLayoutEngine` — geometry assertions on the output: no overlapping shapes, reference tables within a bounded distance of their owner, every connector carrying `ManuallyRouted="true"` and at least two edge points.
- Engines are swappable, so `LayoutEngineManager` is tested with a stub engine rather than either real one.

Mutate the implementation and confirm each new test fails before trusting it.

Fixtures come from `TestEdmxBuilder`, which composes an EDMX from a short description — entities, associations with their foreign keys, and per-shape fill colours and group names. `TestEdmx` stays as it is: a literal is the right shape for one fixed two-entity model and the wrong shape for grouping, where every test wants a different arrangement of a dozen entities. The builder writes the conceptual model and the diagram only, because the layout reads neither the storage model nor the mappings.

What the mutation pass caught, recorded because both are the kind of thing that reads as covered when it is not:

- `Layout_leaves_a_group_name_already_in_the_file_alone` originally named a group on `Show`, and passed with the never-overwrite guard deleted. Under the `Explicit` strategy a named shape's detected group *is* its stored name, so writing it back changes nothing. The only entity whose detected group can differ from its stored one is a lookup table, because the post-pass moves it — which is how the placement bug above was found.
- No test can now distinguish the guard in `PersistGroupNames`, since the post-pass fix upholds the invariant before the write is reached. It is kept as defence at the write boundary, not claimed as covered.

## Spike first

**Does a persisted route go stale when the user drags a shape?**

This is unresolved and now sits on the main path, since every connector in Modern mode is manually routed. The shipped DSL documentation is auto-generated stubs and says nothing useful.

The spike is a small addition to `HeadlessRoutingSpikeTests`: set `ManuallyRouted = true` with known `EdgePoints`, move a shape, assert whether `EdgePoints` changed. Run it before writing `MsAglLayoutEngine`.

The answer decides the UX. If DSL keeps endpoints attached to the shape and only middle segments go wrong, staleness is tolerable and a manual re-run suffices. If the whole polyline freezes in place, Modern mode needs re-layout or targeted re-routing on shape move.

## Correction to another spec

`headless-edmx-rendering.md` open question 1 states that connectors with `ManuallyRouted="true"` store `<ConnectorPoint>` children and that "nothing in the current path reads them." That is wrong. `TranslateAssociationConnectors` and `TranslateInheritanceConnectors` read them and assign `EdgePoints`, and the translator is step 6 of the load sequence the same document describes. That open question should be closed.

## Open questions

1. **Shape-move behaviour in Modern mode.** Blocked on the spike above.
2. ~~**Does the toggle state persist?**~~ **Answered: yes.** `LayoutMode` on `TDiagram`, as the alternative in this entry proposed. Inferring it from `ManuallyRouted="true"` across every connector was rejected for the reason given: it cannot be told apart from a user hand-routing one connector.
3. ~~**No UI for editing `GroupName`.**~~ **Answered: it is on the shape's property sheet**, beside `Fill Color`, from the first release rather than later.
