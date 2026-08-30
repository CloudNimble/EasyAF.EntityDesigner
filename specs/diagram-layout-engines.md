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

### Grouping and name generation are opt-in

Grouping was originally always on in Modern mode, and it always wrote names back. On a real diagram that made the arrangement worse rather than better and filled the file with guessed names nobody asked for. Both are now separate `Diagram` settings, each a `bool` shown in the property window in Modern mode, and **both default to off**:

- **`EnableGrouping`** ("Grouping") — whether the layout clusters shapes at all.
- **`GenerateGroupNames`** ("Generate Group Names") — whether the layout writes a detected group name back onto a shape that has none.

They are independent switches, but name generation is subordinate to grouping. The rules, exactly:

1. **Grouping off → no clusters and no generation, whatever `GenerateGroupNames` says.** Off means the arrangement is ungrouped; a generator with nothing to generate for is not run. So `GenerateGroupNames` on while `EnableGrouping` is off generates nothing.
2. **Grouping on, generation off → cluster by names already in the file only.** `GroupingLayout.GroupByExisting` maps just the shapes that carry a `GroupName`; the rest are laid out free. Detection (colour/hub/structural) does not run and nothing is written back. This is how a user groups strictly by names they typed.
3. **Grouping on, generation on → detect, cluster, and write back.** The full `GroupingLayout.Group` runs and `PersistGroupNames` records a name for every shape that had none.
4. **Clearing group names obeys the same gate.** The clear button removes the attributes and re-lays out (clearing is a layout input). Regeneration only happens if *both* switches are on; with grouping off, or generation off, the names stay cleared.
5. **An existing value is never touched when generation is on.** Unchanged from *Write-back* above — `PersistGroupNames` writes only shapes with no name, and the reference-table post-pass skips named shapes.

The gate lives in `MsAglLayoutEngine.Layout`, which reads `EntityDesignerSurface.EnableGrouping` / `.GenerateGroupNames` and either calls `GroupingLayout.Group` (+ `PersistGroupNames`), calls `GroupByExisting`, or passes no groups at all. Both attributes are in `DiagramLayoutInput`, so toggling either one re-runs the layout.

### Property order

`LayoutMode` should read first, since it decides whether the other three (`ConnectorMode`, `EnableGrouping`, `GenerateGroupNames`) mean anything. The stock Visual Studio Properties window feeds off `ICustomTypeDescriptor` and sorts alphabetically both by category and, within a category, by display name — the descriptor's own order is ignored — so nothing can move a property ahead of its alphabetical neighbours *inside* a category.

The fix is to split the category rather than fight the sort. `LayoutMode` sits alone in **Layout**; the other three sit in **Modern Layout Options**. Categories sort alphabetically too, and "Layout" precedes "Modern Layout Options", so Layout Mode reads first and the modern-only knobs follow as a labelled block. This also reads better: the block is exactly the settings that do nothing until `LayoutMode` is Modern, and its name says so.

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

`MsAglLayoutEngine` assigns `EdgePoints` on every connector it routes so the routes are persisted. It originally also set `ManuallyRouted = true` on all of them; that was wrong and is superseded by *Connector routing and provenance* below — the flag now means "a human authored this route" and the engine never sets it.

**None of the persistence is new code.** The path exists end to end:

```
AssociationConnector_ChangeRule       fires on EdgePoints + ManuallyRouted, TimeToFire.TopLevelCommit
  -> AssociationConnectorChange
    -> SetConnectorPointsCommand      writes <ConnectorPoint PointX PointY>
```

`InheritanceConnector_ChangeRule` mirrors it. Read-back exists too: `TranslateAssociationConnectors` and `TranslateInheritanceConnectors` load `ConnectorPoint`s into `EdgePoints` when `ManuallyRouted="true"`, and add the connector to `shapesToAutoLayout` when it is false.

Turning Modern mode off is also already implemented. Setting `ManuallyRouted = false` runs `SetConnectorPointsCommand` with an empty list, stripping every `<ConnectorPoint>` and handing routing back to DSL. The toggle maps directly onto the flag.

One consequence to accept:

- **The EDMX grows.** A 39-entity model has roughly 113 connectors; at two to four points each that is 250–450 new elements, and a visible diff every time layout re-runs.

## Connector routing and provenance

The legacy designer's worst routing bug: hand-fix a bad route, and it comes back on reopen. The cause is that `ManuallyRouted` was overloaded to mean two things — *provenance* ("a human authored this route") and the *SDK directive* ("don't JIT-route this link"). `EntityTypeShape_ChangeRule` cleared the directive whenever a shape moved, and that clear flowed to `ManuallyRoutedChange`, which strips the persisted `ConnectorPoints` and writes `ManuallyRouted="false"`. A transient "recalc because geometry moved" destroyed the durable "a human authored this".

