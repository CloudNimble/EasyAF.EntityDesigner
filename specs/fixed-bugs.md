# Fixed Bugs

Defects that were on `known-issues.md` and have since been resolved. The entries are kept because the reasoning behind each fix is worth having — several describe traps that are easy to walk back into.

Numbers are the ones the issues carried in `known-issues.md`. They are not reused there, so a reference to "issue 1.2" still resolves, here.

## 1. Failing tests

### 1.1 Wizard pages need a live VS shell — 10 failures

**All ten now pass.** They were fixed by deleting the `Services` service locator, not by anything aimed at the tests. That class cached its `IServiceProvider` in a `??=` static which nothing ever cleared: a `MockPackage` from one test stayed live for every test that followed, long after the test that created it had disposed it. Every call site now reads `PackageManager.Package`, which each test sets for itself.

This was verified by bisection — restoring the old `Debug.Assert` in `PackageManager.Package` while keeping the locator deleted still gives 60 passed / 0 failed, so the locator was the cause and the assert change was not.

The analysis below is kept because the cross-assembly interference it describes is real and still applies to anything that leans on shell statics.

Run the project on its own and this used to be a deterministic 10. Run the whole solution and the count drifts by one or two, and the extra names move between assemblies — `DispatchSaveToExtensions_invokes_serializers_if_present` in `Tests.Design.Package` one run, a pair in `Tests.Design` the next, neither reproducible in isolation.

That is the same root cause as the ten, one level up: the shell statics these tests lean on (`ThreadHelper.JoinableTaskContext`, the `IVsSettingsManager` service lookup) are process-wide, and test assemblies run concurrently. Whichever assembly initialises the shell first decides what the others see. `UsesDslStore` (see `threading-model.md`) serialises tests *within* an assembly; nothing serialises them *across* assemblies.

So when comparing runs, compare against a per-project baseline, not the solution total. A regression shows up as a deterministic isolated failure.

The ten, in `Microsoft.Data.Entity.Tests.Design.Package` (net48):

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

### 1.2 Theming reaches the VS shell on net10

Three renderer tests failed on net10 with `This is a reference assembly.` from
`EntityDesignerSurface.IsThemeServiceAvailable()`, which resolved `SVsUIShell` through
`Package.GetGlobalService` to decide whether it was running inside Visual Studio.

Theming is now pushed in by the host rather than pulled by the designer. The Dsl declares a `DiagramPalette` of semantic colors with defaults; `Microsoft.Data.Entity.Design.Package` reads Visual Studio's theme and calls `DiagramTheme.Apply`. Nothing on a headless path touches `Microsoft.VisualStudio.Shell`, and `IsThemeServiceAvailable` is deleted — the designer no longer asks whether it is inside Visual Studio, which was never a question it should have had to answer.

`Microsoft.Data.Entity.Tests.Design.Renderer` now passes **138/138 on both net48 and net10**.

### 1.3 `DatabaseGenerationAssemblyLoader` null reference

`AssemblyLoader_passed_WebsiteProject_can_find_correct_paths_to_DLLs` threw at the `foreach` over `vsWebSite.References`.

The production code was fine; the defect was in `MockDTE.CreateVsWebsite`. `foreach` binds to the strongly typed `AssemblyReferences.GetEnumerator()`, not the `IEnumerable` one, and the helper set up only the latter — so the former returned null and the loop threw. `CreateVsProject2` sets up both, which is why the project-reference test alongside it always passed.

It also had a second, latent defect: `Returns(references.GetEnumerator())` evaluated the enumerator once at setup instead of `Returns(() => ...)`, so a second enumeration would have received an already-exhausted enumerator.

`Microsoft.Data.Entity.Tests.Design` now passes 455 of 481 with 26 ignored, stable across four runs.

### 1.4 `Generate_returns_code` intermittent — fixed 2026-08-19

`System.InvalidOperationException: Sequence contains no matching element`, intermittent for months.

**The cause was `GeneratorTestBase.Model`, an unguarded lazy static:**

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

An entity set from A handed to the generator alongside model B means `TableDiscoverer.Discover` looks the set up in B's `ConceptualToStoreMapping.EntitySetMappings`, matches nothing, and `First` throws.

