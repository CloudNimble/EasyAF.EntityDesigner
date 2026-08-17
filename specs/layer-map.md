# Layer map

The reference for where code goes. `platform-independence.md` says *why*; this says *where*, and what an assembly is allowed to reference.

## The rule

**A dependency pointing into a layer it does not belong in is not an option to be weighed. It is a no.**

Not a tradeoff, not "smaller diff", not "fewer moving parts". If a proposal needs the sentence "but it puts the call in the wrong place", the answer was already no and the proposal should not have been written down.

Two corollaries that are easy to miss, because both *look* like decoupling:

- **Moving a shell type down into a lower layer is not decoupling.** It relocates the dependency and adds a layer violation. `IEntityDesignDocData` carries `IVsHierarchy`; moving it into the Dsl would drag VS interop into the designer. No.
- **An interface the lower layer calls into is not decoupling either.** If the Dsl calls `IDesignerDocumentServices.GetTextForSaving(...)`, the Dsl still requires a shell at run time — the compile-time reference moved, the actual dependency did not. No.

The discriminator, applied after the change: **does the lower layer still need the upper one to function?** If yes, nothing was fixed.

## The layers

Each layer may reference only layers below it.

### 1. Foundation — no Visual Studio, no UI, no GDI

| Assembly | Holds |
|---|---|
| `Microsoft.Data.Tools.Design.XmlCore` | XML/model plumbing, `EFObject`, `EFArtifact`, `ModelManager`, `EditingContext` |
| `Microsoft.Data.Entity.Design.VersioningFacade` | EDMX version handling |
| `Microsoft.Data.Entity.Design.Model` | The EF model over XLinq: entities, associations, mappings, commands, `Model.Designer.Diagram` |

Runs anywhere. A console app, a build task, a test.

These three are exactly the contents of the solution's `/Core/` folder. That is not a coincidence and it is worth trusting: if an assembly is not in `/Core/`, it is not foundation, whatever its name suggests.

### 2. Designer — Modeling SDK, still no shell

| Assembly | Holds |
|---|---|
| `Microsoft.Data.Entity.Design.Dsl` | Domain model, shapes, connectors, `EntityDesignerSurface`, `EntityDesignerViewModel`, translation |

May reference: `Microsoft.Data.Tools.Design.XmlCore`, `Microsoft.Data.Entity.Design.Model`.

**Target state — both of these references are deleted:**

- `Microsoft.VisualStudio.Data.Entity.Design` (`PackageManager`, `VsUtils`, `Services`, dialogs, `IEntityDesignDocData`)
- `Microsoft.VisualStudio.Data.Tools.Design.XmlCore` (`VSHelpers`, `DocumentFrameMgr`, `EditingContextManager`, `XmlModelDocData`)

The second is easy to overlook because its types read like plumbing. `EditingContext` is foundation and stays; `EditingContextManager` is shell and goes.

Depends on the Modeling SDK, which is Windows-only and single threaded — the honest limit recorded in `platform-independence.md`. That is a constraint to live with, not a licence to add more.

### 3. Headless consumers

| Assembly | Holds |
|---|---|
| `Microsoft.Data.Entity.Design.Renderer` | SVG export, `EdmxDiagramLoader` |
| `Microsoft.Data.Entity.Tools` | The `edmx` CLI (net10.0-windows) |

These exist to prove layer 2 has no shell in it. When the renderer has to fork designer logic to avoid a shell call — as `EdmxDiagramLoader` forks `LoadModel` — that fork is the bug report.

### 4. Shell — Visual Studio, WinForms, WPF, GDI

| Assembly | Holds |
|---|---|
| `Microsoft.VisualStudio.Data.Tools.Design.XmlCore` | VS-flavoured plumbing: `VSHelpers`, `DocumentFrameMgr`, `EditingContextManager` |
| `Microsoft.VisualStudio.Data.Entity.Design` | All UI: dialogs, wizards, explorer, mapping details, `PackageManager`, `VsUtils`, `Services` |
| `Microsoft.Data.Entity.Design.Package` | The VSIX: package, doc data, doc view, commands, request handlers |
| `Microsoft.Data.Entity.Design.Extensibility` | Extension contracts handed to third-party authors: `ModelTransformExtensionContext` and friends |

Everything that needs a shell belongs here, and only here.

`Extensibility` reads like foundation and is not. Its contexts expose `EnvDTE.Project` and
`EnvDTE.ProjectItem`, because the extensions implementing them run inside Visual Studio and need the
project system — that is the point of the contract, not a leak in it. Nothing below the shell
references the assembly: the consumers are the package, the VS UI assembly, and tests. No headless
path goes near it, so the `EnvDTE` dependency costs the .NET 10 goal nothing.

Two things make the misfiling easy, and both are worth remembering as tells:

- `EnvDTE` is not under `Microsoft.VisualStudio.*`, so it does not show up in a namespace sweep for
  shell types. Grepping the source said this assembly was clean. Deleting the package reference and
  reading the compiler errors said otherwise. **Prefer the second method.**
- The solution folders already had it right. `Extensibility` has always sat in `/Designer/`, never in
  `/Core/`. When this document and the solution layout disagree, the solution layout is the one that
  has been maintained by people.

## Moving code out of a lower layer

In order of preference. The first three remove the dependency; the fourth relocates the code that has it.

1. **Delete it.** More of this than expected is unreachable — `EntityDesignerViewModel.SelectedEFObject` had no live callers.
2. **Push the value in.** The caller usually already has what the lower layer is reaching for. `LoadModel` recomputes the editing context that `MicrosoftDataEntityDesignDocData` is already holding.
3. **Raise an event.** For anything needing a decision from outside — a dialog, a log entry, a window. Unhandled must be a working no-op, which is what lets the renderer run with nothing subscribed.
4. **Move the code up.** When the work genuinely is shell work, the code goes to the shell. `DSLDesignerNavigationHelper.NavigateTo` walks document views and shows frames; it belongs in the Package and now lives there.

What is never on the list: adding an abstraction so the lower layer can keep making the call.

## Measuring before proposing

Count what a file actually *reaches for*, not what it imports. Repeatedly the inventory has overstated the work:

| File | Looked like | Actually was |
|---|---|---|
| `DSLDesignerNavigationHelper` | 5 VS types across the file | 2 lines telling the mapping window to follow |
| `EntityDesignerViewModel` | 3 dependencies | 1 dead property, 1 log line, 1 value it already had |

A file naming five shell types in two lines is a two-line problem.
