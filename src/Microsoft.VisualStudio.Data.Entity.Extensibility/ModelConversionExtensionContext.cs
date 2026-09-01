// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// Provides file and project information to Visual Studio extensions that enable the loading and saving of custom file
    /// formats.
    /// </summary>
    /// <remarks>
    /// Handed to both methods of <see cref="IModelConversionExtension" />. Which of <see cref="CurrentDocument" /> and
    /// <see cref="OriginalDocument" /> is the input and which is the output depends on the direction of the conversion: on load
    /// the extension reads the custom text from <see cref="OriginalDocument" /> and fills in <see cref="CurrentDocument" />, and
    /// on save it reads EDMX from <see cref="CurrentDocument" /> and assigns the custom text to
    /// <see cref="OriginalDocument" />.
    /// </remarks>
    public abstract class ModelConversionExtensionContext : ExtensionContext
    {

        #region Properties

        /// <summary>
        /// Returns the .edmx document after it has been converted from a custom file format.
        /// </summary>
        /// <value>The EDMX form of the model.</value>
        /// <remarks>
        /// While loading, this is pre-populated with an empty EDMX document for the targeted Entity Framework version and the
        /// extension is expected to fill it in. While saving, it holds the EDMX the designer wants to persist and is
        /// write-protected: attempting to modify it throws an <see cref="InvalidOperationException" />.
        /// </remarks>
        public abstract XDocument CurrentDocument { get; }

        /// <summary>
        /// A list of errors that can be shown in the Visual Studio Error List when loading a custom file format and converting
        /// it to a custom file format.
        /// </summary>
        /// <value>A mutable list to which an extension adds <see cref="ExtensionError" /> instances.</value>
        /// <remarks>
        /// Entries added here are written to the Error List once the load or save completes.
        /// </remarks>
        public abstract List<ExtensionError> Errors { get; }

        /// <summary>
        /// Returns information about the custom file being processed by the Entity Data Model Designer.
        /// </summary>
        /// <value>A <see cref="System.IO.FileInfo" /> describing the file on disk.</value>
        /// <remarks>
        /// Its <see cref="System.IO.FileSystemInfo.Extension" /> is what the designer matched against
        /// <see cref="ModelFileExtensionAttribute.FileExtension" /> in order to choose this extension.
        /// </remarks>
        public abstract FileInfo FileInfo { get; }

        /// <summary>
        /// Returns the original document as opened or saved by the Entity Designer.
        /// </summary>
        /// <value>The contents of the custom-format file, as text.</value>
        /// <remarks>
        /// While loading, this holds the raw text that was read from disk and is the extension's input. While saving, the
        /// extension assigns the serialized custom format to it and that string is what gets written to the file.
        /// </remarks>
        public abstract string OriginalDocument { get; set; }

        /// <summary>
        /// The current Visual Studio project item.
        /// </summary>
        /// <value>The <see cref="EnvDTE.ProjectItem" /> for the custom-format file being loaded or saved.</value>
        public abstract ProjectItem ProjectItem { get; }

        #endregion

    }

}
