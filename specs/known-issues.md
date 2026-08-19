# Known Issues

Everything currently between this repository and "builds clean and all tests pass, every time".

Measured with `dotnet build -c Release --no-incremental` and `dotnet test -c Release`. The solution **compiles with 0 errors**; everything below is warnings, test failures, or tests that silently do not run. Items marked FIXED have been resolved since the list was first taken at `2d94be6`; their entries are kept because the reasoning behind the fix is worth having.

## Summary

| Category | Count |
|---|---|
| Failing tests | 0 — see 1.1, and 1.4 for the last intermittent |
| Tests disabled with `[Ignore]` | 186 |
| Tests that never run because of an invalid signature | 17 |
| Build warnings | 284 |
| Packages with known vulnerabilities | 0 — fixed, see 3.1 |
| Minor defects logged for later | 6 — see section 5 |

## 1. Failing tests

### 1.1 Wizard pages need a live VS shell — 10 failures — FIXED

**All ten now pass.** They were fixed by deleting the `Services` service locator, not by anything aimed at the tests. That class cached its `IServiceProvider` in a `??=` static which nothing ever cleared: a `MockPackage` from one test stayed live for every test that followed, long after the test that created it had disposed it. Every call site now reads `PackageManager.Package`, which each test sets for itself.

This was verified by bisection — restoring the old `Debug.Assert` in `PackageManager.Package` while keeping the locator deleted still gives 60 passed / 0 failed, so the locator was the cause and the assert change was not.

The analysis below is kept because the cross-assembly interference it describes is real and still applies to anything that leans on shell statics.

Run the project on its own and this used to be a deterministic 10. Run the whole solution and the count drifts by one or two, and the extra names move between assemblies — `DispatchSaveToExtensions_invokes_serializers_if_present` in `Tests.Design.Package` one run, a pair in `Tests.Design` the next, neither reproducible in isolation.

That is the same root cause as the ten, one level up: the shell statics these tests lean on (`ThreadHelper.JoinableTaskContext`, the `IVsSettingsManager` service lookup) are process-wide, and test assemblies run concurrently. Whichever assembly initialises the shell first decides what the others see. `UsesDslStore` (see `threading-model.md`) serialises tests *within* an assembly; nothing serialises them *across* assemblies.

So when comparing runs, compare against a per-project baseline, not the solution total. A regression shows up as a deterministic isolated failure.

`Microsoft.Data.Entity.Tests.Design.Package` (net48):

- `OnActivate_result_depends_on_FileAlreadyExistsError`
- `OnDeactivate_creates_and_verifies_model_path`
- `OnDeactivate_does_not_update_settings_if_model_file_already_exists`
- `OnDeactivate_updates_model_settings_if_model_file_does_not_exist_for_CodeFirst_empty_model`
- `OnDeactivate_updates_model_settings_if_model_file_does_not_exist_for_CodeFirst_from_database`
- `OnDeactivate_updates_model_settings_if_model_file_does_not_exist_for_empty_model`
- `OnDeactivate_updates_model_settings_if_model_file_does_not_exist_for_generate_from_database`
- `listViewModelContents_DoubleClick_calls_OnFinish_if_EmptyModel_selected`
- `listViewModelContents_DoubleClick_calls_OnFinish_if_EmptyModelCodeFirst_selected_and_EF_not_referenced_or_EF6_referenced`
- `listViewModelContents_DoubleClick_wont_call_OnFinish_if_EmptyModelCodeFirst_selected_and_EF5_referenced`

```
Microsoft.Assumes+InternalErrorException: Cannot find an instance of the
Microsoft.VisualStudio.Shell.Interop.IVsSettingsManager service.
   at Microsoft.VisualStudio.PlatformUI.DpiHelper.DpiHelperImplementation.get_SettingsStore()
   at Microsoft.VisualStudio.Utilities.Dpi.DpiHelper.CreateDeviceFromLogicalImage(...)
```

They construct real WinForms wizard pages, and VS's `DpiHelper` resolves image scaling through the shell settings store. Note these previously failed *earlier*, in `InitializeComponent` on a missing resx; fixing that in `2d94be6` moved the failure to `DpiHelper`. So this was always the second failure waiting behind the first.