**The principle: `ManuallyRouted` is provenance, and provenance is sacred.**

- Set **true** only by a human dragging a connector. Set **false** only by an explicit user reset. No shape move, collapse, expand, auto-layout, or engine bake ever changes it.
- On load, a connector with `ManuallyRouted="true"` and points is restored verbatim and never auto-routed. This already works; the bug was upstream, in the clearing.

### The rules

1. **Never auto-clear.** `EntityTypeShape_ChangeRule` no longer sets `link.ManuallyRouted = false` on shape move / expand. This one change fixes the reported bug: a hand-fixed route now survives reopen.
2. **Reopen verbatim.** Hand-dragged connectors load exactly as saved; the rest are drawn by the active engine (Legacy: SDK JIT; Modern: MSAGL).
3. **Move / collapse keeps the route hand-routed.** When a shape moves, the Modeling SDK re-routes the connector to track the shape but **keeps `ManuallyRouted` set** — it is never reverted to an auto-route. (Implementation note: the SDK does the endpoint tracking itself; an earlier plan to hand-translate the endpoint was removed once testing showed the SDK already re-routes a still-`ManuallyRouted` link on move. The interior bends are the SDK's to keep or rebuild; what the fix guarantees is that the connector stays hand-routed and reopens to its saved points.)
4. **Persistence is the normal flow.** Every write above goes through the in-memory model → dirty → explicit VS Save. Nothing writes the `.edmx` directly, and the headless renderer never saves. (Verified: the only direct `.Save` calls are SVG/PNG/Mermaid *output* and the one-time `MigrateDiagramInformationCommand`.)

Rules 1–4 are implemented. Two further behaviours — the engine skipping hand-routed connectors on a re-bake, and the re-bake dialog — are **not yet built**, because they turn out to require the *ephemeral engine routes* change below rather than a small addition.

### Ephemeral engine routes (not yet built)

`ManuallyRouted` is strictly human. That has a consequence: an engine-baked route cannot be persisted as `ManuallyRouted=true` (that would be a lie the engine-skip and the dialog then can't see through), and it cannot be persisted as `ManuallyRouted=false` with points either (the loader ignores points when the flag is false). So **engine routes are not persisted at all — they are recomputed on open.** Only human routes persist. This is the design the "strictly human" decision forces, and it has a nice side effect: the `.edmx` stops carrying hundreds of engine `ConnectorPoint`s and no longer churns on every layout.

The unit of work:

- A **persist guard**: connector changes made *during a layout* are view-only and never persisted (so an engine bake leaves the persisted flag `false`). The connector change rules check the surface's "laying out" state.
- **Recompute on open**: a Modern diagram routes its non-human connectors when it loads (VS and the headless renderer both), keeping the persisted shape positions — so this needs a route-only pass, not a full re-layout that would move shapes.
- **Engine skip**: with the persisted flag now honestly human-only, `MsAglLayoutEngine` skips connectors whose persisted `ManuallyRouted` is true. (This was drafted and backed out — it is incoherent until the guard and recompute above exist, because today the engine still marks its own routes manual.)
- **Re-bake dialog**: with hand-routed connectors identifiable, a Modern re-bake asks (in VS) whether to keep or clear them, remembering the answer in `ManualRouteRebakePolicy` (`Ask`/`Keep`/`Clear`, default `Ask`; headless treats `Ask` as `Keep` and never writes). The attribute and `RebakePolicy` enum are already in place.

Most of this is VS-transaction behaviour that can only be verified in the running VSIX, so it is its own unit rather than part of the provenance fix.

OOB interop was dropped as a goal: the stock EF6 designer can't render a `Microsoft.Data.SqlClient` model at all (provider validation fails → XML editor), so keeping engine routes stock-compatible protects nobody. That is what lets `ManuallyRouted` mean one honest thing instead of two.

### Implementation checklist

- [x] `EntityTypeShape_ChangeRule` — removed the `ManuallyRouted = false` loop; still emits the position `EntityTypeShapeChange`. **This is the fix.**
- [x] ~~Endpoint re-attach~~ — **not needed.** Testing showed the SDK already re-routes a still-`ManuallyRouted` link to track a moved shape; a hand-written translation only fought it and was removed.
- [x] `Diagram` + `Edmx_3.xsd` — added `ManualRouteRebakePolicy` (`Ask`/`Keep`/`Clear`, default `Ask`) and the `RebakePolicy` enum. *(Plumbing in place; consumed by the re-bake dialog in the ephemeral-routes unit.)*
- [x] Tests — provenance survives move / resize; auto stays auto; load keeps `ManuallyRouted`; malformed XML (`ManuallyRouted="false"` with points → treated as auto; `"true"` with none → drawable fallback, no crash).
- [ ] **Ephemeral engine routes (own unit, mostly VS):** persist guard, recompute-on-open (route-only), engine skip, re-bake dialog. See *Ephemeral engine routes* above. Decided (forced by "strictly human"); not yet built.

## Redrawing a single connector (implemented)

Right-clicking an association gives a **Redraw Route** item that re-routes just that one connector through the active engine, moving nothing else. It exists for the common annoyance the property-window toggle does not cover: a connector whose route paints badly and that the user wants the engine to redo in place.

`LayoutEngineBase` gained a placement-free counterpart to `Layout`:

```
RouteConnectors(EntityDesignerSurface surface, IList connectors)   // route these in place, move no shape
```

- **MSAGL**: builds a graph with *every* shape as a fixed obstacle at its current position and edges only for the named connectors, runs `RectilinearEdgeRouter` (no `CalculateLayout`, so nothing is placed), and writes each edge back. It reuses `ApplyConnectorRoutes`, which now takes `markManuallyRouted` — a full layout owns its routes and passes `true`; a redraw passes **`false`** and leaves the flag exactly as it found it. Coordinates map designer→MSAGL as `(x·scale, −y·scale)`, so reading the result back through the `origin=(0,0)` mirror returns points in the shapes' own coordinates (no shift, because shapes don't move).
- **DSL/Legacy**: freezes every shape (`SaveLayoutFlags` + `NoMoveShapeFlags`) and asks the SDK to re-route just those links at right angles. A connector the user hand-routed stays as the SDK leaves it — Legacy has no notion of overriding a frozen route.

`EntityDesignerSurface.RerouteConnectors(IList)` is the entry point, mirroring `AutoLayoutDiagram` (same `_isLayingOut` guard). The context-menu handler calls it with the one connector.

**The flag is deliberately left alone.** A redraw only repaints; whether the route is a human's or the engine's is not its call to change, and the in-memory view model owns what is painted (persistence follows the normal dirty→Save path). This is the behavior the user asked for: "if I'm clicking a route to fix its painting, the flag is likely already true — just don't touch it." Headless tests (`MsAglLayoutEngineTests`) hold both halves: shapes never move, and the flag comes back exactly as it went in (mutation-checked — stamping it fails the test).

## How a property edit reaches the diagram (the two-model round-trip)

This is the plumbing every property-window change rides, routing included. It has been re-derived from the code several times; this is the authoritative writeup. Verify against the cited `file:symbol` before trusting it — the code is the source of truth.

### Two models, one bridge

There are two parallel object graphs, and almost everything here is about keeping them in step:

- **The EDMX designer model** — `Microsoft.Data.Entity.Design.Edmx.*`. Plain objects over the XLinq tree (`EFObject`/`EFElement`), e.g. `Edmx.Entity.Association`, and the diagram shapes `Edmx.Designer.EntityTypeShape` / `AssociationConnector` / `InheritanceConnector`. Attributes are `DefaultableValue<T>` (`Connector.ManuallyRouted`, `EntityTypeShape.FillColor`/`GroupName`). This is what gets serialized to the `.edmx`.
- **The DSL view model** — `Microsoft.Data.Entity.Design.Diagrams.*`, living in a Modeling-SDK `Store`. Model elements (`Diagrams.ViewModel.Association`, `Diagrams.ViewModel.EntityType`) plus presentation/shape elements (`Diagrams.View.EntityTypeShape`, `Diagrams.View.AssociationConnector` — a `LinkShape` — and `EntityDesignerSurface`, the diagram). This is what draws on screen.

**The bridge is `ModelToDesignerModelXRef`** (`CustomSerializer/ModelToDesignerModelXRef.cs`), stored as a `ContextItem` on the `EditingContext` and reachable as `viewModel.ModelXRef`. It maps EDMX `EFObject` ⇄ DSL `ModelElement`, **keyed per `Partition`**. Each open diagram is one `Partition` = one `EntityDesignerViewModel` = one `DiagramId`. So `xref.GetExisting(efObject)` returns *a list* — one DSL element per diagram the object appears on — and `xref.GetExisting(dslElement)` returns the single EDMX object. Both connectors and shapes are registered here (`EntityModelToDslModelTranslatorStrategy` calls `ModelXRef.Add(modelAssociationConnector, dslAssociationConnector, …)`).

### Active diagram

Multiple diagrams can be open. Each `EntityDesignerViewModel` carries a `DiagramId`; the **active** one is the view model whose `GetDiagram().ActiveDiagramView` is non-null. Model→DSL translation is gated on `modelDiagramObject.Diagram.Id == DiagramId` (`EntityDesignerViewModel.cs` ~line 487 and `ProcessSingleDiagramModelChange` ~line 631), so a change to one diagram's shape never redraws another's. When code has only an EDMX object and needs "the connector the user is looking at", it resolves through the active partition.

### Property window: selection → descriptor

`MicrosoftDataEntityDesignDocView.ConvertDslModelElementArrayToItemDescriptors` turns the DSL selection into descriptors:

- It takes the selected presentation element's `.ModelElement` (the DSL model element) and calls `XRef.GetExisting(dslElem)` to get the EDMX object, then `PropertyWindowViewModel.GetObjectDescriptor(efObject, …)`.
- `PropertyWindowViewModel.ObjectDescriptorTypes` maps EDMX type → descriptor: `Association → EFAssociationDescriptor`, `EntityTypeShape → EFEntityTypeShapeDescriptor`, `Diagram → EFDiagramDescriptor`, …
- **Entity shapes are special-cased**: for a shape the code resolves `XRef.GetExisting(presElem)` (the shape itself), so `EntityTypeShape` reaches `EFEntityTypeShapeDescriptor` and can show shape-only properties (`FillColor`, `GroupName`).
- **A selected connector resolves to its `Association`, not the connector shape.** `presElem.ModelElement` for a DSL `AssociationConnector` is the DSL `Association`, so `EFAssociationDescriptor` is shown. The connector shape (which holds `ManuallyRouted`) is *not* the descriptor's object — the descriptor reaches it via the xref/active-diagram when it needs it.
- Whatever lands here also becomes `EntityDesignerSelection.PrimarySelection`, which the command layer reads as `SelectedEFObject` and acts on via `SelectedEFObject.XObject` (`MicrosoftDataEntityDesignCommandSet.cs`). **This is why you don't casually redirect connector selection to the connector shape** — Delete/Rename would then target the `<Connector>` node instead of the association.

### Descriptor edits are EDMX-model commands, and stay in memory until Save

Descriptor setters do **not** poke the DSL. They run a command against the EDMX object through `PropertyWindowViewModelHelper.GetCommandProcessorContext()`:

```csharp
CommandProcessor.InvokeSingleCommand(cpc, new UpdateDefaultableValueCommand<Color>(shape.FillColor, value));
```

That change dirties the document and is only written to disk on an explicit VS **Save**. Nothing in this path touches the `.edmx` file directly — the in-memory model *is* the working copy. (Same guarantee the routing sections rely on.)

### The round-trip, both directions

**DSL → EDMX model** (user drags a shape/connector). SDK change rules fire on commit and translate the view change into EDMX commands:

```
EntityTypeShape_ChangeRule (LocalCommit)      shape moved/resized/expanded
AssociationConnector_ChangeRule (TopLevelCommit)   EdgePoints / ManuallyRouted changed
  -> ViewModelChangeContext.ViewModelChanges.Add(new …Change(dslElem, propId))
     AssociationConnectorChange.StaticInvoke:
       ManuallyRouted flipped false  -> SetConnectorPointsCommand(model, empty)   // strips <ConnectorPoint>s
       always                        -> UpdateDefaultableValueCommand<bool>(model.ManuallyRouted, dsl.ManuallyRouted)
       EdgePoints changed & routed   -> SetConnectorPointsCommand(model, points)
```

**EDMX model → DSL** (a command changed the model — from the property window, undo, update-from-DB, or the DSL→model leg above). `EntityDesignerViewModel.OnModelChangesCommitted` → `ProcessModelChanges` walks every committed `EfiChange`, and for each diagram object in *this* diagram calls `EntityModelToDslModelTranslatorStrategy.TranslateDiagramObject`, which dispatches to:

- `TranslateAssociationConnectors` / `TranslateInheritanceConnectors`: set `dslConnector.ManuallyRouted = model.ManuallyRouted.Value`; **if the flag is false or there are no points, add the connector to `shapesToAutoLayout`**; otherwise load the persisted `ConnectorPoint`s into `EdgePoints`.
- shape translators: push `FillColor`, bounds, etc. onto the DSL shape.

### Routing is the engine's job, reached from here

`ProcessModelChanges` finishes by handing the queued shapes to the layout **engine**, never to DSL line-drawing:

```
ProcessModelChanges  ->  diagram.AutoLayoutDiagram(shapesToAutoLayout)     // or AutoLayoutDiagram() when a layout input changed
                          -> LayoutManager.Resolve(LayoutMode).Layout(surface, shapes, ConnectorMode)   // LayoutEngineBase override
```

So a connector whose `ManuallyRouted` becomes false is re-routed by the active engine (`DslLayoutEngine` or `MsAglLayoutEngine`) on the next translate, because it was queued into `shapesToAutoLayout`. `DiagramLayoutInput.Includes` decides which model changes trigger a *full* re-layout vs. a targeted one, and `AutoLayoutDiagram` no-ops re-entrant calls (`_isLayingOut`) so a layout's own write-backs don't recurse.

### `ManuallyRouted` in the property window (implemented)

Selecting a connector shows `EFAssociationDescriptor`; it now carries a **Routing → Manually Routed** boolean. The path needs no selection redirect and no new DSL code:

1. `EFAssociationDescriptor.ResolveActiveConnector()` maps the bound `Association` to the `Edmx.Designer.AssociationConnector` on the **active diagram**. It walks `ModelToDesignerModelXRef.GetExisting(context, association)` (one DSL element per open diagram), takes each connector's presentation shape (`PresentationViewsSubject.GetPresentation`), and picks the one whose surface has a live `ActiveDiagramView` — falling back to the sole candidate when a single diagram is open. Multi-diagram stays unambiguous because the connector identity comes from the shape, not a search of the model's diagrams.
2. The getter reads that connector's `ManuallyRouted.Value`. `IsBrowsableManuallyRouted()` hides the property when no connector resolves (e.g. the association selected in the Model Browser).
3. The setter writes through the standard command path, so everything is in-memory/dirty until Save and `Association` stays the selected object (no Delete/Rename regression):
   - **Off** → `SetConnectorPointsCommand(connector, [])` + `UpdateDefaultableValueCommand<bool>(…, false)`. Clearing the points mirrors the drag path's `AssociationConnectorChange`; the model→DSL translate (`TranslateAssociationConnectors`) then queues the connector into `shapesToAutoLayout` and `AutoLayoutDiagram` re-routes it **through the engine**.
   - **On** → persists the connector's *current* `EdgePoints` as `ConnectorPoint`s, then sets the flag true — so "manually routed" is always a real saved route, never the empty-points fallback. (This is the "pin the current route" direction; the feature the user asked for is Off.)

This is VS-only: it needs a live `ActiveDiagramView` and the property-window command context, so it is verified by building and exercising it in the running VSIX, not headlessly. Resources: `PropertyWindow_Category_Routing`, `PropertyWindow_DisplayName_ConnectorManuallyRouted`, `PropertyWindow_Description_ConnectorManuallyRouted`.

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

## Shape-move staleness

**Does a persisted route go stale when the user drags a shape?** Yes — a `ManuallyRouted` link's `EdgePoints` are absolute, so the SDK freezes them and the endpoint detaches from the moved shape. That is exactly why the legacy code cleared the flag on move (and lost the route as a result).

The answer, per *Connector routing and provenance*: the flag is left true, and the SDK re-routes the connector to track the shape on its own — testing showed a still-`ManuallyRouted` link is re-routed by the SDK on move, so no hand-written endpoint translation is needed (an early attempt at one was removed for fighting the SDK). What the fix guarantees is that the connector stays hand-routed and reopens to its saved points; the legacy defect was the flag being cleared on move, not the SDK's re-routing.

## Correction to another spec

`headless-edmx-rendering.md` open question 1 states that connectors with `ManuallyRouted="true"` store `<ConnectorPoint>` children and that "nothing in the current path reads them." That is wrong. `TranslateAssociationConnectors` and `TranslateInheritanceConnectors` read them and assign `EdgePoints`, and the translator is step 6 of the load sequence the same document describes. That open question should be closed.

## Open questions

1. ~~**Shape-move behaviour in Modern mode.**~~ **Answered:** provenance is sacred (`ManuallyRouted` never auto-cleared); manual routes re-attach their endpoint on move and keep their interior; the Modern engine skips them; a re-bake honours `ManualRouteRebakePolicy`. See *Connector routing and provenance*.
2. ~~**Does the toggle state persist?**~~ **Answered: yes.** `LayoutMode` on `TDiagram`, as the alternative in this entry proposed. Inferring it from `ManuallyRouted="true"` across every connector was rejected for the reason given: it cannot be told apart from a user hand-routing one connector.
3. ~~**No UI for editing `GroupName`.**~~ **Answered: it is on the shape's property sheet**, beside `Fill Color`, from the first release rather than later.
