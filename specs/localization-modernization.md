# Localization Modernization Plan

## Executive Summary

This document outlines the plan to convert EasyAF Entity Designer's localization from Microsoft's proprietary LCL/LCX format to the industry-standard XLIFF format using `Microsoft.DotNet.XliffTasks`. This modernization preserves approximately 16,500 existing translations across 10 languages while enabling a sustainable, open-source-friendly localization workflow.

---

## Current State Analysis

### Existing Localization Assets

#### Location: `/loc/`

```
loc/
├── lci/                    # Localization Comment Input (English source + comments)
│   └── *.dll.lci          # 11 files
├── lcl/                    # Localization Content (translations per language)
│   ├── CHS/               # Chinese Simplified (zh-Hans)
│   ├── CHT/               # Chinese Traditional (zh-Hant)
│   ├── DEU/               # German (de)
│   ├── ESN/               # Spanish (es)
│   ├── FRA/               # French (fr)
│   ├── ITA/               # Italian (it)
│   ├── JPN/               # Japanese (ja)
│   ├── KOR/               # Korean (ko)
│   ├── PTB/               # Portuguese-Brazilian (pt-BR)
│   └── RUS/               # Russian (ru)
└── intellisense/          # Localized XML documentation
    └── [same languages]/
```

#### Translation Statistics (per language)

| Assembly | String Count |
|----------|-------------|
| Microsoft.Data.Entity.Design.dll | 780 |
| Microsoft.Data.Entity.Design.EntityDesigner.dll | 236 |
| Microsoft.Data.Tools.Design.XmlCore.dll | 218 |
| Microsoft.Data.Entity.Design.Package.dll | 154 |
| Microsoft.Data.Entity.Design.Model.dll | 112 |
| Microsoft.VisualStudio.Data.Tools.Design.XmlCore.dll | 62 |
| Microsoft.Data.Entity.Design.VersioningFacade.dll | 35 |
| Microsoft.Data.Entity.Design.DatabaseGeneration.dll | 32 |
| Microsoft.Data.Entity.Design.DataSourceWizardExtension.dll | 11 |
| Microsoft.Data.Entity.Design.BootstrapPackage.dll | 5 |
| Microsoft.Data.Entity.Design.Extensibility.dll | 4 |
| **Total per language** | **~1,649** |
| **Total all languages (×10)** | **~16,490** |

### LCL File Format

The LCL format is XML-based with the following structure:

```xml
<LCX SchemaVersion="6.0" Name="..." SrcCul="en-US" TgtCul="de-DE">
  <Item ItemId=";ResourceName" ItemType="0" PsrId="211" Leaf="true">
    <Str Cat="Text">
      <Val><![CDATA[English source text]]></Val>
      <Tgt Cat="Text" Stat="Loc" Orig="Auto">
        <Val><![CDATA[Translated text]]></Val>
      </Tgt>
    </Str>
  </Item>
</LCX>
```

Key elements:
- `ItemId` → Resource string name (prefixed with `;`)
- `Val` → English source text
- `Tgt/Val` → Translated text
- `Stat="Loc"` → Translation status (Loc = Localized)

### Current RESX Files

The source projects contain English `.resx` files:

| Project | Primary RESX |
|---------|-------------|
| Microsoft.Data.Entity.Design | Resources.resx |
| Microsoft.Data.Entity.Design.EntityDesigner | Properties/Resources.resx |
| Microsoft.Data.Entity.Design.Model | Resources.resx |
| Microsoft.Data.Entity.Design.Package | Resources.resx, VSPackage.resx |
| Microsoft.Data.Entity.Design.DatabaseGeneration | Properties/Resources.resx |
| Microsoft.Data.Entity.Design.Extensibility | Properties/Resources.resx |
| Microsoft.Data.Entity.Design.VersioningFacade | Resources.VersioningFacade.resx |
| Microsoft.Data.Tools.Design.XmlCore | Resources.resx |
| Microsoft.VisualStudio.Data.Tools.Design.XmlCore | Resources.resx |

---

## Target Architecture

### XLIFF-Based Workflow

