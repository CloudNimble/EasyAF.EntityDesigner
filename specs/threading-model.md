# Threading Model

The Visual Studio Modeling SDK is single threaded. Everything below follows from that.

## The constraint

The Modeling SDK assumes it runs on one thread — the Visual Studio UI thread — and keeps process wide state that it never synchronizes. Two known sites, both confirmed by stack traces rather than inference:

| State | Written by | When |
|---|---|---|
| `DomainXmlSerializerDirectory` serializer registry | `CoreDesignSurfaceSerializationHelperBase.InitializeSerialization`, via `InternalAddBehavior` | constructing a `Store` |
| `StoreDiagramMappingData` — a static `Dictionary` keyed by `Store` | `DiagramCommittingRule.TransactionCommitting` | **every transaction commit** |

Driving it from two threads at once produces, in rough order of how obvious they are:

- `System.NullReferenceException` from `Dictionary.Insert`, when the dictionary's internal buckets are torn
- `InvalidOperationException: Collection was modified; enumeration operation may not execute`
- a test host process that dies outright with no message

The failures are intermittent. In one measured run of a six test suite, twelve executions produced seven clean runs, one single failure, one double failure, and one host crash. That intermittency is the dangerous part: this passes code review, passes locally, and fails in CI.

## What does not work

**Locking `Store` construction is not sufficient.** This was tried and measured, not assumed. With a lock around `new Store(...)` and parallelism otherwise enabled, the suite still failed roughly one run in four, because `StoreDiagramMappingData` is written during transaction commit — from inside an SDK rule, on a path no discipline at our own call sites can guard.

**Per class `[DoNotParallelize]` is not sufficient either.** Not technically — it works — but as a policy it depends on every future author knowing the rule and remembering to apply it. Four classes carried the attribute and the hazard still reached the point of crashing a test host.

## How it is enforced

Per assembly, in `src/Directory.Build.props`. A test project opts out of parallelism by declaring:

```xml
<PropertyGroup>
  <UsesDslStore>true</UsesDslStore>
</PropertyGroup>
```

which swaps the default `[assembly: Parallelize(Workers = 0, Scope = MethodLevel)]` for `[assembly: DoNotParallelize]`. Nothing else in the solution loses parallelism.

**If you add a test project that builds a `Store`, set `UsesDslStore`.** If you are not sure whether it does, it does if it references `Microsoft.Data.Entity.Design.Dsl` and constructs anything.

## Which projects are single threaded

| Project | Concurrency | Why |
|---|---|---|
| `Microsoft.Data.Entity.Design.Dsl` | **single threaded** | owns the DSL `Store`, shapes, connectors and diagram |
| `Microsoft.Data.Entity.Design.Renderer` | **single threaded** | builds a `Store` per render through `HeadlessDiagramStore` |
| `Microsoft.Data.Entity.Tools` | **single threaded** | drives the renderer |
| `Microsoft.Data.Entity.Design.Package` | **single threaded** | VS integration; runs on the UI thread anyway |
| `Microsoft.VisualStudio.Data.Entity.Design` | single threaded in practice | VS UI, dialogs, wizards |
| `Microsoft.Data.Entity.Design.Model` | free threaded | `EFObject`/`EFElement` over XLinq; no SDK dependency |
| `Microsoft.Data.Entity.Design.VersioningFacade` | free threaded | EF6 metadata; no SDK dependency |
| `Microsoft.Data.Entity.Design.DatabaseGeneration` | free threaded | T4 and DDL generation |
| `Microsoft.Data.Tools.Design.XmlCore` | free threaded | XML model plumbing |

"Free threaded" here means *not constrained by the Modeling SDK*. It does not mean the types are individually thread safe; it means nothing in them takes a process wide SDK lock-free static.

The practical consequence for the command line tool: **rendering several diagrams must be sequential within a process.** Rendering them in parallel requires one process per diagram.

## Rule of thumb

If a type touches `Store`, `ModelElement`, `ShapeElement`, `Diagram` or a DSL transaction, it is single threaded. Say so in its XML documentation, and if it is a test, put it in an assembly that sets `UsesDslStore`.
