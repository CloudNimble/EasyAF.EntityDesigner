# EF6 Tools Build Instructions

## Prerequisites
- Visual Studio 2022 or later (with VS SDK/Extensibility workload + DSLModeling SDK)
- .NET Framework 4.8 SDK
- .NET SDK 8.0 or later

## Building the Project

```bash
dotnet build EasyAF.EntityDesigner.slnx -c Release
```

## Cleaning the Project

```bash
dotnet easyaf cleanup
```

## Running Unit Tests

```bash
dotnet test EasyAF.EntityDesigner.slnx -c Release
```

Note: Use Release mode to avoid Debug.Assert triggers during testing.

## Output Locations

- Main binaries: `bin\Debug\`
- Test results: `TestResults\`

## Testing the Designer in Visual Studio
The system is running Visual Studio 2026 Insiders.

## Key Projects

- `src/Microsoft.Data.Entity.Design.EntityDesigner/` - Main designer project
- `src/Microsoft.Data.Entity.Design/` - Core design functionality
- `src/Microsoft.Data.Entity.Design.Model/` - Entity model classes
- `src/Microsoft.Data.Entity.Design.Package/` - VS Package

## Documentation References

- [VS Modeling SDK (DSL Tools)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.modeling.diagrams) - API reference for diagram shapes, connectors, and DSL designer components