**Options.** Move them to the `Microsoft.VisualStudio.TestTools.VsIdeTesting` host that already exists in this repo; or extract the logic under test from the form so it can be tested without constructing a control; or mark them with a category excluded from headless runs. The second is the only one that makes them fast and CI friendly, and it is real work.

### 1.2 Theming reaches the VS shell on net10 — FIXED

Three renderer tests failed on net10 with `This is a reference assembly.` from
`EntityDesignerSurface.IsThemeServiceAvailable()`, which resolved `SVsUIShell` through
`Package.GetGlobalService` to decide whether it was running inside Visual Studio.

Theming is now pushed in by the host rather than pulled by the designer. The Dsl declares a `DiagramPalette` of semantic colors with defaults; `Microsoft.Data.Entity.Design.Package` reads Visual Studio's theme and calls `DiagramTheme.Apply`. Nothing on a headless path touches `Microsoft.VisualStudio.Shell`, and `IsThemeServiceAvailable` is deleted — the designer no longer asks whether it is inside Visual Studio, which was never a question it should have had to answer.

`Microsoft.Data.Entity.Tests.Design.Renderer` now passes **138/138 on both net48 and net10**.

### 1.3 `DatabaseGenerationAssemblyLoader` null reference — FIXED

`AssemblyLoader_passed_WebsiteProject_can_find_correct_paths_to_DLLs` threw at the `foreach` over `vsWebSite.References`.

The production code was fine; the defect was in `MockDTE.CreateVsWebsite`. `foreach` binds to the strongly typed `AssemblyReferences.GetEnumerator()`, not the `IEnumerable` one, and the helper set up only the latter — so the former returned null and the loop threw. `CreateVsProject2` sets up both, which is why the project-reference test alongside it always passed.

It also had a second, latent defect: `Returns(references.GetEnumerator())` evaluated the enumerator once at setup instead of `Returns(() => ...)`, so a second enumeration would have received an already-exhausted enumerator.

`Microsoft.Data.Entity.Tests.Design` now passes 455 of 481 with 26 ignored, stable across four runs.

### 1.4 `Generate_returns_code` intermittent — FIXED 2026-08-19

`DefaultCSharpEntityTypeGeneratorTests.Generate_returns_code`, `System.InvalidOperationException: Sequence contains no matching element`. Observed once in roughly ten runs; did not reproduce in six consecutive runs afterwards.

Suspect the same hazard as issue 2.3: MSTest runs at `MethodLevel` parallelism and something here is not thread safe. Worth confirming with `[DoNotParallelize]` on that class and a few hundred runs.

**2026-08-17 — the rate is far higher than "one in ten", and it is not caused by the ongoing refactor.** Running the test alone with `--filter FullyQualifiedName~Generate_returns_code` (8 tests in the filtered set) failed 1 of 3 on the working tree. A detached worktree at `ff6422f0`, built and run the same way with none of the refactor present, failed **2 of 5** — same test, same exception. So the flake is pre-existing and reproduces at roughly 20-40% when the class is run in a small filtered set, versus rarely in a full-solution run.

That inversion is itself the clue. A smaller run is *more* likely to fail, which is backwards for a contention bug that needs many threads. It fits an ordering hazard instead: with fewer tests scheduled, `Generate_returns_code` is more likely to run before whatever sibling test initialises the state it reads. Filtering to that one class is therefore a much cheaper reproduction than a few hundred full runs — use it when someone actually chases this down.

**2026-08-19 — fixed.** The cause was `GeneratorTestBase.Model`, an unguarded lazy static:

```csharp
private static DbModel _model;
protected static DbModel Model
{
    get
    {
        if (_model == null) { /* build */ _model = modelBuilder.Build(...); }
        return _model;
    }
}
```

Four test classes derive from that base, and MSTest runs at `MethodLevel` parallelism. Two threads both find the field null, both build a model, and the second assignment **replaces** the first.

The replacement is what breaks it, not the wasted work. Every test there reads the property twice:

```csharp
generator.Generate(
    Model.ConceptualModel.Container.EntitySets.First(),   // model instance A
    Model,                                                // model instance B
    "WebApplication1.Models");
```

