# Microsoft's Entity Framework Designer is dead.

Yet it still ships inside Visual Studio 2026, broken and out of date. Unable to work with moderm projects or SQL providers. And [not accepting new PRs](https://github.com/dotnet/ef6tools/issues/83).

So the EDMXperts at CloudNimble have taken matters into our own hands.

# Welcome to the EasyAF Entity Designer

[**EasyAF**](https://easyaf.dev) is CloudNimble's platform for warp-speed application development with .NET, powered by EDMX.

**EasyAF.EntityDesigner** is a stripped-down modern replacement that works with EF 6.5 and SDK-style projects targeting .NET Framework _AND_ .NET 8+.

We've removed all the legacy code, dependencies, and ugly WinForms UI that bogged it down.

It's now refreshed with a lighter codebase, modern UI enhanced by WPF, and a bevy of new features.

| Feature Supported            | Microsoft In-Box Designer  | EasyAF.EntityDesigner |
| ---------------------------- | -------------------------- | --------------------- |
| .NET Framework 4.8           | ✅                         | ✅                   |
| .NET 8 / 9 / 10 / 11         | ❌                         | ✅                   |
| `Microsoft.Data.SqlProvider` | ❌                         | ✅                   |
| SDK-Style Projects           | ❌                         | ✅                   |
| Intuitive User Experience    | ❌                         | ✅                   |
| High-Res Image Export        | ❌                         | ✅                   |
| SVG Export                   | ❌                         | ✅                   |
| Mermaid Diagram Export       | ❌                         | ✅                   |

## Screenshots



# Why Now?

The Age of AI makes it easy to keep just about anything up-to-date. We recently took the time to make some minor improvements to the codebase with Claude Code, and were rejected.

But Entity Framework 6.5 runs on modern .NET, and there are thousands of EDMX-based applications still being maintained. But Microsoft's corporate calculus is leaving developers high and dry.

Even if you're running on Entity Framework Core, having a visual designer to help you understand complex database structures is invaluable.