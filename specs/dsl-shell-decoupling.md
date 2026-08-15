# Decoupling the DSL from the Visual Studio Shell

Making `Microsoft.Data.Entity.Design.Dsl` self-contained business logic, so that loading and laying out an EDMX diagram needs no running shell and Visual Studio becomes one consumer among several.

## The end state, stated as a check

`Microsoft.Data.Entity.Design.Dsl.csproj` contains no `ProjectReference` to `Microsoft.VisualStudio.Data.Entity.Design.csproj`.

That edge exists today and points the wrong way. Deleting it is the whole job; everything below is what has to move first. A second check, weaker but useful during the work: no file under `Microsoft.Data.Entity.Design.Dsl` mentions `PackageManager`, `VsUtils`, `VSColorTheme`, `DiagramView` or any `*Dialog` type.

## Why this is tractable

The coupling is concentrated, not diffuse. 12 of 117 files in the Dsl project touch the Visual Studio layer:

| File | What it drags in |
|---|---|
| `CustomCode/Diagram/EntityDesignerDiagram.cs` | dialogs, `PackageManager`, `VsUtils`, `VSColorTheme`, watermark `LinkLabel`s, zoom, `IVsUIShell5` |
| `CustomCode/DomainClasses/EntityDesignerViewModel.cs` | `PackageManager.ModelManager`, `DocumentFrameMgr` |
| `CustomCode/Shapes/EntityTypeShape.cs`, `CustomCode/Connectors/AssociationConnector.cs`, `CustomCode/Diagram/DiagramImageHelper.cs` | `VSColorTheme`, `EnvironmentColors` |
| `CustomCode/SerializationHelper/MicrosoftDataEntityDesignSerializationHelper.cs` | doc data via `PackageManager` |
| `CustomCode/ModelChanges/{InheritanceAdd,InheritanceModelChange,AssociationModelChange,EntityType_AddFromDialog}.cs` | `ViewUtils`, live dialog instances |
| `CustomCode/Dialogs/CustomZoomDialog.cs`, `CustomCode/Diagram/DSLDesignerNavigationHelper.cs` | WinForms, explorer window |

Symbol counts across the project:

| Symbol | Uses | Symbol | Uses |
|---|---|---|---|
| `ActiveDiagramView` | 41 | `EnvironmentColors` | 5 |
| `PackageManager` | 20 | `New*Dialog` | 9 |
| `DiagramView` | 20 | `ViewUtils` | 2 |
| `VSColorTheme` | 10 | `ExplorerWindow` | 2 |
| `VSHelpers` | 9 | `VSArtifact` | 1 |
| `VsUtils` | 6 | `IVsUIShell5` | 1 |
| `DocumentFrameMgr` | 6 | `EntityDesignViewModelHelper` | 1 |

35 of the 41 `ActiveDiagramView` references are in `EntityDesignerDiagram.cs` (1889 lines). That one file is most of the problem.

## The three models

`EntityDesignerDiagram` is not a diagram. It is a *surface*. The object that represents an `<edmx:Designer><Diagrams><Diagram/>` entry already exists and is called something else:

| Layer | Type | Holds | Depends on |
|---|---|---|---|
| EDMX | `Model.Designer.Diagram` | the `<Diagram>` element: shape positions, sizes, connector points, zoom and grid settings | XLinq only |
| DSL domain model | `Dsl.ViewModel.EntityDesignerViewModel` | entity types, associations, inheritances. No geometry | Modeling SDK |
| DSL notation | `Dsl.View.EntityDesignerDiagram` | shapes, computed bounds, routed `EdgePoints`, selection, mouse | Modeling SDK |

`ModelToDesignerModelXRef` maps between them.

Two naming observations follow from this. First, every consumer renames the EDMX type on import — `using ModelDiagram = Microsoft.Data.Entity.Design.Model.Designer.Diagram;` — which is the codebase admitting the name is wrong. Second, `EntityDesignerDiagram` performs seven `ModelXRef.GetExisting(this) as ModelDiagram` lookups to reach the `<Diagram>` entry it presents, because it holds no reference to it.

