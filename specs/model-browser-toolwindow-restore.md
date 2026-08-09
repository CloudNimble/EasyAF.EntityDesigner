# Model Browser Tool Window Restore

## Symptom

With the Model Browser docked and visible when VS launches, and no solution or `.edmx` loaded, the tool window frame renders:

```
System.ArgumentNullException: Value cannot be null.
Parameter name: persistenceSlot
   at Microsoft.VisualStudio.Modeling.Shell.ModelingPackage.CreateToolWindow(Guid& persistenceSlot)
```

It clears as soon as an `.edmx` is opened, and the window works normally for the rest of the session.

## Evidence

Exp hive activity log (`$env:APPDATA/Microsoft/VisualStudio/18.0_93d9f2a2Exp/ActivityLog.xml`, UTF-16):

```
Frame identifier: ST:0:0:{a34b1c5d-6d37-4a0c-a8b0-99f8e8158b48}
Frame caption:    Model Browser
```

That GUID is `PackageConstants.guidExplorerWindowString`, the `[Guid]` on `EntityDesignExplorerWindow`.

## Root cause

The package owns two tool windows with different base classes:

| | `MappingDetailsWindow` | `EntityDesignExplorerWindow` |
|---|---|---|
| Base class | `TreeGridDesignerToolWindow` → DSL `ToolWindow` | `ExplorerWindow` → shell `ToolWindowPane` |
| `[ProvideToolWindow]` | yes | yes |
| `AddToolWindow` in `Initialize` | yes | **no — cannot be** |
| Retrieved by | `GetToolWindow` (DSL) | `FindToolWindow` (shell) |

On startup the shell asks for each persisted frame through `IVsPackage.CreateTool(ref Guid)`. That reaches `ModelingPackage.CreateToolWindow`, which resolves persistence slots **only** against the registry `AddToolWindow` fills, and never falls back to attribute-based resolution — so `[ProvideToolWindow]` alone is not enough for a `ModelingPackage`. The Explorer's slot isn't in that registry, the lookup yields nothing, and the DSL code throws naming its own `persistenceSlot` parameter.

It self-heals because opening an `.edmx` reaches the `ExplorerWindow` property, which calls `FindToolWindow` — the base `Package` path, which *does* honour the attribute.

## Approaches that do not work

Verified against the compiler, not assumed:

1. **`AddToolWindow(typeof(EntityDesignExplorerWindow))`.** Compiles, but `GetToolWindow` returns DSL `ToolWindow` and `ExplorerWindow` derives from shell `ToolWindowPane` — `CS0039`, no conversion. The DSL registry is typed end-to-end to `ToolWindow`, so a `ToolWindowPane` can never live in it.
2. **Overriding `CreateToolWindow(ref Guid, uint)`.** `CS0506` — inherited from `ModelingPackage` but not virtual.
3. **Overriding `CreateToolWindow(ref Guid)`.** `CS0115` — the one-argument form in the stack trace is private.
4. **Overriding `InstantiateToolWindow(Type)`.** Overridable, but it sits downstream of the GUID→Type lookup that fails, so it is never reached.

## Fix applied

`Transient = true` on the Explorer's `[ProvideToolWindow]` in `MicrosoftDataEntityDesignPackage.cs`.

Transient windows are kept out of the persisted layout, so the shell never attempts the restore that this package cannot service. The window is still created on demand through `FindToolWindow`, which honours the attribute, and `[ProvideToolWindowVisibility(..., MicrosoftDataEntityDesignEditorFactoryId)]` already ties it to the EDMX editor context — so tying its lifetime to a document rather than to the window layout matches the existing declaration.

**Behaviour change:** the Model Browser no longer reappears docked when VS starts. It appears when an `.edmx` is opened. Previously it did reappear, but as an exception box that was non-functional until a model loaded.

## Verification

Not yet run. Steps:

1. Build, launch the Exp hive: `devenv.exe /RootSuffix Exp /log`
2. Open an `.edmx`, open the Model Browser, dock it, close the document and solution, exit VS.
3. Relaunch with no solution. The Model Browser should be **absent**, with no exception in any frame.
4. Open an `.edmx`. The Model Browser should appear and populate.
5. Confirm the log is clean:
   `iconv -f UTF-16 -t UTF-8 "$APPDATA/Microsoft/VisualStudio/18.0_93d9f2a2Exp/ActivityLog.xml" | grep -i persistenceSlot`

**Caveat:** a layout persisted by a previous build still contains the frame. The first launch after this change may still attempt one restore before the layout is rewritten. If the exception appears once and never again, that is the stale layout, not a failure of the fix — confirm with Window → Reset Window Layout or a fresh Exp hive.

## If restore-at-startup must be preserved

Explicitly re-implementing `IVsPackage.CreateTool(ref Guid)` on the package compiles cleanly and is the only viable interception point: handle the Explorer's slot via `FindToolWindow(typeof(EntityDesignExplorerWindow), 0, true)` and return `S_OK`, delegating every other slot to `CreateToolWindow(ref slot, 0)`. Re-implementing the interface re-maps all of `IVsPackage`, with unimplemented members binding to `ModelingPackage`'s public methods. This was not taken because it trades a one-attribute change for shell-interop that only the Exp hive can validate, to restore a window that is empty without a document.

`MappingDetailsWindow` is unaffected — it is in the DSL registry — and should be confirmed as such during verification. If it shows the same exception, this analysis is wrong and should be redone rather than patched.