An entity set from A handed to the generator alongside model B means `TableDiscoverer.Discover` looks the set up in B's `ConceptualToStoreMapping.EntitySetMappings`, matches nothing, and `First` throws `Sequence contains no matching element`.

That also explains the inversion above, which had looked like an ordering hazard. A small filtered run starts all four classes at once with the field still null, which is exactly the widest window for the race; in a full run something has usually initialised it before the rest arrive.

The fix is `Lazy<DbModel>` with `LazyThreadSafetyMode.ExecutionAndPublication`, so the instance is built once and never replaced. Not `[DoNotParallelize]`, which would have hidden a real defect behind slower tests.

Verified: the reproducing filter, which failed 3 of 3 immediately before, passed **10 of 10**; the whole assembly passed 455/481 three times running.

Note the test named here was wrong throughout the entries above. The failure is in `DefaultVBEntityTypeGeneratorTests`, not the C# one — four classes share the base and all four have a method with this name, and the console summary never said which.

### 1.5 The intermittent failure is not confined to one assembly — FIXED 2026-08-19 by 1.4

Two more single-test failures during the 2026-08-16 project file work, each in a different assembly, each passing when that project was rerun on its own immediately afterwards:

| Assembly | Target | Run |
|---|---|---|
| `Microsoft.Data.Entity.Tests.Design` | net48 | full solution, 455/456 |
| `Microsoft.Data.Entity.Tests.Design.VersioningFacade` | net10.0 | full solution, 294/295 |
| `Microsoft.VisualStudio.Data.Entity.Tests.Package` | net48 | full solution, 59/60 (2026-08-17) |

**Neither test name was captured**, which is the first thing to fix — the console logger reports the count on the summary line but the name scrolls past in a full-solution run. Use `--logger "trx" --results-directory <dir>` and read `outcome="Failed"` out of the `.trx`. Three consecutive full-solution runs with trx afterwards produced no failures at all, so the rate is low and matches 1.4's rough one-in-ten.

**2026-08-18 — the `Microsoft.Data.Entity.Tests.Design` net48 name is now captured, and it is `Generate_returns_code`.** So that row is not a third distinct test; it is 1.4 seen from the other assembly that runs `DefaultCSharpEntityTypeGeneratorTests`. Reproduced 3 of 3 on the working tree with `--filter FullyQualifiedName~Generate_returns_code`, and 1 of 3 on a detached worktree at `3ac2cd0a` containing none of the layout-engine work — pre-existing, and the rate varies run to run as a flake does.

That leaves 1.5 with two unnamed rows rather than three, and strengthens 1.4 as the single root cause to chase.

A second trap worth recording, because it caused a misdiagnosis before the trx run settled it: **the console summary lines interleave in a full-solution run**, so a `Failed!` line can appear glued to the wrong assembly name. A run that read `...Duration: 2 sFailed! - Failed: 1, Passed: 294, Skipped: 95, Total: 390 - ...Diagrams.dll (net48)` was actually the EntityFramework assembly — `390 total / 95 skipped` is its shape, while `Tests.Design.Diagrams` has 5 tests. Match the counts to the assembly before believing the name, or just use trx.

Do not read the VersioningFacade row as pointing at `DbDatabaseMappingBuilderTests`. The string `Different API visibility between official dll and locally built one` appeared next to the failure in the console output and looks like an assertion message, but it is the `[Ignore]` reason on a skipped test in that file and has nothing to do with it.

The 2026-08-17 run failed two assemblies at once — `Tests.Design` and `Tests.Package` — and an immediate rerun of the identical binaries passed all fourteen assemblies with zero failures. The Package name was again not captured, because the trx rerun is what passed; **run with `--logger trx` from the start, not as a follow-up**, or the name is lost every time.

What makes this worth its own entry rather than folding into 1.4: three distinct tests across three assemblies and both target frameworks now fail intermittently and pass on rerun. That is a property of the run, not of any one test, which points at the `MethodLevel` parallelism theory in 1.4 and 2.3 rather than at three unrelated bugs. The cheap experiment is a solution-wide `[DoNotParallelize]` or `<RunSettings>` with `MaxCpuCount=1` for a few dozen runs — if the failures stop, the theory holds.