A note on `ViewModel`: it is not Modeling SDK vocabulary. `EntityDesignerViewModel` is a `ModelElement` — the root **domain class**. The SDK's own split is domain model vs presentation elements (`PresentationElement`, `PresentationViewsSubject`). The name describes the layer's role in EF Tools, where the EDMX artifact model is the source of truth and the DSL model is a projection of it. That extra layer is justified — the EDMX is also edited by the XML editor, the wizards, the model browser and reverse-engineering, so the DSL store cannot own the truth — but the name has caused real confusion and is worth revisiting later. It is **not** in scope here.

## This restores the SDK's own architecture

DSL Tools' project template generates exactly two projects:

- **Dsl** — domain model, shapes, connectors, diagram, serialization, validation. No shell.
- **DslPackage** — VSIX, commands, doc data and doc view, toolbox, menus.

`Microsoft.Data.Entity.Design.Dsl` and `Microsoft.Data.Entity.Design.Package` are that pair. This work is not inventing a layering; it is restoring the one the SDK prescribes, which the project violated by admitting dialogs, `PackageManager`, `VsUtils` and theming into Dsl.

## Naming

The `Edmx` prefix removes the alias block at the top of `EntityDesignerDiagram.cs` and the `Model*`/`ViewModel*` aliases scattered elsewhere.

| From | To |
|---|---|
| `Model.Designer.Diagram` | `EdmxDiagram` — public; the API scripted against |
| `Model.Designer.Diagrams` | `EdmxDiagrams` |
| `Model.Designer.EntityTypeShape` | `EdmxEntityTypeShape` — no longer collides with the DSL's `EntityTypeShape` |
| `Model.Designer.AssociationConnector` | `EdmxAssociationConnector` |
| `Model.Designer.InheritanceConnector` | `EdmxInheritanceConnector` |
| `Model.Designer.ConnectorPoint` | `EdmxConnectorPoint` |
| `Dsl.View.EntityDesignerDiagram` | `EntityDesignerSurface` |

The last one is not a find-and-replace. `EntityDesignerDiagram` is declared in `DslDefinition.dsl` (line 441) and generated into `GeneratedCode/Diagram.cs` and `GeneratedCode/Serializer.cs`, so it requires a `.dsl` edit plus T4 regeneration. `XmlClassData` carries a separate `ElementName`, so the serialized element name in `.diagram` files can be pinned and the file format left unchanged. It lands in the same pass as the decoupling rather than afterwards, so no commit ships names and boundaries out of sync.

## Three mechanisms

### 1. Invert the dialog calls

Today `AddNewInheritance` constructs a `NewInheritanceDialog`, then hands the live dialog to `new Inheritance_AddFromDialog(dialog)` — a `ViewModelChange` that reads WPF controls inside the transaction. The DSL cannot express "add an inheritance" without a dialog existing.

The surface exposes value-taking methods — `AddInheritance(baseType, derivedType)`, `AddEntityType(...)`, `AddAssociation(...)` — and the VS layer collects the input and calls them. `EntityType_AddFromDialog`, `Association_AddFromDialog` and `Inheritance_AddFromDialog` are deleted.

`ShouldDeleteUnmappedStorageEntitySets` gets the same treatment: it currently shows `DeleteStorageEntitySetsDialog` and returns a `DialogResult`. It splits into a query the surface answers (which storage entity sets would be orphaned) and a decision the VS layer makes.

This is the change that makes the DSL scriptable, and therefore the one the headless tool depends on.

### 2. One transaction wrapper

Every mutation site repeats the same four steps: `BeginTransaction(name)`, `t.Context.Add(EfiTransactionOriginator.TransactionOriginatorDiagramId, DiagramId)`, `ViewModelChangeContext.GetNewOrExistingContext(t).ViewModelChanges.Add(change)`, `Commit()`. That collapses to a single method taking a name and a change. The `VsUtils.HourglassHelper` wrappers around several of these disappear as a side effect — a wait cursor is the caller's concern.

### 3. Give the surface its `EdmxDiagram`