The fix is `Lazy<DbModel>` with `LazyThreadSafetyMode.ExecutionAndPublication`, so the instance is built once and never replaced. Not `[DoNotParallelize]`, which would have hidden a real defect behind slower tests.

Verified: the reproducing filter, which failed 3 of 3 immediately before, passed **10 of 10**; the whole assembly passed 455/481 three times running.

**Three things this cost, worth not repeating:**

- **The test was misnamed for months.** Every earlier entry said `DefaultCSharpEntityTypeGeneratorTests`. It is `DefaultVBEntityTypeGeneratorTests`. Four classes share the base and all four have a method called `Generate_returns_code`, and the console summary never says which.
- **The "ordering hazard" theory was the wrong explanation for a real observation.** A smaller filtered run *was* more likely to fail, which looked backwards for a race. It is not: a small run starts all four classes at once with the field still null, which is the widest possible window. The parallelism theory in 2.3 was right all along.
- **Console summary lines interleave in a full-solution run**, so a `Failed!` line can appear glued to the wrong assembly name. A run reading `...Duration: 2 sFailed! - Failed: 1, Passed: 294, Skipped: 95, Total: 390 - ...Diagrams.dll (net48)` was actually the EntityFramework assembly — `390 total / 95 skipped` is its shape, while `Tests.Design.Diagrams` has 5 tests. Match counts to the assembly before believing a name, or use `--logger trx` from the start.

This also resolved the `Microsoft.Data.Entity.Tests.Design` (net48) row of issue 1.5, which was never a distinct test. The other two rows of 1.5 remain open.

## 2. Tests that never run

### 2.2 17 tests declared `static` — fixed 2026-08-19

MSTest will not run a `static` test method. It emits `MSTEST0003` and the method silently never executes — it is not even reported as skipped.

| File | Count |
|---|---|
| `EntityFramework/ReverseEngineerDb/OneToOneMappingBuilderTests.GenerateEdmFunctionsTests.cs` | 11 |
| `EntityFramework/ReverseEngineerDb/UniqueIdentifierServiceTests.cs` | 4 |
| `EntityFramework/ReverseEngineerDb/DbDatabaseMappingBuilderTests.cs` | 2 |

**They were not made static recently, and not to hide anything.** All 17 were already `public static void` at `721bc51` (2021-08-03), inherited from the original EF6 repository, where they were xUnit `[Fact]` methods. **xUnit runs static test methods; MSTest does not.** The January 2026 conversion from `[Fact]` to `[TestMethod]` turned a legal xUnit signature into one MSTest silently skips.

Fixed by deleting the `static` keyword. Measured before and after on `Microsoft.Data.Entity.Tests.Design.EntityFramework`:

| | Total | Passed | Skipped |
|---|---|---|---|
| Before | 390 | 295 | 95 |
| After | **407** | **304** | **103** |

+17 collected, exactly the count expected. Nine of them run and **all nine pass**; the other eight carry `[Ignore]`. No new failures, on either target.

Note the earlier entry located these files under `VersioningFacade`. They are in `Microsoft.Data.Entity.Tests.Design.EntityFramework`.

**This did not clear MSTEST0003** — see `known-issues.md` 2.4, a second and unrelated cause that was hidden behind this one.

### 2.4 `[TestMethod]` on eight private helper methods — fixed 2026-08-19

Surfaced by fixing 2.2, which was masking it. Eight private helpers had picked up a stray `[TestMethod, Ignore(...)]` during the same `[Fact]` to `[TestMethod]` conversion:

| File | Method |
|---|---|
| `EntityFramework/.../DbDatabaseMappingBuilderTests.cs` | `CreateSimpleMappingContext` |
| `EntityFramework/.../OneToOneMappingBuilderTests.cs` | `GetLazyLoadingMetadataProperty` |
| `EntityFramework/.../OneToOneMappingBuilderTests.GenerateEdmFunctionsTests.cs` | `CreateStoreModel` |
| `EntityFramework/.../StoreModelBuilderTests.CreateAssociationSetsTests.cs` | `Check_does_not_create_set_if_end_entity_is_missing` |
| `EntityFramework/.../StoreModelBuilderTests.CreateAssociationSetsTests.cs` | `Check_two_column_relationship_..._pk_to_pk` |
| `EntityFramework/.../StoreModelBuilderTests.CreateAssociationSetsTests.cs` | `Check_two_column_relationship_..._pk_to_fk` |
| `EntityFramework/.../StoreModelBuilderTests.CreateAssociationSetsTests.cs` | `Check_cascade_delete_flag_is_reflected_by_delete_behavior` |
| `Tests.Package/.../CodeFirstModelBuilderEngineTests.cs` | `CreateDbModel` |

