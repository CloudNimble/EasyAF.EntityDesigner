# Unified Theming

One setting that drives how entities look in both the Visual Studio designer and the SVG the command line tool emits.

**Status: plan, not implemented.** For review.

## Why this needs a plan

There are three independent color systems today. Changing "the colors entities render in" currently means editing all three, in different ways, and they can silently disagree.

| # | System | Lives in | Drives | Settable |
|---|---|---|---|---|
| 1 | `DiagramPalette` / `DiagramTheme` | `...Design.Dsl` | designer chrome: surface background, zoom lasso, emphasis outline, shape text | **yes** — host pushes a palette |
| 2 | `EntityTypeShape.FillColor` | the EDMX, per `<EntityTypeShape>` | entity header and body fill, and by extension icon colorization | it is model data, not a setting |
| 3 | `SvgStylesheetManager` | `...Design.Renderer` | **everything the CLI emits** | **no** — 14 hardcoded hex literals |

System 1 was introduced when theming was inverted so only the package talks to Visual Studio. It is the right shape but the wrong scope: it covers chrome, and **has no effect on SVG output at all**, because the exporter reads no `StyleSet`.

Two further problems worth fixing in the same pass:

- **The brightness math is implemented twice.** The WCAG relative luminance formula (`0.2126 R + 0.7152 G + 0.0722 B`, with the sRGB gamma ramp) appears in `EntityTypeShape.cs` and again in `SvgStylesheetManager.cs`. Both decide "black or white text on this fill". Two copies means two places to change and a guaranteed drift.
- **`DiagramImageHelper` reaches into the VS project.** It calls `ThemeUtils.GetThemedPropertyIcon` and `ThemeUtils.GetColorizedHeaderIcon` at ten sites, and `ThemeUtils` lives in `Microsoft.VisualStudio.Data.Entity.Design`. That is one of the remaining dependencies blocking the decoupling fitness function, and it is a theming dependency, so it belongs to this work.

## The shape of the fix

Promote the palette from "designer chrome" to **the vocabulary both renderers consume**, and have each surface translate it into its own medium.

```
                    DiagramTheme.Current : DiagramPalette
                     (semantic slots + brightness rules)
                          /                        \
        StyleSet overrides                          CSS custom properties
        + GDI icon colorization                     + SVG fill attributes
                  |                                          |
        VS designer (Package pushes                CLI (options push a palette,
        VS theme colors in)                        default is the modern one)
```

The palette gains the slots the SVG stylesheet currently hardcodes — icon fill, icon accent, icon blue, header compartment fill, header text, property text, association stroke, inheritance stroke — and `SvgStylesheetManager.GetStyleDefinitions()` becomes a function of the palette rather than a constant string.

**Brightness moves into the palette** as a single `ForegroundFor(Color background)` used by both surfaces, replacing the two copies. That is also where an override belongs: today the black/white decision is computed and unappealable, which is exactly the "invert icon colorings" knob that does not exist.

## Work items

1. **Unify the brightness math.** One implementation, used by `EntityTypeShape` and the SVG exporter. No behaviour change; prove it with a byte-identical render.
2. **Move icon colorization out of the VS project.** `ThemeUtils.GetThemedPropertyIcon` and `GetColorizedHeaderIcon` are GDI bitmap operations with nothing Visual Studio specific in them; they belong beside `DiagramImageHelper` in the Dsl. This also removes ten VS references from the Dsl.
3. **Widen `DiagramPalette`** to cover every color either surface draws, including the ones currently hardcoded in the SVG stylesheet.
4. **Make `SvgStylesheetManager` render from the palette.** Emit CSS custom properties so the stylesheet stays readable and a consumer can override a single slot without regenerating everything.
5. **Add the setting.** `DiagramExportOptions` gains a palette for the CLI; the package maps VS theme colors as it does now. A named "modern" palette becomes the CLI default.
6. **Decide what entity fill means.** See below — this is the open question.

Items 1 and 2 are behaviour preserving and can land first, independently.

## The open question: entity fill

`FillColor` is stored per shape in the EDMX. A "modernize the colors" setting can mean two very different things:

- **Render time override.** The file is untouched; the renderer substitutes a modern palette for whatever the file says. Non destructive, reversible, and the diagram looks different in the designer than the file describes. Good for the CLI, arguably wrong for the designer.
- **Rewrite the model.** `FillColor` attributes are updated in the EDMX. Honest — what you see is what is stored — but it is a mutation of the user's file, needs undo, and turns a display setting into a model edit.

A third option is to treat the stored `FillColor` as a *hint* that a palette maps through — for example, quantizing the legacy pastel set onto a modern equivalent — so old diagrams modernize without either lying or being rewritten.

**This needs a decision before item 5.** The rest of the plan does not depend on it.

## Verification

The Northwind SVG baseline is the regression harness for every other piece of work in this repository, and this is the one change that is *supposed* to alter it. So:

- Items 1 and 2 must keep it **byte identical**. They are refactors.
- Items 3 through 5 will change it deliberately. Capture a new baseline at the point the default palette changes, in the same commit, so the diff is reviewable rather than incidental.
- The designer needs a by-hand check against both a light and a dark Visual Studio theme, since the package path has no automated coverage.

## What this does not cover

Theme handling in the three WPF controls still in the Dsl — the diagram surface context menu, its panel, and the floating zoom control. Those subscribe to `VSColorTheme.ThemeChanged` directly and correctly, and they move to the package wholesale under work item 3 of `dsl-shell-decoupling.md`. Folding them in here would duplicate that move.
