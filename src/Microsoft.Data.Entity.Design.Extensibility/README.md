# Microsoft.Data.Entity.Design.Extensibility

This assembly is the public extension-point API for the Entity Data Model (EDMX) Designer in Visual Studio. It contains no
designer logic of its own: it is the set of interfaces, context classes and MEF metadata attributes that a third party
implements in order to plug into the designer, plus the small types those contracts pass around.

Reference it from a Visual Studio extension when you want to take part in what the designer does when it loads, saves or
generates a model, when you want to add rows to the Properties window for things the user selects, or when you want to group a
set of related extensions into a feature the user can switch on and off.

Everything in the assembly lives in the `Microsoft.Data.Entity.Design.Extensibility` namespace.

## Extension points

| Type | What it lets you do | When to use it |
| --- | --- | --- |
| `IModelConversionExtension` | Translate a custom on-disk file format to EDMX when a file is opened, and back again when it is saved. | The model should be stored in a format of your own rather than as `.edmx`. |
| `IModelTransformExtension` | See and rewrite the EDMX document itself on load and on save. | You need to inject, strip or normalize content - typically custom annotations - every time a model moves between disk and the designer. |
| `IModelGenerationExtension` | Post-process the EDMX the Entity Data Model Wizard reverse-engineers from a database, and the merged result the Update Model Wizard produces. | Custom content or conventions should be applied once, at generation time, rather than on every load and save. |
| `IEntityDesignerExtendedProperty` | Return an object whose public properties are shown in the Visual Studio Properties window for the current selection. | You want to expose your own annotations to the user as editable properties. |
| `IEntityDesignerLayer` | Define a named, user-toggleable group of extensions, with its own service provider, property extensions and selection plumbing. | A set of related extensions should behave like one optional designer feature, usually one that owns a tool window. |
| `ModelFileExtensionAttribute` | Metadata. Declares the file extension a conversion extension owns, which also registers that extension with the designer so such files can be opened at all. | Required on every `IModelConversionExtension`. |
| `EntityDesignerExtendedPropertyAttribute` | Metadata. Declares which selections (`EntityDesignerSelection` flags) a property extension applies to. | Required on every `IEntityDesignerExtendedProperty` discovered through MEF. |
| `EntityDesignerLayerAttribute` | Metadata. Tags any extension as belonging to a layer, so it is only active while that layer is enabled. | Optional. Omit it and the extension is always active. |

Supporting types:

| Type | Role |
| --- | --- |
| `ExtensionContext` | Base of every context object. Exposes the `EnvDTE.Project` and the targeted `EntityFrameworkVersion`. |
| `ModelConversionExtensionContext`, `ModelTransformExtensionContext`, `ModelGenerationExtensionContext`, `UpdateModelExtensionContext`, `PropertyExtensionContext` | The context handed to each extension point. They carry the documents involved, the `EnvDTE.ProjectItem`, and (where applicable) the error collection. |
| `EntityDesignerChangeScope` | An undoable unit of work, obtained from `PropertyExtensionContext.CreateChangeScope`. The only supported way to write into a model that is open in the designer. |
| `ExtensionError`, `ExtensionErrorSeverity` | Entries an extension adds to a context's `Errors` collection; the designer copies them into the Visual Studio Error List. |
| `EntityDesignerSelection` | Flags enum naming the kinds of object a user can select in the designer or Model Browser. |
| `WizardKind` | Tells a generation extension whether the Entity Data Model Wizard or the Update Model Wizard is running. |
| `ChangeEntityDesignerSelectionEventArgs` | Payload of `IEntityDesignerLayer.ChangeEntityDesignerSelection`, used to drive the designer's selection from a layer. |

`EntityDesignerCommand` and `IEntityDesignerCommandFactory` also live here and let an extension contribute commands to the
designer's context menu, but both are declared `internal`. Only assemblies listed in this project's `InternalsVisibleTo` can
implement them, so despite living in the extensibility assembly they are not currently part of the third-party surface.

## How discovery works

Extensions are found with the Managed Extensibility Framework. There is no registration file and no interface to call: you
export a type, and the designer asks for it.

1. Your extension class carries `[Export(typeof(TheExtensionPointInterface))]` plus whichever metadata attribute that
   extension point requires. The metadata attributes in this assembly are marked `[MetadataAttribute]`, so MEF folds their
   values into the export's metadata dictionary.
2. Your assembly is shipped in a VSIX that declares it as a `Microsoft.VisualStudio.MefComponent` asset, which is what puts it
   into Visual Studio's component model catalog.
3. Inside the designer, `EscherExtensionPointManager` (in `Microsoft.Data.Entity.Design`) resolves `SComponentModel` from
   Visual Studio, takes its `IComponentModel.DefaultExportProvider`, and calls `GetExports` for each extension-point interface.
   The metadata is read back through the interfaces `IEntityDesignerLayerData` (`LayerName`), `IEntityDesignerConversionData`
   (`FileExtension`) and `IEntityDesignerPropertyData` (`EntityDesignerSelection`), which is why the property names on the
   attributes matter.
4. The results are filtered by layer. If a layer manager is available, extensions tagged with `EntityDesignerLayerAttribute`
   are included only while that layer is enabled; if no layer manager is available at all, every layer-tagged extension is
   dropped. Extensions with no layer tag are always included.

Two consequences worth knowing:

- Order is whatever MEF returns. Transform and generation extensions all run, each seeing the previous one's output, so an
  extension must not assume it is first or last.