**2026-08-19 — the `Microsoft.Data.Entity.Tests.Design` row was 1.4 all along, and is fixed with it.** The name captured on 2026-08-18 was `Generate_returns_code`, so that row was never a third distinct test. The parallelism theory was right; the unsafe static in `GeneratorTestBase` was the specific instance of it.

**The other two rows are still open**, and neither name has been captured: `Tests.Design.VersioningFacade` (net10.0) and `Tests.Design.Package` (net48). Treat them as unknown rather than fixed. If either resurfaces, run with `--logger trx` from the start and read `outcome="Failed"` — and check `Lazy` or `??=` on any static the failing class shares with its siblings first, because that is what this turned out to be.

## 2. Tests that never run

### 2.1 186 tests disabled with `[Ignore]`

Overwhelmingly one reason, spelled thirteen different ways:

| Count | Reason |
|---|---|
| 64 | `Different API Visibility between official dll and locally built` |
| 56 | `Different API Visiblity between official dll and locally built` |
| 15 | `Type lacks parameterless constructor in locally built` |
| 3 | `Different API Visibility between official dll and locally built one` |
| 2 | `Updated binary has updated types` |
| 6 | nine further spellings of the same two reasons |

Concentrated in `Microsoft.Data.Entity.Tests.Design.VersioningFacade` (95 skipped of 390). The build uses locally built EF6 assemblies whose member visibility differs from the shipped ones, so the tests cannot see what they assert against.

This is the single largest hole in the suite. Fixing it means deciding whether to build against the official EF6 binaries, or to publicize the locally built ones — `Krafs.Publicizer` is already a dependency, which suggests someone started down the second path. Until then, roughly 39% of the VersioningFacade suite is dead weight.

At minimum the reason strings should be normalized to one spelling so the count is greppable.

### 2.2 17 tests declared `static`

MSTest will not run a `static` test method. It emits `MSTEST0003` (98 warnings) and the method silently never executes — it is not even reported as skipped.

| File | Count |
|---|---|
| `VersioningFacade/ReverseEngineerDb/OneToOneMappingBuilderTests.GenerateEdmFunctionsTests.cs` | 11 |
| `VersioningFacade/ReverseEngineerDb/UniqueIdentifierServiceTests.cs` | 4 |
| `VersioningFacade/ReverseEngineerDb/DbDatabaseMappingBuilderTests.cs` | 2 |

**They were not made static recently, and not to hide anything.** All 17 were already `public static void` at `721bc51` (2021-08-03), inherited from the original EF6 repository, where they were xUnit `[Fact]` methods. **xUnit runs static test methods; MSTest does not.** The January 2026 conversion from `[Fact]` to `[TestMethod]` turned a legal xUnit signature into one MSTest silently skips.

| File | Static | Framework then |
|---|---|---|
| `OneToOneMappingBuilderTests` (now `.GenerateEdmFunctionsTests`) | 11 of 45 | xUnit `[Fact]` |
| `UniqueIdentifierServiceTests` | 4 of 4 | xUnit `[Fact]` |
| `DbDatabaseMappingBuilderTests` | 2 of 14 | xUnit `[Fact]` |

**Fix:** delete the `static` keyword. There is no evidence they were failing — they simply stopped being collected when the framework changed under them. Worth checking whether any other `[Fact]` to `[TestMethod]` conversion in that batch carried a similar signature assumption.

### 2.3 Store-building tests race under parallelism — FIXED

**The Visual Studio Modeling SDK cannot be driven from more than one thread in a process.** It assumes a single threaded host and keeps unsynchronized process wide state in at least two places:

- `DomainXmlSerializerDirectory.InternalAddBehavior`, reached from `CoreDesignSurfaceSerializationHelperBase.InitializeSerialization` during **store construction**.
- `StoreDiagramMappingData.GetInstance(Store)`, a static `Dictionary` keyed by `Store` that `DiagramCommittingRule.TransactionCommitting` writes to on **every transaction commit**.

