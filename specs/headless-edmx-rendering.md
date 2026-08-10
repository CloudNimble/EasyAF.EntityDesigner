# Headless EDMX Rendering

Rendering EDMX diagrams to SVG, PNG and Mermaid without a running Visual Studio.

## Why this works at all

The gating question was connector routing. The exporters consume `BinaryLinkShape.EdgePoints` - `SvgConnectorRenderer.GetConnectorPathWithOffset` traces a polyline that DSL has already routed, and never routes anything itself. If DSL could not produce `EdgePoints` outside the designer, a headless renderer would have to implement orthogonal routing with obstacle avoidance.

It can. `HeadlessRoutingSpikeTests.Dsl_creates_shapes_and_routes_connectors_without_a_shell` builds a store, two entities and an association with no shell, no window and no `DiagramClientView`, and gets a real rectilinear route back:

```
Shapes: 2, Connectors: 1
EdgePoints: 3
  (1.5000, 1.5448)
  (1.5000, 3.5224)
  (4.0000, 3.5224)
```

Shape one occupies `(0.75, 0.5, 1.5 x 1.0)` and shape two `(4.0, 3.0, 1.5 x 1.0)`. The route leaves shape one's bottom edge at its horizontal midpoint, runs down and across, and enters shape two's left edge at its vertical midpoint - the same port assignment the designer produces.

## The four things that must be right

Getting any of these wrong produces an **empty diagram with no error**, which reads exactly like "DSL cannot do this headlessly". All four are encoded in `HeadlessDiagramStore`.

1. **Enable the diagram rules.** All eight ship `InitiallyDisabled=true`. `MicrosoftDataEntityDesignDomainModel.EnableDiagramRules(store)` must run before any diagram data enters the store. In Visual Studio the doc data does this.
2. **Disable the round-trip rules.** `EntityType_AddRule` and `Association_AddRule` push view model edits back into the EDMX through the editing context. With no artifact behind the store they **silently discard the additions** - `EntityTypes.Count` stays 0 and nothing throws.
3. **Give the diagram its own partition**, matching `MicrosoftDataEntityDesignSerializationHelper.CreateDiagramHelper`.
4. **Do not open serializing transactions.** The second argument to `BeginTransaction(name, isSerializing)` suppresses the fixup rules that create shapes and connectors. `DslTestHelper` passes `true` everywhere because it only builds model elements, never shapes - it is a misleading template for this work.

## Layout

| Project | Role |
|---|---|
| `Microsoft.Data.Entity.Design.Renderer` | Exporters (SVG, Mermaid, raster) and `HeadlessDiagramStore`. No UI, no shell. |
| `Microsoft.Data.Entity.Tools` | Command line front end (`edmx render`). |
| `Microsoft.Data.Entity.Tests.Design.Renderer` | Exporter and headless tests. |

Dependencies run `Tools` → `Renderer` → `EntityDesigner`. The renderer deliberately does **not** reference `Microsoft.Data.Entity.Design.Package`, which owns `PackageManager`, `VsUtils` and everything else that needs a shell. Keeping that edge absent is what makes the renderer usable from a command line.

The WPF export dialog and its orchestration (`DiagramExportHelper`) live in the package, since they are the only part of exporting that needs a user.

## Framework constraint

The Modeling SDK packages ship assemblies for .NET Framework 4.7.2 only, so the renderer and everything downstream of it are pinned to net48. `dotnet tool install` requires a .NET Core target, so `Microsoft.Data.Entity.Tools` is a plain executable rather than a packed tool. Making it a real dotnet tool would require a two process design: a net10 front end shelling out to a net48 worker.

Windows only, for the same reason.

## The load sequence

`EdmxDiagramLoader.Load` replaces `MicrosoftDataEntityDesignSerializationHelper.LoadModel`, which cannot be reused because it resolves a doc data through `PackageManager.Package` and an editing context through `DocumentFrameMgr`. The sequence is:

1. **`RenderArtifactFactory`** creates an `EntityDesignArtifact` plus a `DiagramArtifact` when a sibling `.edmx.diagram` exists. Neither existing factory works: `EFArtifactFactory` never creates the diagram artifact, so a model whose diagrams were moved out looks like it has none; `VSArtifactFactory` does, but produces shell-bound `VSArtifact` types.
2. **`VanillaXmlModelProvider`** supplies the XML. `StandaloneXmlModelProvider` takes an `IServiceProvider` only to run the extension hook through `VsUtils.GetProjectItemForDocument`; the base class does not.
3. **`DetermineIfArtifactIsRenderSafe`** replaces the full designer-safety check, which runs the runtime metadata validator and therefore demands the model's ADO.NET provider be registered on this machine. Rendering needs a conceptual model and matching namespaces, not a working database connection.
4. **`EntityModelToDslModelTranslatorStrategy`** builds the view model.
5. **`Diagram.FixUpDiagram`** is called explicitly per element, because the diagram does not exist while the translator is adding them, so the fixup rules never fire on their own.
6. **`TranslateDiagram`** applies `PointX`, `PointY`, `Width`, `IsExpanded` and `FillColor` from the EDMX and routes the connectors.

### Two shell dependencies that had to be removed

- `EntityDesignerViewModel.RegisterEventDelegates` subscribed to `PackageManager.Package.ModelManager.ModelChangesCommitted` unconditionally. It now checks `PackageManager.IsLoaded` first. **`IsLoaded`, not a null check on `Package`** - the `Package` getter asserts, which puts a modal dialog on screen in Debug builds and would hang a build agent.
- The whole family of view-to-model round trip rules is disabled in `HeadlessDiagramStore`. They route through `ViewModelChangeContext` to mutate the EDMX; with nothing to write back to they silently discard additions, and `EntityType_ChangeRule` throws a name conflict when the translator assigns an entity the name it already has.

## Status

| Format | Works headless |
|---|---|
| SVG | Yes |
| Mermaid | Yes |
| PNG, JPEG, BMP, GIF, TIFF | **No** |

Verified against a real 39 entity model (`PodSocialContext.edmx`, diagrams in a separate `.edmx.diagram` file): a 2320x2785 SVG with 191 rectangles, 113 connector paths and 711 text elements.

**Raster formats cannot work through this path.** `RasterExporter` calls `Diagram.CreateBitmap`, which resolves `SVsUIShell` and throws `Value cannot be null. Parameter name: vsUIShell` outside Visual Studio. This is DSL internals and cannot be worked around from here. The CLI rejects raster formats with a message pointing at SVG rather than surfacing the interop error.

Producing PNG headlessly means rasterising the SVG the tool already generates, which needs a rasteriser dependency - Svg.NET, or SkiaSharp with Svg.Skia. Both target net472. That is a dependency decision, not a loose end.

## Open questions

1. **Connectors with `ManuallyRouted="true"`** store `<ConnectorPoint>` children in the EDMX. Those points should be replayed rather than routed. Nothing in the current path reads them; every connector in the models tested so far is auto routed.
2. **Does the headless route match the designer's exactly?** Routes are produced and are geometrically sensible, and shape positions come from the file rather than auto layout. Whether the connector polylines are pixel identical to what the designer draws has not been checked - comparing a render against the Northwind SVG in the README would settle it.
