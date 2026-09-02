# Model architecture

Why there are three things called "Model", what belongs in each, and why this codebase models EDMX itself
instead of using Entity Framework's own metadata classes. `layer-map.md` says which assembly may reference
which; this says what the model stack *is*.

Upstream is [dotnet/ef6tools](https://github.com/dotnet/ef6tools) — `src/EFTools/` there maps onto this
repository's projects, with `XmlCore` and `DesignXmlCore` as the two halves described below. The runtime is a
separate repository, [dotnet/ef6](https://github.com/dotnet/ef6). Claims about runtime internals in this
document are checked against that repository, not against any local copy.

## The stack

| Tier | Lives in | Is |
|---|---|---|
| Substrate | `Microsoft.Data.Tools.Design.XmlCore` → `Model/` | A designer over *an* XML schema system. No EDMX. |
| Implementation | `Microsoft.Data.Entity.Design.Model` | That substrate bound to EDMX specifically. |
| Editor adapter | `Microsoft.VisualStudio.Data.Tools.Design.XmlCore` → `Model/` | The bridge to Visual Studio's XML editor buffer and undo stack. Not a model. |

### The substrate — `XmlCore/Model/`

108 files. `EFObject`, `EFElement`, `EFArtifact`, `EFNameableItem`, `EFDocumentableItem`, `DefaultableValue`,
`Binding<T>`, plus generic `Commands/`, `Validation/`, `Integrity/`, `Visitor/`, `Eventing/` and
`XLinqAnnotations/` infrastructure.

This is the Data Tools team's general-purpose XML-schema designer framework. The team owned more than one
XML-backed data schema system, and this is the part that would be shared across any of them: an object model
that wraps a live XML document, tracks bindings between elements, validates, and supports undoable commands.
The name is `XmlCore`, not `EdmxCore`, and that is the intent.

**What belongs here:** anything true of *any* XML schema system. Identity, containment, references between
elements, defaultable attribute values, the command and transaction model, the visitor and validation
frameworks, the artifact/artifact-set lifecycle.

**What does not:** any knowledge of EDMX, CSDL, SSDL, MSL, entities, associations or mappings.

### The implementation — `Microsoft.Data.Entity.Design.Model`

343 files: `Entity/`, `Mapping/`, `Designer/`, `Database/`, `MetadataConverter/`, `UpdateFromDatabase/`,
plus EDMX-specific `Commands/`, `Integrity/`, `Validation/`, `Visitor/`.

This is the substrate bound to one schema system. The dependency is overwhelming and one-directional: 74 types
derive from `DefaultableValue`, 36 from `EFElement`, 6 from `EFNameableItem`, 3 from `EFArtifact`. It also
carries the EDMX schemas themselves (`Microsoft.Data.Entity.Design.Edmx_*.xsd`).

**What belongs here:** everything that names an EDMX concept.

### The editor adapter — `VS XmlCore/Model/`

6 files, and the folder name is wrong — there is no model in it. `VSXmlModel`, `VSXmlModelProvider`,
`VSXmlTransaction`, `VSXmlChange` and subclasses, `ParentUndoManager`, `ParentUndoUnit`,
`IXmlDesignerPackage`. It plugs the substrate into Visual Studio's XML editor so that edits flow through the
shared text buffer and land on the IDE's undo stack. Its namespace,
`Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio`, is honest about this even though the folder is not.

The substrate's own `Model/StandAlone/VanillaXmlModelProvider` is the counterpart that lets the same object
model run with no editor at all, which is what makes headless rendering possible.

## Why EDMX is modelled here instead of reusing Entity Framework's classes

The obvious question is why `Microsoft.Data.Entity.Design.Model` exists when Entity Framework already ships
`EdmItemCollection`, `StoreItemCollection`, `StorageMappingItemCollection` and `MetadataWorkspace` that parse
exactly these schemas. Four reasons, each checkable against the source:

**1. The runtime reads EDMX only to execute it.** EDMX is an envelope. Alongside `<ConceptualModels>`,
`<StorageModels>` and `<Mappings>` it carries `<Designer>`, `<Diagrams>`, `<Diagram>`, `<AssociationConnector>`,
`<ConnectorPoint>`, `<DesignerProperty>`, `<DesignerInfoPropertySet>`, `<Connection>` and `<CopyToSSDL>` — shape
positions, connector routing and designer settings.

Entity Framework does have an `EdmxReader` (`src/EntityFramework/Infrastructure/EdmxReader.cs` in
[dotnet/ef6](https://github.com/dotnet/ef6)), but its whole signature is the argument:

```csharp
public static DbCompiledModel Read(XmlReader reader, string defaultSchema)
```

It pulls the storage mapping item collection out of the document and returns a `DbCompiledModel` — a compiled,
frozen artifact for running queries. The designer half of the envelope is not read, nothing is retained that
would let the document be written back, and the result is not editable. Reading EDMX to execute it and reading
EDMX to edit it are different problems, and the runtime solves only the first.

**2. The runtime model is immutable.** `MetadataItem.SetReadOnly()` and `ReadOnlyMetadataCollection<T>` freeze
the graph once it is built. That is correct for a metadata cache consulted on every query, and useless as the
backing store for an editor.

**3. The runtime model requires a valid model; a designer must open invalid ones.** Building an
`EdmItemCollection` over malformed schema throws. A designer has to load the file *because* it is broken, show
the errors in place, and let the user fix them. `ErrorClass` encodes exactly this split, separating
`Runtime_CSDL` / `Runtime_SSDL` / `Runtime_MSL` from designer-level `Escher_*` errors — "Escher" being the
designer's internal codename.

**4. The file must round-trip.** `EFObject` holds an `XObject`: every model object is a live handle on the
actual XML node, and `Delete(bool deleteXObject)` deliberately separates removing the object from removing the
XML. `Microsoft.Data.Entity.Design.Model` carries 538 XLinq references. This is what preserves formatting,
comments, element order and unrecognised content across an edit — a semantic model reconstructed from parsed
metadata cannot, because the information was discarded at parse time.

**Both models are used, on purpose.** The designer represents and edits with the XLinq model, then hands off
to Entity Framework's metadata classes to validate. `Design.Model` touches `EdmItemCollection` or
`StoreItemCollection` in exactly seven files, all of them validation or resolution:
`Validation/RuntimeMetadataValidator.cs`, `Validation/UnrecoverableRuntimeErrors.cs`,
`Validation/RuntimeErrorCodes/MappingErrorCode.cs`, `EdmRuntimeSchemaResolver.cs`,
`Integrity/PropagateStoragePropertyFacetsToConceptualModel.cs`, `ModelHelper.cs` and `EFExtensions.cs`.
`Microsoft.Data.Entity.Design.VersioningFacade` wraps the same classes for version-specific work. So the answer
is not "they ignored the runtime model" — it is "the runtime model answers *is this valid*, and cannot answer
*what is in the file and how do I change it*."

## Why there is no shared core

Two teams, two wrong assumptions, no shared owner. This section says so plainly because the alternative —
reading the result as a considered design — is how the same mistakes get preserved.

**The runtime team modelled the build output and called it the model.** EDMX is a *design-time* artifact. At
build time the designer extracts `.csdl`, `.ssdl` and `.msl` from it, and those extracted files are what the
runtime consumes ([CSDL](https://learn.microsoft.com/en-us/ef/ef6/modeling/designer/advanced/edmx/csdl-spec),
[SSDL](https://learn.microsoft.com/en-us/ef/ef6/modeling/designer/advanced/edmx/ssdl-spec),
[MSL](https://learn.microsoft.com/en-us/ef/ef6/modeling/designer/advanced/edmx/msl-spec) specs). So the runtime
metadata stack was designed against compiler *output*: already extracted, already valid, never edited. Freezing
it with `SetReadOnly()` is coherent under that assumption. The mistake was that nobody modelled the source
artifact developers actually edit, and the one model that existed was locked shut. `EdmxReader` arrived later
and does not change this — it returns a frozen `DbCompiledModel` for execution.

**A different team had to solve the editing problem from outside.** The designers live in
[dotnet/ef6tools](https://github.com/dotnet/ef6tools), a separate repository from the runtime at
[dotnet/ef6](https://github.com/dotnet/ef6), and it describes itself as "the codebase for the Entity Framework
6 **and LINQ-To-SQL** designers". That is a Visual Studio designers codebase, not an Entity Framework one —
which is also why `XmlCore` was written as a schema-agnostic substrate: it had more than one designer to serve.
That team could not extend the runtime's model, because it was immutable, validity-gated and owned elsewhere.
So they built a second complete model of the same three schemas.

**Neither team owned the seam, so it was solved by copying.** `src/` in the tools repo holds `Common`,
`EFTools`, `Migrate`, `PowerTools` — and `EntityFramework/Core`, which is exactly where a shared core would
live. It contains two files:

```
src/EntityFramework/Core/Mapping/MappingErrorCode.cs
src/EntityFramework/Core/SchemaObjectModel/ErrorCode.cs
```

Both are error-code enums copied verbatim from the runtime, keeping their original
`System.Data.Entity.Core.*` namespaces, and both are still `internal`. Both carry the same comment:

```
// error numbers end up being hard coded in test cases; they can be removed, but should not be changed.
// reusing error numbers is probably OK, but not recommended.
//
// The acceptable range for this enum is
// 0000 - 0999
//
// The Range 10,000-15,000 is reserved for tools
```

The runtime team knew the tools existed — they reserved a number range and froze the numbering. But the enums
holding those numbers stayed `internal`, so the tools could not reference them. Rather than either side
publishing a shared contract, the tools repository copies the two files and the two codebases stay in sync by
convention and a comment. That is not a design; it is what teams do when neither owns the boundary and the
release trains do not line up.

**So the honest answer is that both models exist because the architecture is bad, in two distinct ways.** The
runtime made the source artifact unrepresentable by modelling only its compiled form and sealing it. The tools
team, working around that from another org, built a parallel stack with its own problems — a "generic"
substrate with EDMX hard-coded into it, one namespace split across two assemblies at two layers, designer types
sealed `internal` so its own extensibility point could never work. Each decision is locally explicable. The sum
is two full implementations of one file format, maintained separately, coupled by two hand-copied enums.

Both copies are still here, unchanged, at
`Microsoft.Data.Entity.Design.Model/Validation/RuntimeErrorCodes/{ErrorCode,MappingErrorCode}.cs`. They are the
only real seam, and they are load-bearing: if the runtime ever renumbers, the designer's error reporting
silently mismaps. Nothing detects that.

**What this means here.** We own the tools side only, so the runtime's assumption is a fact of life, not
something to fix. What is ours to fix is the second layer of damage — and the standing instruction is to
inherit none of it by default. Where the upstream structure is bad, say so and change it; do not reconstruct a
rationale for it.

## Where the current code breaks its own rule

The substrate is supposed to be schema-agnostic and mostly is, but EDMX has leaked into it. These are the known
bleed-throughs, and they are the reason `XmlCore` cannot be reused for another schema system as it stands:

- `Model/EFElement.cs` hard-codes SSDL namespace URIs (`http://schemas.microsoft.com/ado/*/edm/ssdl`).
- `Model/Validation/ErrorClass.cs` enumerates `Runtime_CSDL`, `Runtime_SSDL`, `Runtime_MSL`, `Escher_CSDL`,
  `Escher_SSDL`, `Escher_MSL`, `Escher_UpdateModelFromDB` — EDMX vocabulary in the base library.
- `Model/EFArtifact.cs`, `Model/ModelManager.cs`, `Model/Commands/Command.cs` and
  `Model/StandAlone/VanillaXmlModelProvider.cs` refer to EDMX in names or documentation.
- The `EF` prefix on `EFObject` / `EFElement` / `EFArtifact` names the abstraction after its only consumer.

Whether to clean these up depends on whether a second schema system is ever a real goal. If it is not, the
honest move is to stop describing the substrate as general and let the names say EDMX. What is not defensible
is the current position: a library that claims to be schema-neutral, is treated as though it were, and quietly
is not.
