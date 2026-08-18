# Diagram layout engines

Swappable diagram layout, toggled from the designer's floating toolbar. The existing DSL layout stays as the default; an MSAGL-backed engine becomes the opt-in "Advanced" mode that groups related entities and persists explicit connector routes to the EDMX.

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

**It does not replace DSL routing globally.** `GraphEdge.RouteJIT` keeps handling interactive drags in the default mode. MSAGL runs when the user asks for it.

**It does not build a taxonomy of table roles.** Only reference tables are detected, because that is the one convention that holds across arbitrary schemas and has an obvious geometric payoff. Everything else is the user's to correct via `GroupName`.

## Architecture

```
LayoutEngineBase (abstract)
    Key, DisplayName
    Layout(EntityDesignerSurface surface, IList shapes)

    DslLayoutEngine      today's three-pass AutoLayoutShapeElements + Reroute, moved verbatim
    MsAglLayoutEngine    grouping -> MSAGL placement -> rectilinear routing -> persisted routes

LayoutEngineManager
    IReadOnlyDictionary<string, LayoutEngineBase>   injected, keyed
    Current { get; set; }                           driven by the toolbar toggle
```

`EntityDesignerSurface.AutoLayoutDiagram(IList shapes)` becomes a delegation to `Current.Layout(...)`. Its five existing callers are untouched:

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

### Hub affinity

Seeds groups from high-degree entities and attaches the rest by affinity. One caveat worth encoding: a **tenant root** — an entity referenced by a large fraction of the model through the same FK name — should have its edges down-weighted rather than up-weighted. Treating it as a hub pulls half the diagram adjacent to it and produces a hairball. Its presence on a table says almost nothing about where that table belongs.

## GroupName

A new optional attribute, per shape rather than per entity, so two diagrams over one model can group differently.

```xml
<EntityTypeShape EntityType="Model.Post" PointX="5" PointY="2" GroupName="Content" />
```

It mirrors `FillColor`, so it follows an existing four-part pattern rather than inventing one:

1. `TEntityTypeShape` attribute in `Microsoft.Data.Entity.Design.Edmx_3.xsd`, beside `FillColor`. The XSD already carries four of our own attributes on `TDiagram` under a *"we need to move this new value to a new XSD version"* comment, so the precedent exists.
2. `Edmx.Designer.EntityTypeShape.GroupName` as `DefaultableValue<string>` with a nested `GroupNameDefaultableValue`, plus registration in `Children` and `MyAttributeNames`.
3. A DSL domain property on `EntityTypeShape` in `DslDefinition.dsl`. Normal storage — `FillColor` uses `CustomStorage` only because of theming.
4. Translation both ways.

That fourth point is where `GroupName` and `FillColor` differ. `FillColor` flows **model → DSL only**; it is edited on the EDMX object and re-synced through `TranslateDiagramObject`. `GroupName` needs the write-back leg, so it follows `IsExpanded`'s path — a new case in `EntityTypeShapeChange`, which already does exactly this for `AbsoluteBounds → PointX/PointY` and `IsExpanded` via `UpdateDefaultableValueCommand`.

### Write-back

After grouping, any shape with **no** `GroupName` gets its detected group written to the EDMX.

An existing value is never overwritten. That is what closes the loop: the first pass persists its guess, the user edits the names by hand, and every later run takes the `Explicit` path and honours them. The guess is visible and editable in the file rather than buried in code, which is the whole point — the code's job is a good first guess across thousands of schemas of varying quality, not to be right about any one of them.

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

Advanced mode does not fall back to DSL routing. `MsAglLayoutEngine` always sets `ManuallyRouted = true` and assigns `EdgePoints`; routes are required to be persisted.

**None of the persistence is new code.** The path exists end to end:

```
AssociationConnector_ChangeRule       fires on EdgePoints + ManuallyRouted, TimeToFire.TopLevelCommit
  -> AssociationConnectorChange
    -> SetConnectorPointsCommand      writes <ConnectorPoint PointX PointY>
```

`InheritanceConnector_ChangeRule` mirrors it. Read-back exists too: `TranslateAssociationConnectors` and `TranslateInheritanceConnectors` load `ConnectorPoint`s into `EdgePoints` when `ManuallyRouted="true"`, and add the connector to `shapesToAutoLayout` when it is false.

Turning Advanced mode off is also already implemented. Setting `ManuallyRouted = false` runs `SetConnectorPointsCommand` with an empty list, stripping every `<ConnectorPoint>` and handing routing back to DSL. The toggle maps directly onto the flag.

Two consequences to accept:

- **Connectors will not reroute when the user drags a shape**, because that is what `ManuallyRouted` means. Advanced mode needs a defined answer here — see *Spike first*.
- **The EDMX grows.** A 39-entity model has roughly 113 connectors; at two to four points each that is 250–450 new elements, and a visible diff every time layout re-runs.

## The toolbar toggle

The floating toolbar already supports this. `FloatingZoomControl.xaml` has a `ToggleTemplate` with two-way `IsChecked`, `CommandTemplateSelector` dispatches on `MenuCommandDefinition.IsToggle`, and `MicrosoftDataEntityDesignDocView_PanZoom.cs` already registers `ShowGrid` and `SnapToGrid` toggles plus an `Auto Layout` button.

Adding an "Advanced Layout" toggle follows that pattern directly. Toggling it sets `LayoutEngineManager.Current`.

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

`RectilinearEdgeRouter` is the slow option — reports of it bogging down around 90 nodes. That is acceptable here precisely because Advanced mode bakes once on an explicit user action rather than routing per frame, and `EntityDesignerSurface.BeginLongOperation` already exists to report it.

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

## Spike first

**Does a persisted route go stale when the user drags a shape?**

This is unresolved and now sits on the main path, since every connector in Advanced mode is manually routed. The shipped DSL documentation is auto-generated stubs and says nothing useful.

The spike is a small addition to `HeadlessRoutingSpikeTests`: set `ManuallyRouted = true` with known `EdgePoints`, move a shape, assert whether `EdgePoints` changed. Run it before writing `MsAglLayoutEngine`.

The answer decides the UX. If DSL keeps endpoints attached to the shape and only middle segments go wrong, staleness is tolerable and a manual re-run suffices. If the whole polyline freezes in place, Advanced mode needs re-layout or targeted re-routing on shape move.

## Correction to another spec

`headless-edmx-rendering.md` open question 1 states that connectors with `ManuallyRouted="true"` store `<ConnectorPoint>` children and that "nothing in the current path reads them." That is wrong. `TranslateAssociationConnectors` and `TranslateInheritanceConnectors` read them and assign `EdgePoints`, and the translator is step 6 of the load sequence the same document describes. That open question should be closed.

## Open questions

1. **Shape-move behaviour in Advanced mode.** Blocked on the spike above.
2. **Does the toggle state persist?** `ManuallyRouted="true"` across every connector is de facto evidence Advanced mode ran, but it is ambiguous with a user hand-routing a single connector. A diagram-level attribute on `TDiagram` would be unambiguous and follows `ShowGrid` / `SnapToGrid`. Not decided.
3. **No UI for editing `GroupName`.** The first release reads and writes it in the file only; a human or an AI edits it there. A designer affordance is later scope.