```
Source (.resx)           XLIFF Files              Satellite Assemblies
    │                        │                           │
    │    ┌───────────────────┼───────────────────┐       │
    │    │                   │                   │       │
    ▼    ▼                   ▼                   ▼       ▼
Resources.resx ──► Resources.de.xlf ──► de/Project.resources.dll
                   Resources.fr.xlf ──► fr/Project.resources.dll
                   Resources.ja.xlf ──► ja/Project.resources.dll
                   ...
```

### Microsoft.DotNet.XliffTasks

This official Microsoft package provides:

1. **UpdateXlf target** - Syncs XLIFF files with source RESX changes
2. **Build integration** - Automatically builds satellite assemblies from XLIFF
3. **Translation state tracking** - Marks strings as new/needs-review/translated

### XLIFF File Format (1.2)

```xml
<xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
  <file datatype="xml" source-language="en" target-language="de" original="Resources.resx">
    <body>
      <trans-unit id="ResourceName">
        <source>English source text</source>
        <target state="translated">German translation</target>
        <note from="developer">Developer comment</note>
      </trans-unit>
    </body>
  </file>
</xliff>
```

---

## Implementation Plan

### Phase 1: Setup XliffTasks Infrastructure

#### 1.1 Add NuGet Package Reference

Add to `src/Directory.Build.props`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.DotNet.XliffTasks" Version="9.0.0-beta.*" PrivateAssets="All" />
</ItemGroup>

<PropertyGroup>
  <!-- Enable XLIFF generation -->
  <UpdateXlfOnBuild>true</UpdateXlfOnBuild>

  <!-- Supported languages -->
  <XlfLanguages>de;es;fr;it;ja;ko;pt-BR;ru;zh-Hans;zh-Hant</XlfLanguages>
</PropertyGroup>
```

#### 1.2 Add Package Source Mapping

Add to `src/NuGet.config`:

```xml
<packageSource key="dotnet-public">
  <package pattern="microsoft.dotnet.xlifftasks" />
</packageSource>
```

#### 1.3 Configure Per-Project (if needed)

Projects can override the default behavior:

```xml
<PropertyGroup>
  <!-- Disable for test projects -->
  <UpdateXlfOnBuild Condition="'$(IsTestProject)' == 'true'">false</UpdateXlfOnBuild>
</PropertyGroup>
```

### Phase 2: Create LCL-to-XLIFF Conversion Tool

#### 2.1 Tool Requirements

Create `tools/ConvertLclToXliff/` with:

- PowerShell script for one-time conversion
- Mapping of LCL language codes to .NET culture codes
- Validation of string ID consistency

#### 2.2 Language Code Mapping

| LCL Code | .NET Culture | Language |
|----------|--------------|----------|
| CHS | zh-Hans | Chinese Simplified |
| CHT | zh-Hant | Chinese Traditional |
| DEU | de | German |
| ESN | es | Spanish |
| FRA | fr | French |
| ITA | it | Italian |
| JPN | ja | Japanese |
| KOR | ko | Korean |
| PTB | pt-BR | Portuguese (Brazil) |
| RUS | ru | Russian |

#### 2.3 Conversion Script Outline

```powershell
# ConvertLclToXliff.ps1

param(
    [string]$LclRoot = "loc/lcl",
    [string]$LciRoot = "loc/lci",
    [string]$SourceRoot = "src",
    [string]$OutputRoot = "src"
)

# Language mapping
$languageMap = @{
    "CHS" = "zh-Hans"
    "CHT" = "zh-Hant"
    "DEU" = "de"
    "ESN" = "es"
    "FRA" = "fr"
    "ITA" = "it"
    "JPN" = "ja"
    "KOR" = "ko"
    "PTB" = "pt-BR"
    "RUS" = "ru"
}

# DLL to project/resx mapping
$dllToResxMap = @{
    "microsoft.data.entity.design.dll" = @{
        Project = "Microsoft.Data.Entity.Design"
        Resx = "Resources.resx"
    }
    # ... additional mappings
}

