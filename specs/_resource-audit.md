# Resource ownership audit

- resx files: **36**
- distinct keys: **1819**

## Keys referenced from another assembly

| Owner | Consumer | Strings | Non-string | Keys |
|---|---|---:|---:|---|
| Microsoft.Data.Entity.Design.Diagrams | **Microsoft.VisualStudio.Data.Entity.Package** | 31 | 0 | `CannotOpenDocument`, `FormatList`, `CannotSaveDocument`, `CannotCloseExistingDiagramDocument`, `BindingErrorOccurred`, `UnresolvedToolboxItem`, +25 more |
| Microsoft.Data.Entity.Design.XmlEngine | **Microsoft.VisualStudio.Data.Entity.XmlDesigner** | 14 | 0 | `NoneDisplayValueUsedForUX`, `DesignerViewCommandsText`, `SwitchConverterErrorMessage`, `BadInsertBadChildType`, `BadRemoveChildNotParent`, `RenameTransactionNameFormat`, +8 more |
| Microsoft.Data.Entity.Design.Edmx | **Microsoft.VisualStudio.Data.Entity.EdmxDesigner** | 5 | 0 | `NAME_NOT_UNIQUE`, `Model_DefaultEntityTypeName`, `Model_IdPropertyName`, `BadEnumTypeMemberValue`, `DatabaseObjectNameFormat` |
| Microsoft.Data.Entity.Design.Edmx | **Microsoft.Data.Entity.Design.XmlEngine** | 3 | 0 | `NAME_NOT_UNIQUE`, `INVALID_FORMAT`, `INVALID_NC_NAME_CHAR` |
| Microsoft.Data.Entity.Design.XmlEngine | **Microsoft.VisualStudio.Data.Entity.EdmxDesigner** | 2 | 0 | `NoneDisplayValueUsedForUX`, `ConverterIncorrectValueForAttribute` |
| Microsoft.Data.Entity.Design.Edmx | **Microsoft.VisualStudio.Data.Entity.Package** | 2 | 0 | `UpdateFromDatabaseExceptionMessage`, `GenerateDatabaseScriptExceptionMessage` |
| Microsoft.VisualStudio.Data.Entity.EdmxDesigner | **Microsoft.VisualStudio.Data.Entity.Package** | 2 | 0 | `ConfirmDeleteDialog_DescriptionLabel_Text`, `ConfirmDeleteDialog_Title` |
| Microsoft.Data.Entity.Design.XmlEngine | **Microsoft.Data.Entity.Design.Edmx** | 1 | 0 | `NoneDisplayValueUsedForUX` |
| Microsoft.VisualStudio.Data.Entity.EdmxDesigner | **Microsoft.Data.Entity.Design.Diagrams** | 1 | 0 | `Tx_CreateDiagram` |

## Unreferenced keys (candidates for deletion)

- **Microsoft.VisualStudio.Data.Entity.EdmxDesigner** — 574 unreferenced
- **Microsoft.Data.Entity.Design.Diagrams** — 207 unreferenced
- **Microsoft.Data.Entity.Design.XmlEngine** — 189 unreferenced
- **Microsoft.VisualStudio.Data.Entity.XmlDesigner** — 37 unreferenced
- **Microsoft.Data.Entity.Design.EntityFramework** — 31 unreferenced
- **Microsoft.VisualStudio.Data.Entity.Package** — 21 unreferenced
- **Microsoft.Data.Entity.Design.DatabaseGeneration** — 4 unreferenced
- **Microsoft.Data.Entity.Design.Edmx** — 3 unreferenced