Symptoms are a `NullReferenceException` from `Dictionary.Insert`, `Collection was modified; enumeration operation may not execute`, or a torn dictionary that crashes the test host outright.

**Locking store construction is not sufficient, and this was measured rather than assumed.** With a lock around `new Store(...)` and parallelism otherwise enabled, 12 runs produced 7 clean, 1 with a single failure, 1 with two, and one host crash. The commit path still raced, because the state is mutated from inside SDK rules that no amount of care at our call sites can guard.

The constraint is therefore declared **per assembly**, in `Directory.Build.props`: a test project sets `UsesDslStore` to opt out of the blanket `[Parallelize(MethodLevel)]` and receive `[DoNotParallelize]` instead. `Microsoft.Data.Entity.Tests.Design.Dsl` and `Microsoft.Data.Entity.Tests.Design.Renderer` set it.

This replaces per-class `[DoNotParallelize]`, which was the previous approach on four classes. Per class only works if every future author remembers, and the failures are intermittent enough to survive review — the same trap that produced this entry.

Verified: 12 consecutive runs of the Dsl suite and 8 of the Renderer suite, all deterministic.

## 3. Build warnings — 181

| Count | Code | What |
|---|---|---|
| 168 | NU1701 | package restored using `.NETFramework` fallback rather than a matching target |
| 98 | MSTEST0003 | invalid test method signature — see 2.2 |
| 20 | SYSLIB0051 | obsolete formatter-based serialization |
| 20 | MSB3245 | could not resolve `System.Data`, `System.Data.Entity`, `System.Drawing` |
| 18 | NU1702 | project restored using a fallback framework |
| 4 | CS0436 | `Krafs.Publicizer` attribute in several assemblies — see 3.2 |
| 14 | MSB3243 | version conflict on `System.Data`, `System.Drawing`, `System.Xml.Linq` |
| 4 | VSTHRD110 | observe the result of async calls |
| 4 | VSTHRD002 | synchronously blocking on async work |
| 4 | SYSLIB0003 | code access security attributes are obsolete |
| 2 | VSSDK004, NU1603, CS0672, CA2022 | one each |

### 3.1 Vulnerable packages — FIXED

All 16 advisories across 4 packages are cleared. Restore and build now emit zero `NU1901`, `NU1902` and `NU1903`.

| Package | Was | Now | Advisories cleared |
|---|---|---|---|
| `MessagePack` | 2.5.187 | 2.5.302 | 11 (2 high, 9 moderate) |
| `Npgsql` | 4.1.3 | 4.1.14 | 1 high |
| `Azure.Identity` | 1.10.3 | 1.16.0 | 2 moderate |
| `Microsoft.Identity.Client` | 4.56.0 | 4.87.0 | 2 (1 moderate, 1 low) |

All four were transitive, so the fix is `PackageVersion` entries in `Directory.Packages.props` relying on `CentralPackageTransitivePinningEnabled`. Each is held inside the major line its consumer binds to, so the API surface does not move:

- **MessagePack** stays on 2.x. StreamJsonRpc and the rest of the VS SDK bind to 2.x; 3.x is a breaking change.
- **Npgsql** stays on 4.1.x. `EntityFramework6.Npgsql` 6.4.3 depends on Npgsql 4.1.3, and 5.x would break the EF6 provider. 4.1.14 is the last of the line.
- **Azure.Identity** is held at 1.16.0 rather than the latest 1.x. 1.17 and later pull `Azure.Core` 1.53, which requires `System.Text.Json` 10 and would force that pin up for the VSIX as well. 1.16.0 lands on `Azure.Core` 1.47.3, needing only `System.Text.Json` 8, which the existing 9.0.0 pin satisfies.

Verified: restore and build clean, `edmx render Northwind.edmx` byte-identical, and the test suite unchanged at the same 14 failures.

### 3.2 CS0436 duplicate type — FIXED

`ModelBuilderWizardFormHelper` was defined twice, in `Microsoft.Data.Entity.Tests.Design` and again in `Microsoft.Data.Entity.Tests.Design.Package`, in the same namespace, with the projects referencing each other. The copies were byte-identical apart from a BOM, using order and a trailing newline. The Package copy is deleted; `Tests.Design.Package` already referenced `Tests.Design`, which already exposed its internals to it, so nothing else was needed.

