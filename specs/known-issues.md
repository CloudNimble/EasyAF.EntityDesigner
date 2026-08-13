# Known Issues

Everything currently between this repository and "builds clean and all tests pass, every time".

Measured with `dotnet build -c Release --no-incremental` and `dotnet test -c Release`. The solution **compiles with 0 errors**; everything below is warnings, test failures, or tests that silently do not run. Items marked FIXED have been resolved since the list was first taken at `2d94be6`; their entries are kept because the reasoning behind the fix is worth having.

## Summary

| Category | Count |
|---|---|
| Failing tests | 14 |
| Tests disabled with `[Ignore]` | 186 |
| Tests that never run because of an invalid signature | 17 |
| Build warnings | 181 |
| Packages with known vulnerabilities | 0 — fixed, see 3.1 |

## 1. Failing tests

### 1.1 Wizard pages need a live VS shell — 10 failures

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

### 1.2 Theming reaches the VS shell on net10 — 3 failures

`Microsoft.Data.Entity.Tests.Design.Renderer` (net10.0 only; net48 passes 138/138):

- `Load_produces_a_diagram_with_positioned_shapes_and_routed_connectors`
- `Load_renders_a_model_whose_database_provider_is_not_installed`
- `Dsl_creates_shapes_and_routes_connectors_without_a_shell`

```
System.InvalidOperationException: This is a reference assembly.
   at Microsoft.VisualStudio.Shell.Package.GetGlobalService(Type serviceType)
   at EntityDesignerSurface.IsThemeServiceAvailable()   EntityDesignerSurface.cs:1972
   at EntityDesignerSurface.SetColorTheme(StyleSet)      EntityDesignerSurface.cs:1950
```

`Microsoft.VisualStudio.Shell` resolves to a compile-only asset under .NET 10, so touching it at all throws. **Expected to be fixed by work item 4 of `dsl-shell-decoupling.md`**, which moves theming out of the Dsl project. Same root cause as issue 4.1 below.

### 1.3 `DatabaseGenerationAssemblyLoader` null reference — 1 failure

`Microsoft.Data.Entity.Tests.Design` (net48): `AssemblyLoader_passed_WebsiteProject_can_find_correct_paths_to_DLLs`

```
System.NullReferenceException
   at DatabaseGenerationAssemblyLoader.CacheWebsiteReferences(VSWebSite vsWebSite)
     DatabaseGenerationAssemblyLoader.cs:69
   at DatabaseGenerationAssemblyLoader..ctor(Project, string)  line 43
```

Fails consistently. Not yet diagnosed — the test supplies a website project and the loader dereferences something that is null. Needs someone to read `CacheWebsiteReferences` against what the test provides.

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

**Fix:** delete the `static` keyword. Then find out whether they pass — some may have been made static precisely because they were failing.

### 2.3 Store-building tests race under parallelism

Not currently failing, but latent. Constructing a DSL `Store` mutates process-wide state through `DomainXmlSerializerDirectory.InternalAddBehavior`, so two tests doing it concurrently fail with a null reference or `Collection was modified`. Every Store-building test class needs `[DoNotParallelize]`. Currently correct in `EdmxDiagramLoaderTests`, `HeadlessRoutingSpikeTests`, `SvgShapeRendererTests` and `EntityDesignerSurfaceTransactionTests`; any new one will hit it.

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

### 4.1 `edmx render` does not run on net10

Builds and starts, reaches `RenderCommand.OnExecute`, throws `This is a reference assembly.` Same call chain as issue 1.2, and blocks shipping `Microsoft.Data.Entity.Tools` as a real `dotnet tool`. See `dsl-shell-decoupling.md`.

### 4.2 DSL regeneration produces code that does not compile

`msbuild -t:TransformAll` works, but the VS 18 DSL SDK templates emit code needing an assembly the project does not reference, and seal an extension point the serialization helper overrides. See `dsl-toolchain-alignment.md`.

### 4.3 Raster export does not work headless

PNG, JPEG, BMP, GIF and TIFF go through `Diagram.CreateBitmap`, which resolves `SVsUIShell`. SVG and Mermaid work. See `headless-edmx-rendering.md`.

## Suggested order

1. **2.2** — delete `static` from 17 tests. Minutes, and it tells us whether they pass.
2. ~~**3.2** — duplicate `ModelBuilderWizardFormHelper`.~~ **Done.**
3. **1.3** — one real bug, one test, self contained.
4. **1.2 and 4.1** — fall out of `dsl-shell-decoupling.md` work item 4. No separate effort.
5. ~~**3.1** — vulnerable packages.~~ **Done.**
6. **1.4** — confirm the parallelism theory.
7. **2.1** — the 186 ignored tests. Largest and least certain; needs a decision about EF6 binaries first.
8. **1.1** — wizard page tests. Needs a design decision about VS-hosted testing.
