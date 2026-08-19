# Known Issues

Everything currently between this repository and "builds clean and all tests pass, every time".

Measured with `dotnet build -c Release --no-incremental` and `dotnet test -c Release`. The solution **compiles with 0 errors**; everything below is warnings, test failures, or tests that silently do not run.

Resolved issues move to `fixed-bugs.md` rather than staying here marked FIXED. **Their numbers are not reused**, so the gaps below are deliberate and a reference to "issue 1.2" still resolves — in the other file.

## Summary

| Category | Count |
|---|---|
| Failing tests | 0 deterministic; 1 named intermittent with a root cause — see 2.5; 2 still unnamed — see 1.5 |
| Tests disabled with `[Ignore]` | 194 |
| Tests that never run because of an invalid signature | 0 — fixed, see `fixed-bugs.md` 2.2 and 2.4 |
| `MSTEST0003` warnings | 0 — was 98 |
| Build warnings | needs a recount; the remaining figures predate 2.2 and 2.4 |
| Packages with known vulnerabilities | 0 |
| Minor defects logged for later | 6 — see section 5 |

## 1. Failing tests

### 1.5 Intermittent single-test failures across assemblies

Single-test failures, each passing when that project was rerun on its own immediately afterwards:

| Assembly | Target | Status |
|---|---|---|
| `Microsoft.Data.Entity.Tests.Design` | net48 | **named and fixed** — `Generate_returns_code`, see `fixed-bugs.md` 1.4 |
| `Microsoft.Data.Entity.Tests.Design.EntityFramework` | net10.0 | **named** — see 2.5, root cause found |
| `Microsoft.Data.Entity.Tests.Design.VersioningFacade` | net10.0 | still unnamed, full solution 294/295 |
| `Microsoft.VisualStudio.Data.Entity.Tests.Package` | net48 | still unnamed, full solution 59/60 (2026-08-17) |

**Two of the four are now identified, and both were the same defect shape: a one-time initialization whose "done" state becomes visible to another thread before the work is finished.** Start there for the remaining two.

The EntityFramework row was named on the **first run after trx logging became unconditional**, having escaped four reactive attempts before that. That is the whole lesson of this entry.

> **Always run with `--logger "trx" --results-directory <dir>`. Every run, not just the one after you have already seen a failure.**

An intermittent is by definition gone by the time you go looking for it. The console logger reports the count on the summary line but the name scrolls past in a full-solution run, so the instinct is to rerun with trx — and the rerun is the run that passes. Capturing on every run costs nothing and is the only thing that ever works; capturing reactively has now failed on four separate sightings.

Read the names out with:

```bash
grep -ho 'testName="[^"]*"[^>]*outcome="Failed"' <dir>/*.trx
```

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

### 2.5 `StoreSchemaConnectionFactory.EnsureSqlClientRegistered` races — this is the 1.5 intermittent

**Named on the first run after trx logging became unconditional.** `Create_creates_valid_EntityConnection` and `Create_creates_valid_EntityConnection_and_returns_EF_version`, in `StoreSchemaConnectionFactoryTests`, net10 only:

```
System.ArgumentException: The specified invariant name 'System.Data.SqlClient'
wasn't found in the list of registered .NET Data Providers.
   at System.Data.Common.DbProviderFactories.GetFactory(String, Boolean)
```

The cause is in **production code**, `Microsoft.Data.Entity.Design.EntityFramework/ReverseEngineerDb/StoreSchemaConnectionFactory.cs`:

```csharp
if (_sqlClientRegistrationChecked) { return; }
_sqlClientRegistrationChecked = true;      // set before the work it guards

try { DbProviderFactories.GetFactory(SqlClientInvariantName); }
catch (ArgumentException) { DbProviderFactories.RegisterFactory(...); }
```

**The flag is set before the registration it is meant to guard.** Thread A sets it and starts registering; thread B sees `true`, returns immediately, and calls `GetFactory` against a registry nothing has populated yet.

net10 only because `DbProviderFactories` is a process-wide static registry with no ambient content on .NET Core — .NET Framework resolves the provider from machine.config and never enters this path.

This is the same defect as `fixed-bugs.md` 1.4, one layer down: a one-time initialization whose "done" marker becomes visible before the work is. **Fix it the same way** — `Lazy<T>` with `LazyThreadSafetyMode.ExecutionAndPublication`, or a lock around the whole check-and-register — and note it is a real concurrency bug in shipping code, not only a test problem.

## 3. Build warnings — 181

| Count | Code | What |
|---|---|---|
| 168 | NU1701 | package restored using `.NETFramework` fallback rather than a matching target |
| 0 | MSTEST0003 | was 98 — fixed, see `fixed-bugs.md` 2.2 and 2.4 |
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

1. **2.5** — the `EnsureSqlClientRegistered` race. Named, root-caused, and a real concurrency bug in shipping code.
2. **1.5** — the unnamed intermittents. Costs nothing to progress: run every suite with trx from now on and the next sighting names itself.
3. **2.1** — the 194 ignored tests. Largest and least certain; needs a decision about EF6 binaries first.
4. **6.1 and 6.2** — the T4 registrations. Needs a scope decision about EDMX T4 codegen before either is worth touching.