All are `private`, take parameters, and most return a value, so none could ever have been collected as a test. Seven are called by live tests; `CreateDbModel`'s call sites are commented out along with the tests that used them.

**Fixed by deleting the attribute, not the method** — `[Ignore]` on a private method does nothing either, so both halves were noise. No behaviour change. The methods are kept because the commented-out tests around them will need them if those are ever restored.

The eighth was only visible after the other seven were cleared; it is in a different assembly that the original 2.2 survey never covered.

**`MSTEST0003` is now 0 across the solution**, down from 98.

### 2.3 Store-building tests race under parallelism

**The Visual Studio Modeling SDK cannot be driven from more than one thread in a process.** It assumes a single threaded host and keeps unsynchronized process wide state in at least two places:

- `DomainXmlSerializerDirectory.InternalAddBehavior`, reached from `CoreDesignSurfaceSerializationHelperBase.InitializeSerialization` during **store construction**.
- `StoreDiagramMappingData.GetInstance(Store)`, a static `Dictionary` keyed by `Store` that `DiagramCommittingRule.TransactionCommitting` writes to on **every transaction commit**.

Symptoms are a `NullReferenceException` from `Dictionary.Insert`, `Collection was modified; enumeration operation may not execute`, or a torn dictionary that crashes the test host outright.

**Locking store construction is not sufficient, and this was measured rather than assumed.** With a lock around `new Store(...)` and parallelism otherwise enabled, 12 runs produced 7 clean, 1 with a single failure, 1 with two, and one host crash. The commit path still raced, because the state is mutated from inside SDK rules that no amount of care at our call sites can guard.

The constraint is therefore declared **per assembly**, in `Directory.Build.props`: a test project sets `UsesDslStore` to opt out of the blanket `[Parallelize(MethodLevel)]` and receive `[DoNotParallelize]` instead. `Microsoft.Data.Entity.Tests.Design.Dsl` and `Microsoft.Data.Entity.Tests.Design.Renderer` set it.

This replaces per-class `[DoNotParallelize]`, which was the previous approach on four classes. Per class only works if every future author remembers, and the failures are intermittent enough to survive review — the same trap that produced this entry.

Verified: 12 consecutive runs of the Dsl suite and 8 of the Renderer suite, all deterministic.

## 3. Build warnings

### 3.1 Vulnerable packages

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

### 3.2 CS0436 duplicate type

`ModelBuilderWizardFormHelper` was defined twice, in `Microsoft.Data.Entity.Tests.Design` and again in `Microsoft.Data.Entity.Tests.Design.Package`, in the same namespace, with the projects referencing each other. The copies were byte-identical apart from a BOM, using order and a trailing newline. The Package copy is deleted; `Tests.Design.Package` already referenced `Tests.Design`, which already exposed its internals to it, so nothing else was needed.

Note it could not move to `Microsoft.Data.Entity.Tests.Shared` as first suggested: it depends on `MockDTE`, which lives in `Tests.Design`, and `Tests.Design` already references `Tests.Shared` — moving it would have inverted that edge. It also cannot be made `public`, because it returns the internal `ModelBuilderWizardForm`.

CS0436 drops from 26 to 4. The remaining 4 are `Krafs.Publicizer` injecting `IgnoresAccessChecksToAttribute` into more than one assembly, which is expected. They are left alone deliberately: blanket-suppressing CS0436 would also hide genuine duplicate-type mistakes like the one just fixed.

## 4. Runtime and tooling gaps

### 4.1 `edmx render` does not run on net10

Same call chain as issue 1.2, and fixed by the same change. The .NET 10 build now renders, producing a **byte-identical SVG to the net48 build** — 51 rects, 36 connector paths, 152 text elements from `Northwind.edmx`.

This was the blocker on shipping `Microsoft.Data.Entity.Tools` as a real `dotnet tool`. Packaging it as one is now a packaging decision rather than a technical obstacle.
