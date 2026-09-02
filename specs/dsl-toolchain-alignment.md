# Aligning the DSL Toolchain

Making `DslDefinition.dsl` regenerable again, on a pinned baseline, emitting modern C#.

## The problem

The DSL codegen is not reproducible today. The repo builds against Modeling SDK **17.10** packages, the only installed DSL SDK is **18.0**, and regenerating from it produces code that does not compile against what we reference. `DslDefinition.dsl` is therefore frozen in practice: any change to the DSL means hand-editing `GeneratedCode\*.cs`, which is exactly the drift that bit the `CodeGeneration\Generators\GeneratedCode` templates during the namespace rename.

This was measured, not assumed. `msbuild -t:TransformAll -p:TransformOutOfDateOnly=false` regenerates all 16 outputs with zero template errors, and the result differs by 475 insertions / 518 deletions ignoring whitespace. Two things then break:

| Break | Detail |
|---|---|
| `InternalSaveModel` sealed | The 18.0 template no longer marks it `virtual`. `MicrosoftDataEntityDesignSerializationHelper` overrides it to write the EDMX out of the document buffer instead of serializing the DSL store. **Without that override, saving in the designer overwrites the .edmx with DSL XML.** |
| `ModelingTextTransformation` | `DirectiveProcessor.cs` gains a real `typeof(...)` where the old output only had a CodeDom string, so it now needs `Microsoft.VisualStudio.TextTemplating.Modeling.dll`. That assembly ships in `Common7\IDE\PublicAssemblies`, not in the `TextTemplating.VSHost` package pinned at 17.10. |

Note the corollary: regenerating **inside the VS 18 IDE hits both of these identically**. The constraint is the SDK version, not the command line. The command line only made it visible.

The 475/518-line diff contains no modernization. It is added `CA1502` suppressions, a swap of the serialization extension hook (`ReadAdditionalElementData` → `ReadExtensions`/`WriteExtensions`, a DSL extensions feature this project does not use), and the sealing above. Adopting it buys nothing on its own — which is why it is worth doing only as part of this larger piece of work, where it buys reproducibility and a path to better output.

## The enabler: vendor the templates

The DSL SDK templates live in `Common7\IDE\Extensions\Microsoft\DSL SDK\DSL Designer\18.0\TextTemplates` — 61 files, about 25,000 lines of T4. Copy them into the repository and point `DslTemplatesSrc` at the vendored copy.

Three things follow, and none are possible without it:

1. **Regeneration stops depending on which Visual Studio is installed.** The baseline is whatever is committed, so a teammate on a different VS gets identical output.
2. **Template changes become reviewable.** Right now a VS update silently changes the codegen; vendored, it is a diff.
3. **Modernization becomes possible at all.** The templates in Program Files are shared by every DSL project on the machine and are replaced on update, so they cannot be edited in place.

The vendored copy is a fork. That is the real cost: upstream fixes have to be merged deliberately. Given that DSL Tools is in maintenance and the templates change rarely, that cost is low and the reproducibility is worth it.

## Work items

1. **Vendor the templates.** Copy the `TextTemplates` tree into the repo. Point `DslTemplatesSrc` at it, replacing the environment variable requirement in `Microsoft.Data.Entity.Design.Dsl.csproj` with a repo-relative default. Commit the unmodified copy first, so later modernization diffs are legible.
2. **Align versions.** Bump the Modeling SDK and TextTemplating packages to match the vendored baseline, and add the reference `DirectiveProcessor.cs` now needs. Confirm the packages exist on the configured feeds before committing to this — 18.x may not be published, in which case the vendored templates get patched to keep the CodeDom string form instead.
3. **Adopt the regenerated baseline.** Relocate the `InternalSaveModel` override onto `SaveModel`, which is still virtual, replicating the base class's file write. Change `SaveModelAndDiagram` to call `SaveModel` rather than `base.SaveModel`, or it silently bypasses the override and serializes the view model over the .edmx. This edit is written and verified to compile; it is recorded here rather than applied.
4. **Verify the save path by hand.** See Verification below. This is the highest risk in the whole plan and has no automated coverage.
5. **Modernize the templates.** Only after 1–4 are green, so that a codegen regression can be told apart from a version-alignment regression.

## What "modern" means here

Per the repo conventions in `CLAUDE.md`, and applied to template output:

- `#nullable enable`, with generated signatures annotated. Declare non-nullable, check at entry points.
- `is null` / `is not null` rather than `== null` / `!= null`.
- Pattern matching in place of `as` followed by a null test — the generated serializers are full of this shape.
- Collection expressions and collection initializers.
- Target-typed `new`.
- Expression-bodied members for trivial accessors.
- `nameof` instead of string literals for member names.
- XML documentation that is actually useful, rather than restating the signature.

Explicitly **not** changing: normal namespace declarations stay (the repo forbids file-scoped), a newline stays before every opening brace, and the defensive `global::` prefixes stay — they exist so generated code cannot be broken by a user-declared namespace, which matters more in generated code than terseness does.

Sequence the modernization one template at a time, regenerating and diffing after each. `Serializer.cs` and `SerializationHelper.cs` are the largest outputs and should go last.

## Verification

- `edmx render Northwind.edmx` produces byte-identical SVG. Baseline: 51 rects, 36 connector paths, 152 text elements.
- The solution builds clean and the test suite passes.
- **Manual save test, required.** Open an EDMX in the designer, change something, save, and confirm the file is still EDMX rather than DSL XML. The render harness never saves, so nothing else covers work item 3, and the failure mode is silent data loss over the user's model.
- Regeneration is idempotent: run `TransformAll` twice and confirm the second run produces no diff.

## Sequencing

This does not belong inside the DSL/shell decoupling. It touches the serialization save path, which the decoupling does not, and it would make a regression in either one hard to attribute. Land it before or after, on its own branch, with the manual save test performed each time.
