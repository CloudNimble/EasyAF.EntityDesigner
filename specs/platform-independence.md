# Platform Independence

The rule that governs where code goes. Everything else in `specs/` is an application of it.

## The rule

**The core owns decisions. The shell owns the platform.**

The core — the model, the designer, the model builders — contains logic and nothing platform specific. It does not know whether Visual Studio exists, whether a screen exists, or whether Windows exists. Anything it needs from outside arrives one of two ways:

1. **Pushed in**, for values and services it needs continuously while working.
2. **Requested by event**, for anything that needs an outer system to act — above all, anything that needs a human.

Nothing is pulled. The core never reaches out to ask whether a host is present, and never calls a platform API to find out what to do.

## Visual Studio dependence and Windows dependence are the same problem

This is easy to get wrong, and has been got wrong here at least once. Removing `VSColorTheme` from the designer while moving GDI bitmap work *into* it is not progress — it trades one platform dependency for another and makes the harder one worse. Visual Studio, WinForms, WPF, GDI, the registry, the file system: all the same category.

The test is not "does this reference a Visual Studio assembly". The test is **"could this run somewhere that has no user interface at all"**.

## Mechanism 1: push

For things the core needs while it works, the host supplies them once and the core reads them.

```csharp
// core declares what it needs, with a default that works headless
internal static class DiagramTheme
{
    internal static DiagramPalette Current { get; }
    internal static void Apply(DiagramPalette palette);
}

// shell supplies it, and owns the lifetime of anything it subscribes to
_diagramTheme = new VsDiagramTheme();   // reads the VS theme, pushes it in, disposes cleanly
```

Push suits values and providers: colors, icons, a clock, a file system. It has no callback cost at the point of use, which matters when the point of use is a paint loop.

**The default must be a working default, not a null.** That is what lets the same assembly run under the command line with nothing supplied.

## Mechanism 2: events

For anything that requires the outer system to *do* something — show a dialog, pick a file, authenticate — the core raises an event describing what it needs and waits for an answer. The shell subscribes at construction, does whatever it does, and calls back into core functions with the result.

```csharp
// core
internal event EventHandler<ConnectionStringRequestedEventArgs> ConnectionStringRequested;

// shell, at construction
engine.ConnectionStringRequested += (s, e) =>
{
    using var dialog = new EntityDataConnectionDialog(...);
    if (dialog.ShowModal() == true)
    {
        e.ConnectionString = dialog.ConnectionString;   // shell hands back a value
    }
};
```

The important part is the **direction**: the core says what it needs, not how to get it. A command line host answers the same event from a `--connection` argument. An AI host answers it from configuration. No dialog type appears anywhere in the core.

## Decisions are functions

The corollary, and the one that has been violated most.

Anything that is a *decision* — validating a path, deriving a name, building a connection string, choosing a default, deciding whether a file may be overwritten — is a function in the core. It takes values and returns values. It does not live on a form.

Today `WizardPageStart.OnDeactivate` computes a model path, validates it, and maps a GUI selection into settings. `WizardPageDbConfig.GetTextBoxConnectionStringValue` formats a connection string. Those are functions wearing a `UserControl`. The proof is that testing them requires constructing a WinForms control, which requires `DpiHelper`, which requires a live Visual Studio shell — ten tests currently fail for exactly this reason.

A wizard page should collect input, call a core function, and display the result. Nothing else.

## What this does not claim

The designer is built on the Visual Studio Modeling SDK, and that SDK is WinForms and GDI to its foundations: `ShapeElement`, `Diagram`, `StyleSet` and `PointD` all come from `Microsoft.VisualStudio.Modeling.Diagrams`. **While the designer is built on the SDK it cannot be fully platform free**, and pretending otherwise leads to busywork.

The achievable target is narrower and still worth a lot: the designer contains no platform code *of ours*. No bitmap rasterization, no theme lookups, no service provider queries, no dialogs. What remains is the SDK's own surface, which is a single, known, replaceable dependency rather than a hundred scattered ones.

Replacing the SDK with a model that carries its own layout and routing is the only way past that, and it is not this work.

## Applying it

| Symptom | Fix |
|---|---|
| Core asks "am I inside Visual Studio?" | push the answer in as a value with a working default |
| Core constructs a dialog | raise an event, let the shell construct it |
| Core rasterizes, colorizes or paints | move it to the shell; push the results back if the core needs them |
| Logic lives on a form | make it a function in the core; the form calls it |
| A test needs a shell to run | the logic under test is in the wrong layer |

That last row is the cheapest detector available. A test that needs Visual Studio running is telling you something about the architecture, not about the test.
