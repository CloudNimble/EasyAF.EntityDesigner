# Plan: Remove Legacy VS/EF Version Support

## Constraints
- **KEEP**: .NET Framework 4.8 support
- **KEEP**: EF6 and later support
- **KEEP**: VS2022 and later support
- **KEEP**: Any EF6-based provider support
- **REMOVE**: Everything for pre-EF6, pre-VS2022, pre-.NET 4.5

---

## Phase 1: Delete LegacyProviderWrapper Infrastructure

### 1.1 Delete Entire Folder
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/LegacyProviderWrapper/`

Files to delete (8 files, ~1,500 LOC):
- `LegacyDbProviderServicesWrapper.cs`
- `LegacyDbProviderManifestWrapper.cs`
- `LegacyDbCommandDefinitionWrapper.cs`
- `LegacyDbExpressionConverter.cs`
- `TypeUsageHelper.cs`
- `LegacyMetadataExtensions/MetadataWorkspaceExtensions.cs`
- `LegacyMetadataExtensions/StoreItemCollectionExtensions.cs`
- `LegacyMetadataExtensions/TypeUsageExtensions.cs`

### 1.2 Delete LegacyDbProviderServicesResolver
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/LegacyDbProviderServicesResolver.cs`

**Downstream Impact**:
- `DbProviderServicesResolver.cs` - Remove fallback to legacy resolver (line 88)
- `DependencyResolver.cs` - Remove legacy resolver initialization

### 1.3 Update DbProviderServicesResolver.cs
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/DbProviderServicesResolver.cs`

Changes:
- Remove lines 16-17: `LegacyDbProviderServicesResolver` field
- Remove line 88: Fallback to legacy resolver (return `null` instead)
- Remove `using` for LegacyProviderWrapper namespace

### 1.4 Delete Associated Test Files
**Path**: `src/Microsoft.Data.Entity.Tests.Design.VersioningFacade/`

Files to delete:
- `LegacyDbProviderServicesResolverTests.cs`
- `LegacyProviderWrapper/LegacyDbCommandDefintionWrapperTests.cs`
- `LegacyProviderWrapper/LegacyDbExpressionConverterTests.cs`
- `LegacyProviderWrapper/LegacyDbProviderManifestWrapperTests.cs`
- `LegacyProviderWrapper/LegacyDbProviderServicesWrapperTests.cs`
- `LegacyProviderWrapper/TypeUsageVerificationHelper.cs`
- `LegacyProviderWrapper/LegacyMetadataExtensions/MetadataWorkspaceExtensionsTests.cs`
- `LegacyProviderWrapper/LegacyMetadataExtensions/StoreItemCollectionExtensionsTests.cs`
- `LegacyProviderWrapper/LegacyMetadataExtensions/TypeUsageExtensionsTests.cs`

---

## Phase 2: Simplify LegacyCodegen

### 2.1 Delete EntityClassGenerator.cs
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/LegacyCodegen/EntityClassGenerator.cs`

This wraps EF1's `System.Data.Entity.Design.EntityClassGenerator` - not needed for EF6.

### 2.2 Update CodeGeneratorBase.cs
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/LegacyCodegen/CodeGeneratorBase.cs`

Current (lines 46-55):
```csharp
return targetEntityFrameworkVersion == EntityFrameworkVersion.Version1
    ? (CodeGeneratorBase)new EntityClassGenerator(language)
    : new EntityCodeGenerator(language, targetEntityFrameworkVersion);
```

Change to:
```csharp
return new EntityCodeGenerator(language, EntityFrameworkVersion.Version3);
```

### 2.3 Delete Associated Test File
**Path**: `src/Microsoft.Data.Entity.Tests.Design.VersioningFacade/LegacyCodegen/CodeGeneratorBaseTests.cs`

Update tests to only test Version3 scenarios.

---

## Phase 3: Clean Up Version Constants

### 3.1 Update EntityFrameworkVersion.cs
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/EntityFrameworkVersion.cs`

Remove:
- Line 13: `Version1 = new Version(1, 0, 0, 0)` - DELETE
- Line 14: `Version2 = new Version(2, 0, 0, 0)` - DELETE
- Lines 17-22: Update `GetAllVersions()` to only yield `Version3`

Keep:
- Line 15: `Version3 = new Version(3, 0, 0, 0)` - KEEP (EF6 uses this)
- Line 42-45: `Latest` property - KEEP

### 3.2 Update CsdlVersion.cs
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/Metadata/CsdlVersion.cs`

Remove:
- `Version1 = new Version(1, 0, 0, 0)` - DELETE
- `Version1_1 = new Version(1, 1, 0, 0)` - DELETE
- `Version2 = new Version(2, 0, 0, 0)` - DELETE

Keep:
- `Version3 = new Version(3, 0, 0, 0)` - KEEP

### 3.3 Update RuntimeVersion.cs
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/RuntimeVersion.cs`

Remove:
- Line 30: `Version1 = new Version(3, 5, 0, 0)` - DELETE
- Line 31: `Version4 = new Version(4, 0, 0, 0)` - DELETE
- Line 32: `Version5Net40 = new Version(4, 4, 0, 0)` - DELETE

