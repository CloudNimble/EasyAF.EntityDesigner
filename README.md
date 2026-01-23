# Microsoft's Entity Framework Designer is dead.

Yet it still ships inside Visual Studio 2026, broken and out of date. Unable to work with moderm projects or SQL providers.

So we've taken matters into our own hands.

# Modern Entity Designer 2026

Modern Entity Designer 2026 is a stripped down, refactored, modernized version of Microsoft EDMX Designer.

We've updated the code for modern .NET, stripped out the unnecessary crap, simplified the build process, fixed a bunch of bugs, and added a ton of new features.

| Feature Supported            | Microsoft In-Box Designer | Modern Entity Designer 2026 |
| ---------------------------- | ------------------------- | --------------------------- |
| .NET Framework 4.8           | ✅                         | ✅                           |
| .NET 8 / 9 / 10 / 11         | ❌                         | ✅                           |
| `Microsoft.Data.SqlProvider` | ❌                         | ✅                           |
| SDK-Style Projects           | ❌                         | ✅                           |
| SVG Export                   | ❌                         | ✅                           |
| Mermaid Diagram Export       | ❌                         | ✅                           |

# Why Now?

There are situations where the current designer won't even open your EDMX file, and [Microsoft is not accepting new PRs](https://github.com/dotnet/ef6tools/issues/83).

At CloudNimble, we are EDMXperts. We have been leveraging the format in EF 6, OData, and EF Core to power robust code-generated .NET solutions for almost 15 years.

Simply put, we wanted to be able to open EDMX files in our own projects and have the Designer work properly.

## Use Cases

Modern Entity Designer 2026 supports a wide-range of features to help you maintain EDMX-based solutions, including [EasyAF](https://easyaf.dev) for database-first development with Entity Framework Core.
