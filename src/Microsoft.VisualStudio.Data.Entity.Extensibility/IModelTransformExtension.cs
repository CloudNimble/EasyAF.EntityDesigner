// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Exposes methods for extending the loading and saving processes of .edmx files.
    /// </summary>
    /// <example>
    /// A transform extension is discovered through MEF and needs no metadata attribute beyond the export itself, because it
    /// applies to every .edmx document the designer opens or saves.
    /// <code>
    /// [Export(typeof(IModelTransformExtension))]
    /// public class StripMyAnnotations : IModelTransformExtension
    /// {
    ///     public void OnAfterModelLoaded(ModelTransformExtensionContext context)
    ///     {
    ///         // Rewrite the in-memory EDMX before the designer parses it.
    ///         var document = context.CurrentDocument;
    ///         MyAnnotations.Expand(document);
    ///         context.CurrentDocument = document;
    ///     }
    ///
    ///     public void OnBeforeModelSaved(ModelTransformExtensionContext context)
    ///     {
    ///         // Rewrite the EDMX on its way to disk.
    ///         var document = context.CurrentDocument;
    ///         MyAnnotations.Collapse(document);
    ///         context.CurrentDocument = document;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Implement this interface to see and change the EDMX document itself as it moves between the file and the designer. Unlike
    /// <see cref="IModelConversionExtension" />, this extension does not change the format the model is stored in - both the
    /// input and the output are EDMX. Use it to inject or strip custom annotations, normalize content, or validate the document
    /// and report problems to the Visual Studio Error List.
    /// <para>
    /// All registered transform extensions run, one after another, in the order MEF returns them; each one sees the document as
    /// the previous one left it. When a custom file format is in play, conversion runs first on load and last on save, so a
    /// transform extension always works on EDMX.
    /// </para>
    /// </remarks>
    public interface IModelTransformExtension
    {

        #region Public Methods

        /// <summary>
        /// Defines functionality for extending the process by which an .edmx file is loaded by the Entity Data Model Designer.
        /// </summary>
        /// <param name="context">Provides file and Visual Studio project information.</param>
        /// <remarks>
        /// Called after the file has been read and parsed but before the designer builds its model from it. Modify (or replace)
        /// <see cref="ModelTransformExtensionContext.CurrentDocument" />;
        /// <see cref="ModelTransformExtensionContext.OriginalDocument" /> is the untouched document and must not be modified.
        /// Adding anything to <see cref="ModelTransformExtensionContext.Errors" /> during load causes the designer to discard
        /// the extension-produced document and surface the entries in the Error List.
        /// </remarks>
        void OnAfterModelLoaded(ModelTransformExtensionContext context);

        /// <summary>
        /// Defines functionality for extending the process by which an .edmx file is saved by the Entity Data Model Designer.
        /// </summary>
        /// <param name="context">Provides file and project information.</param>
        /// <remarks>
        /// Called before the document is written to disk, and before any <see cref="IModelConversionExtension" /> converts it to
        /// a custom format. Modify (or replace) <see cref="ModelTransformExtensionContext.CurrentDocument" />; whatever it holds
        /// when the last extension returns is what gets persisted. Entries added to
        /// <see cref="ModelTransformExtensionContext.Errors" /> during save are reported in the Error List but do not cancel the
        /// save.
        /// </remarks>
        void OnBeforeModelSaved(ModelTransformExtensionContext context);

        #endregion

    }

}