- Exactly one conversion extension may claim a given file extension. Opening a file whose extension has no converter, or has
  more than one, is an error and the file does not open.

Editing a model that is open in the designer goes through `PropertyExtensionContext.CreateChangeScope`, and the designer
checks the calling assembly's strong name before allowing it. An extension that is not signed with the designer's own key may
only add, delete and change XML in its own namespaces - attempting to touch a Microsoft- or EDM-owned namespace throws
`InvalidOperationException`.

## A worked example, end to end

The smallest useful extension is a transform extension that stamps an annotation into the model on save and removes it again
on load. Three pieces are involved: the project, the class, and the VSIX asset.

**1. The project.** A class library that references this assembly and MEF. This assembly multi-targets `netstandard2.0` and
`net10.0`, so pick whichever target framework your Visual Studio extension needs.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="Microsoft.Data.Entity.Design.Extensibility" />
    <Reference Include="System.ComponentModel.Composition" />
  </ItemGroup>

</Project>
```

**2. The extension.** One class, one export, no registration code.

```csharp
using System;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Extensibility;

namespace Contoso.EdmxTools
{

    [Export(typeof(IModelTransformExtension))]
    public class StampGeneratedBy : IModelTransformExtension
    {

        private static readonly XNamespace Contoso = "http://schemas.contoso.com/edmx/2026";

        public void OnAfterModelLoaded(ModelTransformExtensionContext context)
        {
            // Take our annotation back out so the designer never sees it.
            var document = context.CurrentDocument;

            foreach (var attribute in document.Root.Attributes(Contoso + "generatedBy"))
            {
                attribute.Remove();
            }

            context.CurrentDocument = document;
        }

        public void OnBeforeModelSaved(ModelTransformExtensionContext context)
        {
            var document = context.CurrentDocument;

            if (document.Root is null)
            {
                context.Errors.Add(new ExtensionError("The model has no root element.", 1, ExtensionErrorSeverity.Error));
                return;
            }

            // Put it back on the way to disk.
            document.Root.SetAttributeValue(Contoso + "generatedBy", Environment.UserName);
            context.CurrentDocument = document;
        }

    }

}
```

Note what the context gives you and what it expects back. `OriginalDocument` is the document as it stood before any transform
extension ran and must not be modified; `CurrentDocument` is the one you edit or replace, and it already carries the edits of
any extension that ran before you. Anything added to `Errors` is written to the Visual Studio Error List. During a load, adding
even one entry - whatever its severity - makes the designer discard the extension-produced document; during a save the entries
are reported but the save still goes ahead.

**3. The VSIX.** Add the assembly as a MEF component so Visual Studio's component model can see it.

```xml
<Assets>
  <Asset Type="Microsoft.VisualStudio.MefComponent" d:Source="Project" d:ProjectName="Contoso.EdmxTools" Path="|Contoso.EdmxTools|" />
</Assets>
```

Install the VSIX, open an `.edmx` file, save it, and the attribute appears in the file on disk but never in the designer.

The other extension points follow the same shape - export the interface, add the metadata attribute the point requires, and
implement the methods:

```csharp
[Export(typeof(IModelConversionExtension))]
[ModelFileExtension(".myedmx")]
public class MyConverter : IModelConversionExtension { /* ... */ }

[Export(typeof(IEntityDesignerExtendedProperty))]
[EntityDesignerExtendedProperty(EntityDesignerSelection.ConceptualModelEntityType)]
public class MyProperties : IEntityDesignerExtendedProperty { /* ... */ }

[Export(typeof(IEntityDesignerLayer))]
public class MyLayer : IEntityDesignerLayer { /* ... */ }
```

## Everything here operates on EDMX

It is worth being explicit about what a conversion extension is and is not. `IModelConversionExtension` changes the *file
format* the model is stored in; it does not teach the designer a different *schema*. On load you are handed the raw text of
your file and are expected to hand back an EDMX document; on save you are handed EDMX and are expected to hand back your text.
Everything downstream - the design surface, the Model Browser, validation, code generation, and every other extension point in
this assembly - continues to work purely on EDMX and never learns that your format exists.

So a conversion extension is the right tool for storing an EDMX-equivalent model as, say, JSON or a database record. It is not
a way to open a model that is not expressible as EDMX in the first place.

## Known gaps

Things that are understood but deliberately not addressed yet. None of them block writing an extension.

| Gap | Effect | Why it was left |
|---|---|---|
| Conversion metadata carries no identity | When two converters claim the same file extension the designer refuses to load the file, but the error can only name a converter that declared a `LayerName`; the rest are listed by position. | Naming the actual types means instantiating converters that are about to be rejected. Fixing it properly means adding a required identity member to `IEntityDesignerConversionData`, which is a breaking change to this assembly's public API. |
| `IEntityDesignerLayer.IsSealed` is never read | Implementing it has no effect on designer behaviour. | No consumer exists anywhere in the codebase, so the intended semantics cannot be recovered from the source. |
| Transform extensions have no ordering control | Every `IModelTransformExtension` runs, in whatever order MEF enumerated them, each mutating the document for the next. Two transforms touching the same annotation can produce different results between runs. | Unlike converters, composition is the intended behaviour here, so this cannot simply be made an error. It needs an explicit ordering mechanism. |
| `PropertyExtensionContextImpl.CreateChangeScope` renames its parameter | The override uses `name` where the base declares `undoRedoDescription`, so named arguments differ depending on the static type. | Cosmetic, but it is a published API, so changing it is a (small) source-breaking change for anyone using named arguments. |
