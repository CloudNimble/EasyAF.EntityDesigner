# Known Issues

Everything currently between this repository and "builds clean and all tests pass, every time".

Measured with `dotnet build -c Release --no-incremental` and `dotnet test -c Release`. The solution **compiles with 0 errors**; everything below is warnings, test failures, or tests that silently do not run.

Resolved issues move to `fixed-bugs.md` rather than staying here marked FIXED. **Their numbers are not reused**, so the gaps below are deliberate and a reference to "issue 1.2" still resolves — in the other file.

## Summary

| Category | Count |
|---|---|
| Failing tests | 0 deterministic; 2 unnamed intermittents — see 1.5 |
| Tests disabled with `[Ignore]` | 186 |
| Tests that never run because of an invalid signature | 17 |
| Build warnings | 284 |
| Packages with known vulnerabilities | 0 |
| Minor defects logged for later | 6 — see section 5 |

## 1. Failing tests

### 1.5 Two unnamed intermittent failures

Single-test failures seen during the 2026-08-16 project file work, each in a different assembly, each passing when that project was rerun on its own immediately afterwards:

| Assembly | Target | Run |
|---|---|---|
| `Microsoft.Data.Entity.Tests.Design.VersioningFacade` | net10.0 | full solution, 294/295 |
| `Microsoft.VisualStudio.Data.Entity.Tests.Package` | net48 | full solution, 59/60 (2026-08-17) |
| `Microsoft.Data.Entity.Tests.Design.EntityFramework` | net10.0 | full solution, 294/295 (2026-08-19) |

The third row is a fresh sighting, seen once and not reproduced: the assembly passed 295/295 on its own immediately afterwards, and three consecutive full-solution runs with trx enabled from the start were all clean. So the name escaped again — the trx run is the one that passes.

**Neither name has ever been captured.** The console logger reports the count on the summary line but the name scrolls past in a full-solution run, and a rerun to capture it is exactly the run that passes. **Use `--logger "trx" --results-directory <dir>` from the start, not as a follow-up**, and read `outcome="Failed"` out of the `.trx`, or the name is lost every time.

A third row of this issue — `Microsoft.Data.Entity.Tests.Design` (net48) — was named on 2026-08-18, turned out to be `Generate_returns_code`, and is fixed. See `fixed-bugs.md` 1.4.

**Start with the same suspicion that one turned out to be:** a `Lazy`, `??=` or null-checked static that the failing class shares with its siblings, unsafe under `MethodLevel` parallelism. That is what 1.4 was, and `fixed-bugs.md` 1.1 and 2.3 were both shared-state problems too.

Do not read the VersioningFacade row as pointing at `DbDatabaseMappingBuilderTests`. The string `Different API visibility between official dll and locally built one` appeared next to the failure in the console output and looks like an assertion message, but it is the `[Ignore]` reason on a skipped test in that file and has nothing to do with it.

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

## 3. Build warnings — 181

| Count | Code | What |
|---|---|---|
| 168 | NU1701 | package restored using `.NETFramework` fallback rather than a matching target |
| 98 | MSTEST0003 | invalid test method signature — see 2.2 |
| 20 | SYSLIB0051 | obsolete formatter-based serialization |
| 20 | MSB3245 | could not resolve `System.Data`, `System.Data.Entity`, `System.Drawing` |
| 18 | NU1702 | project restored using a fallback framework |
| 4 | CS0436 | `Krafs.Publicizer` attribute in several assemblies — expected, see `fixed-bugs.md` 3.2 |
| 14 | MSB3243 | version conflict on `System.Data`, `System.Drawing`, `System.Xml.Linq` |
| 4 | VSTHRD110 | observe the result of async calls |
| 4 | VSTHRD002 | synchronously blocking on async work |
| 4 | SYSLIB0003 | code access security attributes are obsolete |
| 2 | VSSDK004, NU1603, CS0672, CA2022 | one each |

### 3.3 MSB3243 / MSB3245

Unresolved and conflicting references to `System.Data`, `System.Data.Entity`, `System.Drawing` and `System.Xml.Linq`, all in the net10 targets. These are .NET Framework facades being pulled in by net48 project references. Related to NU1701/NU1702 — the same multi-targeting seam.

## 4. Runtime and tooling gaps

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
2. **2.1** — the 186 ignored tests. Largest and least certain; needs a decision about EF6 binaries first.
3. **1.5** — the two unnamed intermittents. Cheap to progress: run with trx from the start and capture the names.
4. **6.1 and 6.2** — the T4 registrations. Needs a scope decision about EDMX T4 codegen before either is worth touching.
