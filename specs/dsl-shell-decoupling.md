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
4. Resolve theming.
5. Give the surface a direct `EdmxDiagram` reference; drop the seven XRef lookups.
6. Rename the Model types to `Edmx*`; delete the aliases they existed to work around.
7. Rename `EntityDesignerDiagram` to `EntityDesignerSurface` via `DslDefinition.dsl` and T4 regeneration.
8. Delete the `ProjectReference`. This is the step that either compiles or does not.

Steps 1 through 5 are independently landable and each keeps the tree green.

## Risks and open items

- **T4 regeneration needs Visual Studio.** `TextTemplatingFilePreprocessor` runs on save, not at build. Step 7 cannot be done from the command line, and the generated output is checked in. The same trap already bit us: the `CodeGeneration/Generators/GeneratedCode` templates had drifted from their output during the namespace rename.
- **`EntityDesignerViewModel.RegisterEventDelegates`** subscribes to `PackageManager.Package.ModelManager.ModelChangesCommitted`, guarded by `PackageManager.IsLoaded`. It must invert to an event the Package subscribes to. The guard must stay a check on `IsLoaded`, never a null check on `Package` — the `Package` getter asserts, which puts a modal dialog on screen in Debug builds and hangs a build agent.
- **The net10 target does not run yet.** `edmx render` builds for `net10.0-windows`, starts, reaches `RenderCommand.OnExecute`, then fails with `This is a reference assembly.` A Visual Studio SDK dependency resolves to a compile-only asset under the .NET 10 TFM. This is independent of the decoupling and blocks shipping `Microsoft.Data.Entity.Tools` as a real `dotnet tool`. Unresolved.
- **Scope discipline.** The `EntityDesignerViewModel` / `ModelXRef` layer stays. Only its shell dependencies leave. Collapsing that layer is a separate question and touches every rule and every `ModelChange`.