Keep:
- Line 33: `Version5Net45 = new Version(5, 0, 0, 0)` - KEEP (for upgrade detection)
- Line 34: `Version6 = new Version(6, 0, 0, 0)` - KEEP

Simplify methods:
- `Version5()` - Always return `Version5Net45`
- `GetName()` - Remove Version4/Version5Net40 branches
- `RequiresLegacyProvider()` - Always return `false` (or delete entirely)
- `GetTargetSchemaVersion()` - Always return `Version3`
- `GetLatestSchemaVersion()` - Always return `Version3`
- `IsSchemaVersionLatestForAssemblyVersion()` - Simplify to only Version3 check
- `GetSchemaVersionForNetFrameworkVersion()` - Always return `Version3`

---

## Phase 4: Clean Up .NET Framework Version Handling

### 4.1 Update NetFrameworkVersioningHelper.cs
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/NetFrameworkVersioningHelper.cs`

Remove:
- `NetFrameworkVersion3_5 = new Version(3, 5)` - DELETE
- `NetFrameworkVersion4 = new Version(4, 0)` - DELETE (or keep for comparison only)

Keep:
- `NetFrameworkVersion4_5 = new Version(4, 5)` - KEEP (minimum for EF6)
- Modern .NET detection logic - KEEP

Simplify:
- `IsSupportedDotNetProject()` - Remove .NET 3.5 check (line 92)

---

## Phase 5: Remove VS12ORNEWER Preprocessor Directives

### 5.1 Files to Update (5 files)

All these files have `#if VS12ORNEWER` blocks. Since VS2022 > VS12, keep the code inside `#if` blocks and remove the conditionals:

1. **EntityDesignExplorerFrame.cs**
   - Path: `src/Microsoft.Data.Entity.Design/UI/Views/Explorer/EntityDesignExplorerFrame.cs`
   - Action: Remove `#if VS12ORNEWER` / `#endif`, keep the code inside

2. **DatabaseObjectTreeView.cs**
   - Path: `src/Microsoft.Data.Entity.Design/VisualStudio/ModelWizard/gui/DatabaseObjectTreeView.cs`
   - Action: Remove conditionals, keep VS12+ code

3. **EntityTypeShape.cs**
   - Path: `src/Microsoft.Data.Entity.Design.EntityDesigner/CustomCode/Shapes/EntityTypeShape.cs`
   - Action: Remove conditionals, keep VS12+ code

4. **MicrosoftDataEntityDesignPackage.cs**
   - Path: `src/Microsoft.Data.Entity.Design.Package/CustomCode/MicrosoftDataEntityDesignPackage.cs`
   - Action: Remove conditionals, keep VS12+ code

5. **VSXmlModelProvider.cs**
   - Path: `src/Microsoft.VisualStudio.Data.Tools.Design.XmlCore/Model/VisualStudio/VSXmlModelProvider.cs`
   - Action: Remove conditionals, keep VS12+ code

### 5.2 Update Directory.Build.props
**Path**: `src/Directory.Build.props`

Remove from DefineConstants (line 64):
- `VS12ORNEWER` - No longer needed

---

## Phase 6: Update Downstream Code

### 6.1 VsUtils.cs
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/VsUtils.cs`

- `EnsureProvider()` method (lines 2213-2230): Remove `useLegacyProvider` conditional, always use modern path
- Remove any .NET 3.5 specific logic

### 6.2 WizardPageRuntimeConfig.cs
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/ModelWizard/gui/WizardPageRuntimeConfig.cs`

- Lines 80-114: Remove `RequiresLegacyProvider` checks
- Remove `UseLegacyProvider` flag setting

### 6.3 ModelBuilderSettings.cs
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/ModelWizard/Engine/ModelBuilderSettings.cs`

- Line 267: Remove or deprecate `UseLegacyProvider` property

### 6.4 OptionsDesignerInfo.cs
**Path**: `src/Microsoft.Data.Entity.Design.Model/OptionsDesignerInfo.cs`

- Lines 37-38: Update `UseLegacyProviderDefault = true` to `false` or remove entirely
- Lines 138-146: Simplify logic

### 6.5 Database Engine Files
Update these files to remove legacy provider handling:
- `DatabaseEngineBase.cs` (lines 100, 158)
- `EdmxModelBuilderEngine.cs` (line 67)
- `DatabaseGenerationEngine.cs` (lines 172, 222-229)
- `UpdateFromDatabaseEngine.cs` (lines 111, 235-237)

### 6.6 RuntimeConfigViewModel.cs
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/ModelWizard/gui/ViewModels/RuntimeConfigViewModel.cs`

- Lines 26-35: Remove .NET 3.5 special case
- Lines 40-62: Remove pre-EF6 version handling, only offer EF6

