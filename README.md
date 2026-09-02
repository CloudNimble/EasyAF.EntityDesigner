# Welcome to the EasyAF Entity Designer

[**EasyAF**](https://easyaf.dev) is CloudNimble's platform for warp-speed application development with .NET, powered by EDMX.

**EasyAF.EntityDesigner** is a modern evolution of Microsoft's original EDMX experience that works with EF 6.5 and SDK-style projects targeting .NET Framework _AND_ .NET 8+.

We've stripped it down and removed all the legacy code, dependencies, and ugly WinForms UI that bogged it down.

It's now refreshed with a lighter codebase, modern UI enhanced by WPF, and a bevy of new features.

## Screenshots

### Simple, Modern User Experience
<img alt="New Windows-11 style Contextmenu + FloatingToolbar" src="https://github.com/user-attachments/assets/2d2b2acb-1e76-4448-bdf9-daf638f18f92" />

### Works with .NET 10 + SDK-style Projects + `Microsoft.Data.SqlClient`
<img alt="Opening the sample Northwind.edmx file targeting Microsoft.Data.SqlClient in a .NET 10 SDK style project" src="https://github.com/user-attachments/assets/39f13201-5118-486e-9d27-162c31a91c38" />

### Exports to Crystal-Clear SVG
<a href="https://raw.githubusercontent.com/CloudNimble/EasyAF.EntityDesigner/refs/heads/main/src/CloudNimble.EasyAF.EntityDesigner.Samples/NorthwindModel-2026-02-01.svg">
  <img alt="Exported Northwind SVG" src="https://github.com/user-attachments/assets/7cbd1ea2-ff97-4035-b15b-314ab19d00cd" />
</a>


## Feature Matrix

| Feature Supported                  | Microsoft EF6Tools | EasyAF Entity Designer |
| ---------------------------------- | ------------------ | ---------------------- |
| .NET Framework 4.8+                | ✅                 | ✅                    |
| .NET 8 / 9 / 10 / 11               | ❌                 | ✅                    |
| Entity Framework v1-5 Support      | ✅                 | ❌                    |
| `Microsoft.Data.SqlClient` Support | ❌                 | ✅                    |
| SDK-Style Projects                 | ❌                 | ✅                    |
| Intuitive User Experience          | ❌                 | ✅                    |
| High-Res Image Export              | ❌                 | ✅                    |
| SVG Export                         | ❌                 | ✅                    |
| Mermaid Diagram Export             | ❌                 | ✅                    |
| Headless Rendering (No VS Needed)  | ❌                 | ✅                    |
| `dotnet` Global Tool CLI           | ❌                 | ✅                    |
| Modern (MSAGL) Layout Engine       | ❌                 | ✅                    |

# Why Now?

The Age of AI makes it easy to keep just about anything up-to-date. We recently took the time to make some minor improvements to Microsoft's still-shipping EF6Tools codebase, and were rejected.

The thing is, EDMX is still a very powerful language for describing a database schema. We use it to power data access and code generation in both EF6 and EFCore applications across multiple cloud providers.

In fact, Entity Framework 6.5 runs **great** on modern .NET, It's a valid compatibility path to get legacy apps onto .NET 8+, and there are thousands of EDMX-based applications still being maintained.

We don't need something new here. We just need something that works.

# Getting Started with AI

Handing an AI your database schema usually means one of two bad trades: thousands of tokens of EDMX XML, most of it namespace declarations and designer coordinates, or raw T-SQL that describes tables but not the conceptual model you actually think in.

`dotnet edmx` gives you a third option. Because the designer no longer needs a Visual Studio shell, the same engine that draws your diagrams can run from a terminal, a build script, or an agent — and emit your model as **Mermaid**, which is compact, readable, and already understood by every major model.

## Installation

```bash
dotnet tool install --global EasyAF.Edmx.DiagramTools
```

The tool is Windows-only and requires the .NET 10 runtime. Update or remove it with:

```bash
dotnet tool update --global EasyAF.Edmx.DiagramTools
dotnet tool uninstall --global EasyAF.Edmx.DiagramTools
```

## Commands

### `edmx render`

Renders the diagrams in an EDMX file to SVG, a raster image, or Mermaid.

```bash
edmx render [options] <Input>
```

| Argument / Option | Description                                                                     |
| ----------------- | ------------------------------------------------------------------------------- |
| `<Input>`         | Path to the `.edmx` file to render.                                             |
| `-d\|--diagram`    | Name of the diagram to render. Defaults to the first one in the file.           |
| `-f\|--format`     | `svg`, `png`, `jpg`, `bmp`, `gif`, `tiff` or `mermaid`. Inferred from `--output` when omitted. |
| `-o\|--output`     | File to write. Defaults to the input name with the format's extension.          |
| `--show-types`    | Show property data types alongside property names.                              |
| `--transparent`   | Render with a transparent background. SVG and raster only.                      |

### Examples

Give an agent the conceptual model instead of the file:

```bash
edmx render Northwind.edmx -f mermaid -o Northwind.mmd
```

```
erDiagram
    Order ||--o{ OrderDetail : FK_Order_Details_Orders
    Shipper |o--o{ Order : FK_Orders_Shippers
    Employee |o--o{ Employee : FK_Employees_Employees
    Employee }o--o{ Territory : EmployeeTerritories
```

Entities, cardinality, relationship names, self-references and many-to-many joins — in a few dozen tokens rather than a few thousand.

Include column data types when the agent needs them:

```bash
edmx render Northwind.edmx -f mermaid --show-types
```

Produce a diagram for docs or a pull request:

```bash
edmx render Northwind.edmx -o docs/schema.svg --transparent
```

# Planned Improvements

### Features
- New "Dark Mode" rendering style
- More control over SVG outputs
- Better integration with EasyAF tooling
- Additional CLI commands beyond `render`
- Richer Mermaid output, including entity attributes

### Codebase
- Better leverage C# 14 language features
- Improve test coverage
