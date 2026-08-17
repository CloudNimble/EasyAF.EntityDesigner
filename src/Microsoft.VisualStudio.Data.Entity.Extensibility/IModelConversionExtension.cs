// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Exposes methods for converting a custom file format to and from the .edmx file format that is readable by the Entity Data Model Designer.
    /// </summary>
    /// <example>
    /// A conversion extension is discovered through MEF. It must be exported as
    /// <see cref="IModelConversionExtension" /> and annotated with
    /// <see cref="ModelFileExtensionAttribute" /> so the designer knows which file extension it owns.
    /// <code>
    /// [Export(typeof(IModelConversionExtension))]
    /// [ModelFileExtension(".myedmx")]
    /// public class MyModelConverter : IModelConversionExtension
    /// {
    ///     public void OnAfterFileLoaded(ModelConversionExtensionContext context)
    ///     {
    ///         // context.OriginalDocument contains the raw text of the ".myedmx" file that was opened.
    ///         // Translate it to EDMX and place the result in context.CurrentDocument.
    ///         var edmx = MyFormat.ToEdmx(context.OriginalDocument);
    ///         context.CurrentDocument.Root.ReplaceWith(edmx.Root);
    ///     }
    ///
    ///     public void OnBeforeFileSaved(ModelConversionExtensionContext context)
    ///     {
    ///         // context.CurrentDocument contains the EDMX the designer wants to persist and is read-only here.
    ///         // Translate it back to the custom format and place the result in context.OriginalDocument.
    ///         context.OriginalDocument = MyFormat.FromEdmx(context.CurrentDocument);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Implement this interface when the model should be persisted on disk in a format of your own rather than as EDMX. The
    /// designer itself never learns the custom format: it hands you the file contents on load and expects EDMX back, and hands
    /// you EDMX on save and expects the custom format back. Everything downstream of the conversion - the designer surface, the
    /// Model Browser, validation and code generation - continues to operate purely on EDMX.
    /// <para>
    /// Registering an extension for a file extension also registers that extension with the designer's document factory, so
    /// files with that extension can be opened in the Entity Data Model Designer.
    /// </para>
    /// <para>
    /// Exactly one conversion extension may claim a given file extension. If a file is opened whose extension has no registered
    /// converter, or if more than one converter claims it, the designer raises an error and the file does not open.
    /// </para>
    /// </remarks>
    public interface IModelConversionExtension
    {

        #region Public Methods

        /// <summary>
        /// Defines custom functionality for loading a file with a custom format and converting it to an .edmx format.
        /// </summary>
        /// <param name="context">Provides file and project information.</param>
        /// <remarks>
        /// Called immediately after the file has been read from disk and before the designer parses it as EDMX. Read the raw
        /// file text from <see cref="ModelConversionExtensionContext.OriginalDocument" /> and write the equivalent EDMX into
        /// <see cref="ModelConversionExtensionContext.CurrentDocument" />, which is pre-populated with an empty EDMX document
        /// for the targeted Entity Framework version. Problems that should be surfaced to the user without aborting the load
        /// can be added to <see cref="ModelConversionExtensionContext.Errors" />.
        /// </remarks>
        void OnAfterFileLoaded(ModelConversionExtensionContext context);

        /// <summary>
        /// Defines custom functionality for converting an .edmx file to a file with a custom format before the file is saved.
        /// </summary>
        /// <param name="context">Provides file and project information.</param>
        /// <remarks>
        /// Called immediately before the file is written to disk. Read the EDMX from
        /// <see cref="ModelConversionExtensionContext.CurrentDocument" /> and assign the serialized custom format to
        /// <see cref="ModelConversionExtensionContext.OriginalDocument" />; the string you assign is what gets written to the
        /// file. <see cref="ModelConversionExtensionContext.CurrentDocument" /> is write-protected during save and attempting
        /// to modify it throws an <see cref="System.InvalidOperationException" />.
        /// </remarks>
        void OnBeforeFileSaved(ModelConversionExtensionContext context);

        #endregion

    }

}