### 6.7 DbContextCodeGenerator.cs
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/ModelWizard/DbContextCodeGenerator.cs`

- `TemplateSupported()` method: Always return true (no .NET 3.5 check)

---

## Phase 7: Clean Up SchemaManager

### 7.1 Update SchemaManager.cs
**Path**: `src/Microsoft.Data.Entity.Design.VersioningFacade/SchemaManager.cs`

The namespace dictionaries (CSDL, EDMX, SSDL, MSL) contain entries for all 3 versions. Options:
1. **Conservative**: Keep all entries for reading old EDMX files, but only write Version3
2. **Aggressive**: Remove Version1/Version2 entries (breaks reading old files)

**Recommendation**: Conservative approach - keep read support, simplify write paths.

---

## Phase 8: Clean Up MetadataConverter

### 8.1 MetadataConverterDriver.cs
**Path**: `src/Microsoft.Data.Entity.Design.Model/MetadataConverter/MetadataConverterDriver.cs`

Keep for upgrading old EDMX files to Version3, but:
- Remove downgrade paths (v3→v2, v2→v1)
- Simplify to only support upgrade to Version3

### 8.2 VersionConverterHandler.cs
**Path**: `src/Microsoft.Data.Entity.Design.Model/MetadataConverter/VersionConverterHandler.cs`

Simplify to only handle upgrade to Version3.

---

## Phase 9: Clean Up EdmFeatureManager

### 9.1 Update EdmFeatureManager.cs
**Path**: `src/Microsoft.Data.Entity.Design.Model/EdmFeatureManager.cs`

All version-gated features are enabled for EF6. Simplify all methods to return `FeatureState.VisibleAndEnabled` unconditionally.

Features that were version-gated (now always enabled):
- FunctionImport
- FunctionImport Complex Return
- FunctionImport Mapping
- ForeignKeysInModel
- GenerateUpdateViews
- EntityContainerTypeAccess
- LazyLoadingEnabled
- ComposableFunction
- UseStrongSpatialTypes
- EnumTypes

---

## Phase 10: Delete EF5 Templates (Optional - Large Cleanup)

### 10.1 Template Directories to Delete
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/InTheBoxItemTemplates/DBContext/`

Delete all EF5-specific template folders (~54 directories across locales):
- `EF5/` folder and all localized variants
- `*/DbCtxCSEF5/` folders
- `*/DbCtxVBEF5/` folders
- `*/DbCtxCSWSEF5/` folders
- `*/DbCtxVBWSEF5/` folders

### 10.2 Update Template Manifest
**Path**: `src/Microsoft.Data.Entity.Design/VisualStudio/InTheBoxItemTemplates/EntityFrameworkTools.1033.item.vstman`

Remove EF5 template entries from the manifest.

---

## Phase 11: Update Tests

### 11.1 Test Files to Delete
- All tests for Version1/Version2 scenarios
- All tests for .NET 3.5/4.0 scenarios
- All tests for legacy provider scenarios

### 11.2 Test Files to Update
Simplify version-specific tests to only test Version3/EF6:
- `EntityFrameworkVersionTests.cs`
- `CsdlVersionTests.cs`
- `RuntimeVersionTests.cs`
- `NetFrameworkVersioningHelperTests.cs`
- `RuntimeConfigViewModelTests.cs`

---

## Phase 12: Update Resource Strings

### 12.1 Resources.resx Files
Remove or update resource strings referencing:
- "EF 3.5", "EF 4.0", "EF 5.0"
- ".NET Framework 3.5"
- Legacy provider messages

---

## Summary Table

| Phase | Category | Files Deleted | Files Modified | Est. LOC Removed |
|-------|----------|---------------|----------------|------------------|
| 1 | LegacyProviderWrapper | 9 | 2 | ~1,600 |
| 2 | LegacyCodegen | 1 | 1 | ~200 |
| 3 | Version Constants | 0 | 3 | ~100 |
| 4 | .NET Framework | 0 | 1 | ~30 |
| 5 | VS12ORNEWER | 0 | 6 | ~50 |
| 6 | Downstream Code | 0 | 10 | ~300 |
| 7 | SchemaManager | 0 | 1 | ~20 |
| 8 | MetadataConverter | 0 | 2 | ~100 |
| 9 | EdmFeatureManager | 0 | 1 | ~150 |
| 10 | EF5 Templates | ~54 dirs | 1 | ~2,000 |
| 11 | Tests | ~15 | ~10 | ~1,500 |
| 12 | Resources | 0 | 2 | ~20 |
| **TOTAL** | | **~80 files** | **~40 files** | **~6,000+ LOC** |

---

## Execution Order

1. **Phase 1-2**: Delete legacy provider and codegen (isolated, minimal downstream impact)
2. **Phase 3-4**: Update version constants (ripples through codebase)
3. **Phase 5**: Remove preprocessor directives (cosmetic cleanup)
4. **Phase 6**: Update downstream code (depends on Phase 3-4)
5. **Phase 7-9**: Clean up managers and converters
6. **Phase 10**: Delete templates (independent, can be done anytime)
7. **Phase 11-12**: Update tests and resources (final cleanup)

---

## Verification

After each phase:
```bash
# Build solution
dotnet build ModernEntityDesigner.slnx -c Debug

# Run tests
dotnet test ModernEntityDesigner.slnx
```

Final verification:
1. Create new EF6 EDMX project in VS2022
2. Reverse engineer a database
3. Generate code from EDMX
4. Verify all features work without legacy paths
