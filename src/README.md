## How to build locally
1. Make sure you have the WiX Toolset installled. https://wixtoolset.org/releases/ (Note that you need the Toolset, not the VSExtension)
1. Launch the VS2019 Developer command prompt. (It has to be this, due to reasons).
1. Make sure that NuGet is available on the path.
1. .\BuildEFTools.cmd from the root of this repo.

## Threading

The Visual Studio Modeling SDK is single threaded and keeps unsynchronized process wide state, so anything built on it must be driven from one thread. Full detail, including the two known static hazards and how the constraint is enforced, is in [specs/threading-model.md](../specs/threading-model.md).

| Project | Concurrency |
|---|---|
| `Microsoft.Data.Entity.Design.Dsl` | **single threaded** |
| `Microsoft.Data.Entity.Design.Renderer` | **single threaded** |
| `Microsoft.Data.Entity.Tools` | **single threaded** |
| `Microsoft.Data.Entity.Design.Package` | **single threaded** |
| `Microsoft.VisualStudio.Data.Entity.Design` | **single threaded** |
| `Microsoft.Data.Entity.Design.Model` | free threaded |
| `Microsoft.Data.Entity.Design.VersioningFacade` | free threaded |
| `Microsoft.Data.Entity.Design.DatabaseGeneration` | free threaded |
| `Microsoft.Data.Tools.Design.XmlCore` | free threaded |

**Adding a test project that builds a DSL `Store`?** Set `<UsesDslStore>true</UsesDslStore>` in the csproj. That swaps the solution default of method level parallelism for `[assembly: DoNotParallelize]` on that assembly only. Without it the suite fails intermittently — null references from a torn dictionary, `Collection was modified`, or a dead test host.

## Regenerating the DSL

`GeneratedCode\*.cs` in `Microsoft.Data.Entity.Design.Dsl` comes from `DslDefinition.dsl`. To regenerate outside Visual Studio, set the `DslTemplatesSrc` environment variable to the DSL SDK's `TextTemplates` folder, then:

```
msbuild Microsoft.Data.Entity.Design.Dsl\Microsoft.Data.Entity.Design.Dsl.csproj -t:TransformAll -p:TransformOutOfDateOnly=false
```

Read [specs/dsl-toolchain-alignment.md](../specs/dsl-toolchain-alignment.md) first: the templates shipped with VS 18 emit code that does not currently compile against the packages this repo pins, and `TransformAll` overwrites its outputs before it discovers a problem. Regenerate only from a clean tree.
