# Rename catalog

The working checklist for the container rename. `layer-map.md` says what each project is *for*;
this says what physically has to change and what will not tell you when you get it wrong.

Generated data lives in `_rename-string-hits.md` alongside this file — 142 rows, regenerate it
whenever the tree moves.

## Order of operations

Containers first, then seats, then names. Do not interleave.

1. **Check in current code.** Done — `5b32702e`.
2. **Rename projects and project folders.** `.csproj` filename, containing folder, `<AssemblyName>`,
   `<RootNamespace>` — all four in the same commit, per project.
3. **Fix the solution file** (`EasyAF.EntityDesigner.slnx`) paths and folder assignments.
4. **Namespaces**, via Roslyn analyzers in Visual Studio, one project at a time.
5. **Type renames, XAML URIs, resource paths**, one project at a time, behind step 4.

The rule that keeps this from recreating the mess it is fixing: **assembly name and root namespace
change together, in the same commit.** The current tree's worst defect is that they diverged — see
the namespace-drift table below.

## Container map

| Current | Becomes | Layer |
|---|---|---|
| `Microsoft.Data.Tools.Design.XmlCore` | `Microsoft.Data.Entity.Design.XmlEngine` | Core |
| `Microsoft.Data.Entity.Design.Model` | `Microsoft.Data.Entity.Design.Edmx` | Core |
| `Microsoft.Data.Entity.Design.VersioningFacade` | `Microsoft.Data.Entity.Design.EntityFramework` | Core |
| `Microsoft.Data.Entity.Design.Dsl` | `Microsoft.Data.Entity.Design.Diagrams` | Core |
| `Microsoft.Data.Entity.Design.Renderer` | `Microsoft.Data.Entity.Design.Diagrams.Rendering` | Output |
| `Microsoft.Data.Entity.Design.DatabaseGeneration` | *unchanged* | Output |
| `Microsoft.VisualStudio.Data.Entity.Design` | `Microsoft.VisualStudio.Data.Entity.EdmxDesigner` | Shell |
| `Microsoft.VisualStudio.Data.Tools.Design.XmlCore` | `Microsoft.VisualStudio.Data.Entity.XmlDesigner` | Shell |
| `Microsoft.Data.Entity.Design.Extensibility` | `Microsoft.VisualStudio.Data.Entity.Extensibility` | Shell |
| `Microsoft.Data.Entity.Design.Package` | `Microsoft.VisualStudio.Data.Entity.Package` | Shell |
| `Microsoft.Data.Entity.Tools` | `CloudNimble.EasyAF.Edmx.Diagrams.Tools` | Deliverable |

The prefix carries the layer: `Microsoft.Data.Entity.Design.*` runs anywhere,
`Microsoft.VisualStudio.Data.Entity.*` needs a shell.

Test projects follow their subject and are not listed. `Microsoft.VisualStudio.TestTools.VsIdeTesting`
is test infrastructure and does not move.

## Namespace drift — what step 4 is actually repairing

Not one project currently has a root namespace matching its assembly name. Counts are files.

| Project | Namespace roots present today |
|---|---|
| `Data.Tools.Design.XmlCore` | `Microsoft.Data.Entity.*` (131), `Microsoft.Data.Tools.*` (34) |
| `Design.Model` | `Microsoft.Data.Entity.*` (339), `System.Data.Entity.*` (2), `Microsoft.Data.Tools.*` (2) |
| `Design.VersioningFacade` | `Microsoft.Data.Entity.*` (41) |
| `Design.Dsl` | `Microsoft.Data.Entity.*` (154), `XamlGeneratedNamespace` (2), `Microsoft.Data.Tools.*` (2) |
| `Design.Renderer` | `Microsoft.Data.Entity.*` (13) |
| `Design.DatabaseGeneration` | `Microsoft.Data.Entity.*` (14) |
| `VS.Data.Entity.Design` | `Microsoft.VisualStudio.Data.*` (458), `XamlGeneratedNamespace` (3) |
| `VS.Data.Tools.Design.XmlCore` | `Microsoft.Data.Tools.*` (69), `Microsoft.Data.Entity.*` (68), `Microsoft.VisualStudio.Data.*` (1) |
| `Design.Extensibility` | `Microsoft.Data.Entity.*` (23) |
| `Design.Package` | `Microsoft.Data.Entity.*` (29), `Microsoft.VisualStudio.Shell` (1) |
| `Entity.Tools` | `Microsoft.Data.Entity.*` (4) |