Note it could not move to `Microsoft.Data.Entity.Tests.Shared` as first suggested: it depends on `MockDTE`, which lives in `Tests.Design`, and `Tests.Design` already references `Tests.Shared` — moving it would have inverted that edge. It also cannot be made `public`, because it returns the internal `ModelBuilderWizardForm`.

CS0436 drops from 26 to 4. The remaining 4 are `Krafs.Publicizer` injecting `IgnoresAccessChecksToAttribute` into more than one assembly, which is expected. They are left alone deliberately: blanket-suppressing CS0436 would also hide genuine duplicate-type mistakes like the one just fixed.

### 3.3 MSB3243 / MSB3245

Unresolved and conflicting references to `System.Data`, `System.Data.Entity`, `System.Drawing` and `System.Xml.Linq`, all in the net10 targets. These are .NET Framework facades being pulled in by net48 project references. Related to NU1701/NU1702 — the same multi-targeting seam.

## 4. Runtime and tooling gaps

### 4.1 `edmx render` does not run on net10 — FIXED

Same call chain as issue 1.2, and fixed by the same change. The .NET 10 build now renders, producing a **byte-identical SVG to the net48 build** — 51 rects, 36 connector paths, 152 text elements from `Northwind.edmx`.

This was the blocker on shipping `Microsoft.Data.Entity.Tools` as a real `dotnet tool`. Packaging it as one is now a packaging decision rather than a technical obstacle.

### 4.2 DSL regeneration produces code that does not compile

`msbuild -t:TransformAll` works, but the VS 18 DSL SDK templates emit code needing an assembly the project does not reference, and seal an extension point the serialization helper overrides. See `dsl-toolchain-alignment.md`.

### 4.3 Raster export does not work headless

PNG, JPEG, BMP, GIF and TIFF go through `Diagram.CreateBitmap`, which resolves `SVsUIShell`. SVG and Mermaid work. See `headless-edmx-rendering.md`.

## 5. Minor defects found while documenting the Manager classes

Six things surfaced during the documentation sweep. All were deliberately left alone at the time — a formatting pass is the wrong place to change behaviour — and all are small enough to fix individually.

### 5.1 `RdtManager.Dispose` releases nothing

It asserts that its table is empty and returns. Any entry still present is an RDT edit lock whose owner is gone, so the document stays open invisibly for the rest of the Visual Studio session. Either release what remains or make the assert a real failure. `Microsoft.VisualStudio.Data.Tools.Design.XmlCore/Common/RdtManager.cs`.

### 5.2 `EdmFeatureManager` is a no-op

All eleven gates return `FeatureState.VisibleAndEnabled` unconditionally, because only EF6/V3 is supported and V3 is a superset of the versions the gates were written to distinguish. 148 lines that can only answer "yes". Either delete it and inline the answer, or keep it and accept it as a placeholder for a future version split. `Microsoft.Data.Entity.Design.Model/EdmFeatureManager.cs`.

### 5.3 Dead branch in `EditingContextManager.GetCurrentUri`

An `if (context is null)` sits inside a block only reachable when `TryGetValue` already failed, so `context` is always null there and the test always passes. Harmless, but it hides the fact that the code after it is only reachable on a map hit. `Microsoft.VisualStudio.Data.Tools.Design.XmlCore/VisualStudio/Package/EditingContextManager.cs`.

### 5.4 `FeatureState.cs` holds two top-level types and no docs

`FeatureState` and `FeatureSupportedStateExtensions` share a file, and the extension methods are undocumented. Same violation as the Manager classes; it simply was not on that list. `Microsoft.Data.Entity.Design.Model/FeatureState.cs`.

### 5.5 `SchemaManager.GetNamespaceName` asserts, then indexes anyway

For an unsupported version it `Debug.Assert`s and then indexes the dictionary regardless, so a Release build throws `KeyNotFoundException` from inside the lookup rather than failing where the bad version was supplied. The same assert-then-proceed shape that `PackageManager.Package` had. `Microsoft.Data.Entity.Design.VersioningFacade/SchemaManager.cs`.