foreach ($lang in $languageMap.Keys) {
    $culture = $languageMap[$lang]
    $lclPath = Join-Path $LclRoot $lang

    foreach ($lclFile in Get-ChildItem $lclPath -Filter "*.lcl") {
        # Parse LCL XML
        [xml]$lcl = Get-Content $lclFile.FullName

        # Extract translations
        $translations = @{}
        foreach ($item in $lcl.SelectNodes("//Item[@Leaf='true']")) {
            $id = $item.ItemId -replace "^;", ""
            $target = $item.SelectSingleNode(".//Tgt/Val")
            if ($target) {
                $translations[$id] = $target.InnerText
            }
        }

        # Generate XLIFF
        $xliff = New-XliffDocument -SourceLanguage "en" -TargetLanguage $culture
        foreach ($id in $translations.Keys) {
            Add-TransUnit -Document $xliff -Id $id -Target $translations[$id]
        }

        # Save to project folder
        $outputPath = Get-OutputPath $lclFile.Name $culture
        $xliff.Save($outputPath)
    }
}
```

#### 2.4 DLL-to-Project Mapping

| LCL File | Project | RESX File |
|----------|---------|-----------|
| microsoft.data.entity.design.dll.lcl | Microsoft.Data.Entity.Design | Resources.resx |
| microsoft.data.entity.design.entitydesigner.dll.lcl | Microsoft.Data.Entity.Design.EntityDesigner | Properties/Resources.resx |
| microsoft.data.entity.design.model.dll.lcl | Microsoft.Data.Entity.Design.Model | Resources.resx |
| microsoft.data.entity.design.package.dll.lcl | Microsoft.Data.Entity.Design.Package | Resources.resx |
| microsoft.data.entity.design.databasegeneration.dll.lcl | Microsoft.Data.Entity.Design.DatabaseGeneration | Properties/Resources.resx |
| microsoft.data.entity.design.extensibility.dll.lcl | Microsoft.Data.Entity.Design.Extensibility | Properties/Resources.resx |
| microsoft.data.entity.design.versioningfacade.dll.lcl | Microsoft.Data.Entity.Design.VersioningFacade | Resources.VersioningFacade.resx |
| microsoft.data.tools.design.xmlcore.dll.lcl | Microsoft.Data.Tools.Design.XmlCore | Resources.resx |
| microsoft.visualstudio.data.tools.design.xmlcore.dll.lcl | Microsoft.VisualStudio.Data.Tools.Design.XmlCore | Resources.resx |

### Phase 3: Execute Conversion

#### 3.1 Pre-Conversion Validation

1. Verify all RESX files have matching LCL files
2. Check for string ID mismatches between RESX and LCL
3. Generate report of missing translations

#### 3.2 Run Conversion

```bash
# From repository root
pwsh tools/ConvertLclToXliff/ConvertLclToXliff.ps1

# Verify output
find src -name "*.xlf" | head -20
```

#### 3.3 Expected Output Structure

```
src/
├── Microsoft.Data.Entity.Design/
│   ├── Resources.resx
│   └── xlf/
│       ├── Resources.de.xlf
│       ├── Resources.es.xlf
│       ├── Resources.fr.xlf
│       ├── Resources.it.xlf
│       ├── Resources.ja.xlf
│       ├── Resources.ko.xlf
│       ├── Resources.pt-BR.xlf
│       ├── Resources.ru.xlf
│       ├── Resources.zh-Hans.xlf
│       └── Resources.zh-Hant.xlf
└── [other projects with same structure]
```

### Phase 4: Build Integration

#### 4.1 Verify Satellite Assembly Generation

```bash
dotnet build src/EasyAF.EntityDesigner.slnx -c Release

# Check for satellite assemblies
find src -path "*/bin/Release/*/de/*.resources.dll" | head -5
```

Expected output:
```
src/Microsoft.Data.Entity.Design/bin/Release/de/Microsoft.Data.Entity.Design.resources.dll
src/Microsoft.Data.Entity.Design/bin/Release/fr/Microsoft.Data.Entity.Design.resources.dll
...
```

#### 4.2 VSIX Packaging

Update `source.extension.vsixmanifest` to include satellite assemblies:

```xml
<Assets>
  <!-- Existing assets -->
  <Asset Type="Microsoft.VisualStudio.VsPackage" ... />

  <!-- Localized resources (auto-included by SDK) -->
