# Known Issues

Everything currently between this repository and "builds clean and all tests pass, every time".

Measured with `dotnet build -c Release --no-incremental` and `dotnet test -c Release`. The solution **compiles with 0 errors**; everything below is warnings, test failures, or tests that silently do not run. Items marked FIXED have been resolved since the list was first taken at `2d94be6`; their entries are kept because the reasoning behind the fix is worth having.

## Summary

| Category | Count |
|---|---|
| Failing tests | 0 — see 1.1 |
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

### 1.4 `Generate_returns_code` intermittent — 1 rare failure

`DefaultCSharpEntityTypeGeneratorTests.Generate_returns_code`, `System.InvalidOperationException: Sequence contains no matching element`. Observed once in roughly ten runs; did not reproduce in six consecutive runs afterwards.

Suspect the same hazard as issue 2.3: MSTest runs at `MethodLevel` parallelism and something here is not thread safe. Worth confirming with `[DoNotParallelize]` on that class and a few hundred runs.

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

## Suggested order

1. **2.2** — delete `static` from 17 tests. Minutes, and it tells us whether they pass.
2. ~~**3.2** — duplicate `ModelBuilderWizardFormHelper`.~~ **Done.**
3. ~~**1.3** — `DatabaseGenerationAssemblyLoader` NRE.~~ **Done.**
4. ~~**1.2 and 4.1** — theming reaching the VS shell.~~ **Done**, via decoupling work item 4.
5. ~~**3.1** — vulnerable packages.~~ **Done.**
6. **1.4** — confirm the parallelism theory.
7. **2.1** — the 186 ignored tests. Largest and least certain; needs a decision about EF6 binaries first.
8. **1.1** — wizard page tests. Needs a design decision about VS-hosted testing.