Replace the seven `ModelXRef.GetExisting(this) as ModelDiagram` lookups with a property set when the surface is attached.

## What moves out

| Concern | Lines (approx) | Destination |
|---|---|---|
| Watermark: `WatermarkText`, `ResetWatermark`, `RefreshWatermarkLinks`, link handlers, `LinkAction` | 250 | Package |
| Zoom: `ZoomIn`, `ZoomOut`, `ZoomToFit`, `ZoomLevel`, `CustomZoomDialog`, `FloatingZoomControl` | 120 | Package |
| Dialog-driven add: `AddNewEntityType`, `AddNewAssociation`, `AddNewInheritance`, `AddNewFunctionImport` | 200 | Package calls the surface |
| Theming: `SetColorTheme`, `VSColorTheme.ThemeChanged`, `IsThemeServiceAvailable` | 40 | Package supplies colors |
| Drag and drop: `OnDragDrop`, `OnDragEnter`, `OnDragOver`, `OnDragLeave`, clipboard | 140 | Package |
| Context menu XAML and converters | — | Package |

Staying in Dsl, because none of it needs a shell: auto-layout, connector routing, expand/collapse, `AddMissingEntityTypeShapes`, `AddMissingAssociationConnectors`, the `Persist*` methods (they write through `CommandProcessor`, which lives in Model), selection *rules*, and shape and compartment measurement.

Theming needs a decision at implementation time: `InitializeResources(StyleSet)` is called by the DSL framework, so something must remain at that call site. The Dsl side should carry a default palette and let the Package override it; the exact mechanism is deliberately left open until the code is in front of us.

## Verification

The regression harness already exists and is stronger than any unit test written for this:

```
edmx render src/CloudNimble.EasyAF.EntityDesigner.Samples/Northwind.edmx --output out.svg
```

It exercises artifact loading, model translation, shape creation, layout and routing end to end. Current baseline: 51 rects, 36 connector paths, 152 text elements. **The SVG must stay byte-identical through every step of this work.** Any diff is a regression until proven otherwise.

Beyond that: the solution builds clean, the existing test suite passes, and the designer still opens and edits in Visual Studio 2026.

## Sequencing

1. Add the transaction wrapper and convert existing call sites. No behaviour change, no moves.
2. Invert the dialog calls; delete the three `*_AddFromDialog` classes. Dialogs move to Package.
3. Move the watermark, zoom, drag-and-drop and context menus to Package.
4. ~~Resolve theming.~~ **Done.** The Dsl declares a `DiagramPalette` of semantic colors with defaults and a `DiagramTheme` the host pushes into; `Microsoft.Data.Entity.Design.Package` owns `VsDiagramTheme`, which reads Visual Studio's colors and subscribes to `VSColorTheme.ThemeChanged` with a matching unsubscribe on dispose. `IsThemeServiceAvailable` is deleted. This closed the three net10 renderer failures and made the .NET 10 `edmx render` work, byte-identical to net48.
5. Give the surface a direct `EdmxDiagram` reference; drop the seven XRef lookups.
6. Rename the Model types to `Edmx*`; delete the aliases they existed to work around.
7. Rename `EntityDesignerDiagram` to `EntityDesignerSurface` via `DslDefinition.dsl` and T4 regeneration.
8. Delete the `ProjectReference`, and consolidate the UI. See below.

Steps 1 through 5 are independently landable and each keeps the tree green.

## Step 8 in detail

Two things happen together, because neither can happen alone:

- The Dsl stops referencing `Microsoft.VisualStudio.Data.Entity.Design`.
- The UI still sitting in the Dsl moves into `Microsoft.VisualStudio.Data.Entity.Design`, where the bulk of the UI already lives.

**The order is forced.** The UI in the Dsl needs Dsl types — `DiagramSurfaceContextMenu` holds an `EntityDesignerSurface`, `CustomZoomDialog` uses the Dsl's own resources — so its new home must be able to reference the Dsl. That cannot happen while the Dsl references it. Cut the edge first, reverse it, then move the UI. Attempting the move first produces a circular reference; this was tried.

### What the Dsl still takes from the VS project