</Assets>
```

The VS SDK should automatically include satellite assemblies in the VSIX.

### Phase 5: Cleanup

#### 5.1 Archive Legacy Files

```bash
# Create archive of legacy localization
mkdir -p archive
mv loc archive/loc-legacy

# Keep intellisense XMLs if needed for XML documentation
# Or archive them too if not distributing localized IntelliSense
```

#### 5.2 Update .gitignore

```gitignore
# Generated XLIFF files are source-controlled
# But generated satellite assemblies are not
**/bin/*/*.resources.dll
```

#### 5.3 Documentation Updates

Update CLAUDE.md and README.md with:
- How to add new strings (add to RESX, build to generate XLIFF)
- How to update translations (edit XLIFF files)
- How to add a new language

---

## Ongoing Workflow

### Adding New Strings

1. Add string to the appropriate `.resx` file
2. Build the project (or run `dotnet build -t:UpdateXlf`)
3. New strings appear in all `.xlf` files with `state="new"`
4. Send XLIFF files for translation

### Updating Translations

1. Edit the `.xlf` file directly, or
2. Use a translation tool that supports XLIFF (Crowdin, Lokalise, POEditor, etc.)
3. Set `state="translated"` when complete
4. Build to generate updated satellite assemblies

### Adding a New Language

1. Add culture code to `XlfLanguages` in Directory.Build.props:
   ```xml
   <XlfLanguages>de;es;fr;it;ja;ko;pt-BR;ru;zh-Hans;zh-Hant;pl</XlfLanguages>
   ```
2. Build to generate new `.xlf` files
3. Translate the new files

---

## Validation Checklist

### Conversion Validation

- [ ] All LCL files have corresponding XLIFF output
- [ ] String counts match between LCL and XLIFF per language
- [ ] No duplicate string IDs in XLIFF files
- [ ] XLIFF files pass schema validation

### Build Validation

- [ ] Solution builds without localization errors
- [ ] Satellite assemblies generated for all languages
- [ ] VSIX includes satellite assemblies
- [ ] Extension loads correctly in VS with non-English locale

### Runtime Validation

- [ ] UI strings display correctly in German Windows
- [ ] Dialog boxes show translated text
- [ ] Error messages are localized
- [ ] No missing resource exceptions in Output window

---

## Risk Mitigation

### String ID Mismatches

**Risk:** LCL files may have string IDs that don't match current RESX files.

**Mitigation:**
- Generate mapping report before conversion
- Log orphaned translations for manual review
- Keep archive of LCL files for reference

### Incomplete Translations

**Risk:** Some strings may be untranslated or machine-translated.

**Mitigation:**
- Set `state="needs-review-translation"` for uncertain translations
- Prioritize UI-visible strings for quality review

### Build Performance

**Risk:** XLIFF processing may slow builds.

**Mitigation:**
- Set `UpdateXlfOnBuild` to false for CI builds
- Run `UpdateXlf` target only when RESX files change

---

## Timeline Estimate

| Phase | Tasks | Effort |
|-------|-------|--------|
| Phase 1 | XliffTasks setup | 1-2 hours |
| Phase 2 | Conversion tool development | 4-8 hours |
| Phase 3 | Execute conversion + validation | 2-4 hours |
| Phase 4 | Build integration + testing | 2-4 hours |
| Phase 5 | Cleanup + documentation | 1-2 hours |
| **Total** | | **10-20 hours** |

---

## Future Enhancements

1. **Crowdin/Lokalise Integration** - Sync XLIFF files with translation management platform
2. **Machine Translation Fallback** - Use Azure Translator for untranslated strings
3. **Translation Coverage Reports** - CI check for translation completeness
4. **Localized IntelliSense** - Convert `/loc/intellisense/` XMLs for localized API docs

---

## References

- [Microsoft.DotNet.XliffTasks](https://github.com/dotnet/xliff-tasks)
- [XLIFF 1.2 Specification](http://docs.oasis-open.org/xliff/xliff-core/xliff-core.html)
- [.NET Globalization and Localization](https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization-and-localization)
- [VS SDK Localization](https://learn.microsoft.com/en-us/visualstudio/extensibility/localizing-vsix-packages)