### 5.6 Rendered SVG depends on the source file's line endings

`SvgStylesheetManager.GetStyleDefinitions` returns a verbatim string literal, so the newlines it emits are literally the bytes in the `.cs` file. With `* text=auto` and `core.autocrlf=true` the repository stores LF and checkout produces CRLF, which means **the rendered SVG differs depending on how the repository was cloned**. The regression baseline is therefore machine specific rather than a stable contract.

Worth fixing at the point of emission — normalise the stylesheet's newlines — rather than by pinning the file's line endings. Note the output is already mixed: `<title>` ends with LF while `<defs>` ends with CRLF, so the writer is inconsistent about newlines more broadly. `Microsoft.Data.Entity.Design.Renderer/Export/Svg/SvgStylesheetManager.cs`.

## 6. Registration defects

### 6.1 The T4 directive processor is registered under a namespace that no longer exists

`PkgDefData\Microsoft.Data.Entity.Design.Package.pkgdef` registers:

```
[$RootKey$\TextTemplating\DirectiveProcessors\T4VSHost]
"Class"="Microsoft.Data.Entity.Design.VisualStudio.Directives.FallbackT4VSHostProcessor"
"CodeBase"="$PackageFolder$\\Microsoft.VisualStudio.Data.Entity.Design.dll"
```

The type's actual full name is `Microsoft.VisualStudio.Data.Entity.Design.Ide.CustomDirectiveProcessor.FallbackT4VSHostProcessor`. The `CodeBase` was updated during the assembly renames; the `Class` was not, so the registration resolves to a type that does not exist and the processor never loads.

Fails silently by design — a T4 template naming `T4VSHost` gets a directive-processor-not-found error at transform time, not at install, so nothing in a build or test run surfaces it. Compare against `EFTools.disabled\Microsoft.Data.Entity.Design.Package.pkgdef.disabled` in the Visual Studio install, which is the shipped original and names the old assembly and the old namespace consistently.

`FallbackT4VSHostProcessor` is a no-op shim whose only job is to satisfy older T4 hosts that lack a built-in `T4VSHost`, so on a current Visual Studio the practical impact may be nil. That should be confirmed rather than assumed before deciding whether to fix the name or drop the registration.

### 6.2 The T4 include folder points at a directory that is never installed

The same pkgdef registers an include path:

```
[$RootKey$\TextTemplating\IncludeFolders\.tt]
"Include67826F5E-E1F5-4618-B91C-957E4A34F0D9"="$RootFolder$Common7\IDE\Extensions\Microsoft\Entity Framework Tools\Templates\Includes\"

```

That folder holds the EF6 DbContext and EntityObject generator `.ttinclude` files in a machine with the original EF tools installed. It does not exist in Visual Studio 18, and this VSIX does not ship it, so the entry registers an include path to nothing.

This is the other half of the same question as 6.1: whether user-authored T4 codegen against an EDMX is in scope at all. If it is, the `.ttinclude` files have to be shipped by this VSIX and the path pointed at them. If it is not, both registrations should go. The related `EntityFrameworkDirectiveProcessor` the DSL SDK generated into the designer has already been deleted as superseded — see the commit that removed `Dsl\GeneratedCode\DirectiveProcessor.cs`.

## Suggested order

1. **2.2** — delete `static` from 17 tests. Minutes, and it tells us whether they pass.
2. ~~**3.2** — duplicate `ModelBuilderWizardFormHelper`.~~ **Done.**
3. ~~**1.3** — `DatabaseGenerationAssemblyLoader` NRE.~~ **Done.**
4. ~~**1.2 and 4.1** — theming reaching the VS shell.~~ **Done**, via decoupling work item 4.
5. ~~**3.1** — vulnerable packages.~~ **Done.**
6. **1.4 and 1.5** — confirm the parallelism theory. Three assemblies and both targets now; one run with parallelism off would settle it.
7. **2.1** — the 186 ignored tests. Largest and least certain; needs a decision about EF6 binaries first.
8. **1.1** — wizard page tests. Needs a design decision about VS-hosted testing.