Measured, not estimated. Thirteen files, and the `Resources` hits in several of them are a false positive — that is the Dsl's own `Properties.Resources` behind the `EntityDesignerRes` alias.

| File | Real dependency | Fix |
|---|---|---|
| `Shapes/EntityTypeShape.cs` | **none** — the using is stale | delete the using |
| `Connectors/AssociationConnector.cs` | `ReferentialConstraintDialog` | event |
| `Diagram/DiagramImageHelper.cs` | `ThemeUtils` (GDI rasterization) | move icons to the shell — `unified-theming.md` item 2 |
| ~~`Diagram/DSLDesignerNavigationHelper.cs`~~ | ~~`MappingDetailsWindow`, `MappingDetailsInfo`, `EntityMappingModes`, `PackageManager`, `Services`~~ | **Done.** Split, not moved — see below |
| `Diagram/EntityDesignerSurface.cs` | `NewEntityDialog`, `NewAssociationDialog`, `NewInheritanceDialog`, `DeleteStorageEntitySetsDialog`, `PackageManager`, `Services`, `VsUtils`, `VSArtifact`, `EdmUtils`, `EntityDesignViewModelHelper`, `IEdmPackage`, `IViewDiagram` | events for the dialogs; watermark, zoom and drag-drop already leave in step 3; `IViewDiagram` moves *into* the Dsl |
| `DomainClasses/EntityDesignerViewModel.cs` | `PackageManager`, `Services`, `VsUtils` | push the model manager in; the `IsLoaded` guard disappears with it |
| `SerializationHelper/...SerializationHelper.cs` | `IEntityDesignDocData`, `PackageManager`, `VsUtils` | event: the designer asks for the document's current text, the shell answers |
| `ModelChanges/EntityType_AddFromDialog.cs`, `AssociationModelChange.cs`, `InheritanceModelChange.cs`, `InheritanceAdd.cs` | the dialogs, `ViewUtils` | delete the three `*_AddFromDialog` classes; `ViewUtils.SetBaseEntityType` inverts |

### Navigation splits rather than moving

The first read said "move the whole file out". That was wrong, and worth recording as a pattern. `DSLDesignerNavigationHelper` was two jobs sharing a file:

- **Which window?** — walk the active document view, then every open view for the artifact, ask each to navigate, show the first frame that matched. Pure shell. This is now `Package/CustomCode/Navigation/DesignerNavigator.NavigateTo`.
- **Which shape?** — resolve an m-space object to its nearest c-space object, walk the model to a shape, build a `DiagramItemCollection`, set the selection. That is 400 lines of designer logic that only *looked* shell-coupled, because of two lines reaching into `PackageManager` for the editing context. It stays in the Dsl as `DiagramNavigator.NavigateToNodeInDiagram`.

Both VS touches came from the same place — telling the mapping details window to follow along — and both became one `MappingDetailsNavigationRequested` event. The mode-setting is the interesting half: the designer used to write `EntityMappingModes.Functions`/`Tables` directly into the shell's context. It now reports `bool? UsesFunctionMapping` instead, a fact about the model. Which tab that corresponds to, or whether the host has tabs at all, is the host's business. `null` preserves the original behaviour of leaving the existing choice alone on the association-set-mapping path.

The lesson for the remaining files: count what a dependency actually *reaches for*, not how many symbols it names. A file that mentions five VS types in two lines is a two-line problem.

### `IViewDiagram` moves the other way

`IViewDiagram` is declared in the VS project, **implemented** by `EntityDesignerSurface`, and consumed by `IDiagramManager` and the package. A base type cannot point back at the shell, so the interface moves into the Dsl. The VS project then references the Dsl to see it — which is the direction this whole exercise establishes.

### Then the UI moves

Once the edge is reversed, these move to `Microsoft.VisualStudio.Data.Entity.Design`, keeping folder and namespace aligned as the rest of that project does:

| From (Dsl) | To (VS project) |
|---|---|
| `CustomCode/ContextMenu/` — 10 files | `UI/Views/ContextMenu/` |
| `CustomCode/Controls/FloatingZoomControl.*` | `UI/Views/Controls/` |
| `CustomCode/Dialogs/CustomZoomDialog.*` | `UI/Views/Dialogs/` |

