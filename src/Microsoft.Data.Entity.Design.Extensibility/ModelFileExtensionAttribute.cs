// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Specifies a custom file extension that can be loaded or saved by the Entity Data Model Designer.
    /// </summary>
    /// <example>
    /// This attribute is MEF metadata: it only has an effect when it sits alongside an <c>Export</c> of
    /// <see cref="IModelConversionExtension" />.
    /// <code>
    /// [Export(typeof(IModelConversionExtension))]
    /// [ModelFileExtension(".myedmx")]
    /// public class MyModelConverter : IModelConversionExtension
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Applying this attribute to a conversion extension does two things: it registers the file extension with the designer so
    /// files of that kind can be opened in the Entity Data Model Designer at all, and it tells the designer which extension to
    /// call when such a file is loaded or saved. Only one conversion extension may claim any given file extension.
    /// </remarks>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ModelFileExtensionAttribute : Attribute
    {

        #region Fields

        private readonly string _fileExtension;

        #endregion

        #region Properties

        /// <summary>
        /// The file extension for custom files that can be loaded and saved by the Entity Data Model Designer.
        /// </summary>
        /// <value>The file extension the annotated conversion extension owns.</value>
        /// <remarks>
        /// Specifies the file extension for which the model conversion extension class will be called. Can optionally include a
        /// leading "."; the designer adds one if it is missing, and matches the extension case-insensitively.
        /// </remarks>
        public string FileExtension
        {
            get => _fileExtension;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="ModelFileExtensionAttribute" /> class.
        /// </summary>
        /// <param name="fileExtension">The file extension for custom files that can be loaded and saved by the Entity Data Model Designer.</param>
        public ModelFileExtensionAttribute(string fileExtension)
        {
            _fileExtension = fileExtension;
        }

        #endregion

    }

}
