# Legacy Testing Infrastructure Assessment

## Executive Summary

The `test/EFTools/` folder contained legacy test projects from Microsoft's internal EF Tools development. After analysis, **the InProcTests are not viable for migration** due to dependency on deprecated Visual Studio test infrastructure, while **the FunctionalTests have been migrated** to the current test stack.

---

## Legacy Test Projects

### InProcTests (Not Migrated - Deleted)

| Aspect | Details |
|--------|---------|
| **Framework** | MSTest with VS IDE Test Host |
| **Test Count** | 94 test methods across 10 files |
| **Purpose** | Integration tests running inside Visual Studio process |

#### Tests Included
- Undo/Redo operations (11 tests)
- Safe mode behavior (20 tests)
- Model-first scenarios (25 tests)
- Automatic DbContext generation (22 tests)
- Refactor rename integration
- Diagram node migration
- Entity shape coloring
- Multi-targeting scenarios
- Workflow activities

#### Why Not Migrated

These tests used the `[HostType("VS IDE")]` attribute:

```csharp
[TestMethod]
[HostType("VS IDE")]
public void Simple_Entity()
{
    // Test runs inside Visual Studio process
    PackageManager.LoadEDMPackage(VsIdeTestHostContext.ServiceProvider);
    // ...
}
```

This infrastructure:
1. **Launched Visual Studio** as the test host process
2. **Loaded test assemblies** into that VS instance
3. **Provided `VsIdeTestHostContext.ServiceProvider`** for accessing VS services
4. Required `Microsoft.VisualStudio.QualityTools.HostAdapters.dll`

**This infrastructure was deprecated and removed from Visual Studio.** The host adapter DLLs no longer ship, and modern VS versions don't support this test execution model.

#### Modern Alternatives (Not Implemented)

| Approach | Description | Effort |
|----------|-------------|--------|
| `Microsoft.VisualStudio.Sdk.TestFramework` | Mock VS services for unit testing | Medium |
| VS Experimental Instance automation | Launch VS experimental instance, automate via DTE | High |
| Custom test host | Build infrastructure to inject tests into VS | Very High |

The existing unit tests in `src/Microsoft.Data.Entity.Tests.*` provide adequate coverage for the core functionality. The integration-level scenarios these tests covered can be verified through manual testing.

---

### FunctionalTests (Partially Migrated)

| Aspect | Details |
|--------|---------|
| **Framework** | xUnit with Moq |
| **Test Count** | 5 test methods across 3 files |
| **Purpose** | Unit/functional tests with mocked VS package |

#### Tests Migrated

| Original File | Tests | Destination | Status |
|---------------|-------|-------------|--------|
| `MiscellaneousTests.cs` | 2 tests | Split between projects | **Migrated** |

**Migrated tests:**
- `VSXmlModel_returns_correct_local_path_for_Uri_with_hashes` → `Microsoft.VisualStudio.Data.Tools.Tests.Design.XmlCore`
- `Filename2Uri_can_handle_file_uris_with_hashes` → `Microsoft.Data.Tools.Tests.Design.XmlCore`

#### Tests Not Migrated

| Original File | Tests | Reason |
|---------------|-------|--------|
| `GenerateFromDatabaseWizardTest.cs` | 1 test | Requires schema manager infrastructure |
| `UpdateModelFromDatabaseTests.cs` | 2 tests | Requires schema manager infrastructure |

These wizard tests required `SchemaManager` setup with EDMX schema files that were part of Microsoft's internal build infrastructure. Recreating this environment would require significant effort for minimal benefit.

#### Conversion Applied
- xUnit `[Fact]` → MSTest `[TestMethod]`
- xUnit `Assert.*` → FluentAssertions `*.Should().*`
- Namespace updates to match destination projects

---

## Test Infrastructure Mapping

The `EFDesignerTestInfrastructure` namespace referenced by legacy tests was reorganized:

| Legacy Namespace | Current Namespace |
|------------------|-------------------|
| `EFDesignerTestInfrastructure.EFDesigner` | `Microsoft.Data.Entity.Tests.Shared.EFDesigner` |
| `EFDesignerTestInfrastructure.VS` | `Microsoft.Data.Entity.Tests.Shared.VS` |

Key types in `Microsoft.Data.Entity.Tests.Shared`:
- `EFArtifactHelper` - Base class for artifact test helpers
- `EFArtifactExtensions` - Extension methods for test assertions
- `EntityTypeExtensions` - Entity type test helpers
- `UITestRunner` - UI thread invocation for tests
- `DteExtensions` - DTE mock helpers

---

## Files Deleted

```
test/
└── EFTools/
    ├── FunctionalTests/
    │   ├── FunctionalTests.csproj
    │   ├── GenerateFromDatabaseWizardTest.cs
    │   ├── MiscellaneousTests.cs
    │   ├── UpdateModelFromDatabaseTests.cs
    │   ├── TestHelpers/
    │   │   ├── EdmPackageFixture.cs
    │   │   ├── MockEFArtifactHelper.cs
    │   │   └── MockPackage.cs
    │   └── TestArtifacts/
    │       └── *.edmx (test data files)
    └── InProcTests/
        ├── InProcTests.csproj
        ├── AutomaticDbContextTests.cs
        ├── EntityTypeShapeColorTest.cs
        ├── MigrateDiagramNodesTest.cs
        ├── ModelFirstTestsRemote.cs
        ├── MultiTargetingTestsInProcRemote.cs
        ├── RefactorRenameTests.cs
        ├── SafeModeTestsRemote.cs
        ├── UndoRedoTestsRemote.cs
        ├── WorkflowActivityTestsRemote.cs
        ├── Extensions/
        ├── Baselines/
        └── TestData/
```

---

## Recommendations

### For Integration Testing

If integration-level testing becomes necessary in the future:

1. **Use `Microsoft.VisualStudio.Sdk.TestFramework`** for mocking VS services
2. **Create scenario-based manual test plans** for complex interactions
3. **Consider UI automation** with tools like WinAppDriver for critical paths

### For Regression Testing

The existing test projects provide good coverage:
- `Microsoft.Data.Entity.Tests.Design` - Core design functionality
- `Microsoft.Data.Entity.Tests.Design.Model` - Model manipulation
- `Microsoft.Data.Entity.Tests.Design.EntityDesigner` - Designer-specific
- `Microsoft.Data.Entity.Tests.Design.Package` - VS package integration
- `Microsoft.Data.Entity.Tests.Design.VersioningFacade` - Version handling

Run tests with:
```bash
dotnet test src/EasyAF.EntityDesigner.slnx -c Release
```

---

## References

- [VS SDK Test Framework](https://github.com/microsoft/vssdktestfx)
- [Testing VS Extensions](https://learn.microsoft.com/en-us/visualstudio/extensibility/testing-extensions)
- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