Nothing outside those three folders references any of it, so the move itself is mechanical. Watch the `.resx`: `CustomZoomDialog` is a WinForms designer resource, and its manifest name derives from root namespace plus folder path while `ComponentResourceManager` looks it up by the type's full name. Those two must agree, or the dialog throws `MissingManifestResourceException` at runtime and nothing catches it at compile time. This exact mistake is what broke the ModelWizard pages.

### Done when

`Microsoft.Data.Entity.Design.Dsl.csproj` contains no `ProjectReference` to `Microsoft.VisualStudio.Data.Entity.Design.csproj`, the solution builds, and `edmx render Northwind.edmx` is byte identical on both targets.

## Risks and open items

- **T4 regeneration works from the command line, but the output no longer compiles.** This was checked rather than assumed. `msbuild Microsoft.Data.Entity.Design.Dsl.csproj -t:TransformAll` regenerates all 16 templates from `DslDefinition.dsl` with zero errors, once four things are wired up (now in the csproj): the `Microsoft.TextTemplating.targets` import, a `DirectiveProcessor` item naming `Microsoft.VisualStudio.Modeling.DslDefinition.DslDirectiveProcessor`, `IncludeFolders` pointing at the DSL SDK's `TextTemplates`, and `VsIdePath` so the transform can resolve the Modeling SDK from `PrivateAssemblies`. `DslTemplatesSrc` must be set as a real **environment variable** — the template host expands it inside include directives, and MSBuild's property function allowlist blocks setting it from the project. A guard target fails fast when it is missing.

  The catch: the checked-in generated code was produced by an older DSL SDK, and VS 18's templates emit materially different code — 475 insertions and 518 deletions ignoring whitespace, with `Serializer.cs` alone changing 516 lines. The regenerated tree **does not build**: `MicrosoftDataEntityDesignSerializationHelper.InternalSaveModel` overrides a base member that the new templates no longer mark virtual. So step 7 is not "run a command"; it is "adopt VS 18's generated code and deal with the fallout", and the serializer diff is large enough to need its own review against the Northwind render. Sequence it accordingly, or pin the rename to a targeted hand edit of the four affected generated files instead.

- **`TransformAll` overwrites outputs even for templates that fail.** Regenerate only from a clean tree, and check `git diff` before trusting the result.
- **`EntityDesignerViewModel.RegisterEventDelegates`** subscribes to `PackageManager.Package.ModelManager.ModelChangesCommitted`, guarded by `PackageManager.IsLoaded`. It must invert to an event the Package subscribes to. The guard must stay a check on `IsLoaded`, never a null check on `Package` — the `Package` getter asserts, which puts a modal dialog on screen in Debug builds and hangs a build agent.
- **The net10 blocker is caused by the coupling this work removes.** `edmx render` on `net10.0-windows` fails with `This is a reference assembly.` The renderer tests pin it down exactly:

  ```
  System.InvalidOperationException: This is a reference assembly.
     at Microsoft.VisualStudio.Shell.ThreadHelper.CheckAccess()
     at Microsoft.VisualStudio.Shell.Package.GetGlobalService(Type serviceType)
  ```

  That is `IsThemeServiceAvailable()` in the surface — `VSPackage.GetGlobalService(typeof(SVsUIShell))` — reached from `SetColorTheme` via the `InitializeResources` override. `Microsoft.VisualStudio.Shell` resolves to a compile-only asset under .NET 10, so merely *touching* it throws.

  Moving theming out (work item 4 above) should therefore also unblock the real `dotnet tool`, because nothing on the headless path would call into `Microsoft.VisualStudio.Shell` at all. Treat that as a hypothesis to confirm, not a promise: 135 of 138 renderer tests already pass on net10, and the 3 failures are all this one call chain.
- **Scope discipline.** The `EntityDesignerViewModel` / `ModelXRef` layer stays. Only its shell dependencies leave. Collapsing that layer is a separate question and touches every rule and every `ModelChange`.