Two specifics worth knowing before Roslyn runs:

- **`Design.Model` and `Data.Tools.Design.XmlCore` share namespaces.** `Microsoft.Data.Entity.Design.Model`,
  `.Model.Commands`, `.Model.Entity`, `.Model.Integrity`, `.Model.Validation` and `.Model.Visitor` all exist
  in both assemblies, base classes in one and derived in the other. This is the direct cause of the C# §7.8.1
  resolution failures seen during earlier renames. Splitting into `XmlEngine` and `Edmx` ends it by construction.
- **`System.Data.Entity.Core.*` in `Design.Model` is deliberate.** Two files, `Validation/RuntimeErrorCodes/
  {ErrorCode,MappingErrorCode}.cs`, are verbatim copies of internal Entity Framework enums and **must keep
  their original namespace**. See `model-architecture.md`. Exclude them from any namespace sweep.

## Things the compiler will not catch

142 string occurrences, by kind. Full file/line list in `_rename-string-hits.md`.

| Kind | Count | Fails |
|---|---:|---|
| `DslDefinition.dsl` namespace attributes | 33 | at regeneration |
| `InternalsVisibleTo` | 29 | at build, in the *consuming* project |
| `.dll` literals (pkgdef, vsct, vsixmanifest, csproj) | 29 | at VSIX load |
| T4 `<#@ #>` directives | 22 | at regeneration |
| XAML `clr-namespace` / `assembly=` | 22 | at XAML parse, i.e. when a dialog opens |
| `Assembly.LoadFile` + manifest resource names | 4 | at test run |
| `pack://` URIs | 2 | when the dialog opens |
| pkgdef `"Class"` | 1 | silently, at directive-processor resolution |

The last four rows are the dangerous ones — nothing in a build or a test sweep fails until the exact
code path runs.

**Already burned by this twice:** two stale `pack://` URIs naming a pre-rename assembly caused
`XamlParseException` at dialog open, and the pkgdef `"Class"` entry still names a namespace that no longer
exists (`known-issues.md` 6.1). Assume the same class of breakage for every row above.

Specific traps:

- `Microsoft.Data.Entity.Tests.Shared/ResourcesHelper.cs` lines 57–65 embed **both** the assembly name and the
  full namespace path of manifest resources — e.g. `"Microsoft.VisualStudio.Data.Entity.Design.UI.Views.Dialogs.DialogsResource"`.
  These change twice: once for the assembly, once for the namespace.
- `DslDefinition.dsl` binds C# type names and XML element names as **separate** attributes
  (`TypeName=` vs `ElementName=`). Rename `TypeName`; never touch `ElementName` — that is the persisted
  `.diagram` format. `EntityDesignerSurface`/`EntityDesignerDiagram` is the existing precedent.
- T4 regeneration is documented broken (`known-issues.md` 4.2), so generated `.cs` under `GeneratedCode\`
  is hand-edited to match its template rather than regenerated.

## Type renames — step 5

Every type in every file is catalogued in `_type-catalog.md`: 1515 files, 2068 type declarations,
0 unreadable. Each row carries the file, type, kind, whether it is generated, whether the file
violates one-type-per-file, the destination name, and why.

| | |
|---|---:|
| Unchanged | 1,896 |
| Unchanged, generated | 116 |
| Unchanged — `MicrosoftDataEntityDesign` prefix, see below | 41 |
| **Decided — collides with `Diagrams`** | **10** |
| **Deferred — carries the retired `Escher` codename** | **5** |
| Non-generated files holding more than one top-level type | **92 files** |

`Escher` renames are deferred, not cancelled. One-type-per-file applies everywhere including
`VirtualTreeGrid` — no folder is grandfathered.

### The `MicrosoftDataEntityDesign` prefix — leave it alone

41 types carry it, 30 of them generated. It comes from `Name="MicrosoftDataEntityDesign"` on the
`<Dsl>` element, via `string dslName = this.Dsl.Name;` in the SDK templates.

**It is not stale and it is not changing.** The value is `Microsoft.Data.Entity.Design` with the dots
stripped — a squash of the *root* namespace, not a name anyone chose. Every Core project keeps that
root, so the prefix stays accurate.

Upstream proves the decoupling. `dotnet/ef6tools` has the same `Name="MicrosoftDataEntityDesign"` with
`Namespace="Microsoft.Data.Entity.Design.EntityDesigner"`; this fork already moved the leaf to `.Dsl`
and left `Name` untouched, with no build, test or round-trip consequence. The attribute has already
survived the exact change we are about to make again.

### Two `.dsl` attributes that *are* step 2

| Attribute | Now | Becomes |
|---|---|---|
| `Namespace` | `Microsoft.Data.Entity.Design.Dsl` | `Microsoft.Data.Entity.Design.Diagrams` |
| `PackageNamespace` | `Microsoft.Data.Entity.Design.Package` | `Microsoft.VisualStudio.Data.Entity.Package` |

Both feed generated namespaces and move with their containers, so they change in the same commit as
the corresponding `.csproj`. `Name` does not move, and neither does any `ElementName`.

### The 5 Escher types

`EscherAttributeContentValidator`, `EscherModelValidator`, `EscherModelValidatorVisitor`,
`EscherExtensionPointManager`, and one test class. Escher was the designer's internal Microsoft
codename. Decide whether it goes; it is the clearest case of a name that carries no meaning for
anyone maintaining this now.

### The 92 files needing extraction

One-type-per-file is a repo mandate for non-generated code. Concentration, worst first:

| Project | Files |
|---|---:|
| `VS.Data.Tools.Design.XmlCore` | 32 |
| `VS.Data.Entity.Design` | 21 |
| `Data.Tools.Design.XmlCore` | 14 |
| `Design.Model` | 13 |
| `Design.Dsl` | 8 |
| everything else | 4 |

Worst single files: `VirtualTreeGrid/Provider/VirtualTreeFlags.cs` (18 types),
`Model/XmlModelProvider.cs` (11), `VirtualTreeGrid/Provider/ProviderEvents.cs` (10).

Two thirds of the extraction work is in the two shell assemblies, and most of that is
`VirtualTreeGrid` — a WinForms control library where enum-and-eventargs-per-file was never the
convention. Worth deciding whether the mandate applies there or whether that folder is grandfathered,
because it is a large amount of churn in code nobody is otherwise touching.

### The 10 collisions

Ten type names exist in **both** `Design.Model` and `Design.Dsl`:

| Colliding type | In `Edmx` it is | In `Diagrams` it is |
|---|---|---|
| `EntityType`, `Property`, `PropertyBase`, `ComplexProperty`, `ScalarProperty`, `NavigationProperty`, `Association` | EDMX semantics, derives from `EFElement` | DSL view model, a `ModelElement` |
| `EntityTypeShape`, `AssociationConnector`, `InheritanceConnector` | **persisted** geometry in `Designer/` — `PointX`, `PointY`, `Width`, `FillColor`, `IsExpanded` | **live** shapes on the surface |

Resolution: prefix the `Edmx` side — `EdmxEntityTypeShape`, `EdmxAssociationConnector`, and so on — so the
distinction is stated at the type rather than carried by the container alone.

This is safe for the file format. XML names are already fully decoupled from C# type names: 62
`static readonly string ElementName = "…"` and 124 `Attribute*` constants in `Design.Model`, and the only
`typeof(X).Name` in the project is inside a debug message.

## Verification gates

Run after **every** project, not at the end.

1. `dotnet build src/EasyAF.EntityDesigner.slnx -c Release` — clean.
2. `dotnet test src/EasyAF.EntityDesigner.slnx -c Release` — 14 assemblies, 0 failures.
3. **Northwind renders byte-identical** on both targets. This is the round-trip gate; it catches accidental
   `ElementName` drift in `DslDefinition.dsl` immediately.
4. Regenerate `_rename-string-hits.md` and confirm the count falls as expected — a row that did not move is
   a row that was missed.
5. For anything touching XAML or the pkgdef, **open the designer in Visual Studio and open the affected
   dialog.** No automated gate covers those paths.
