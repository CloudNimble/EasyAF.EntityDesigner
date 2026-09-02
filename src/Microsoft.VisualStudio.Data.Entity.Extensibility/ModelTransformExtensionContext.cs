// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// Provides file and project information to Visual Studio extensions that extend the file loading and saving of .edmx files
    /// by the Entity Data Model Designer.
    /// </summary>
    /// <remarks>
    /// Handed to both methods of <see cref="IModelTransformExtension" />. Both the input and the output are EDMX; an extension
    /// reads or replaces <see cref="CurrentDocument" /> and may report problems through <see cref="Errors" />.
    /// </remarks>
    public abstract class ModelTransformExtensionContext : ExtensionContext
    {

        #region Properties

        /// <summary>
        /// The current .edmx file on which Visual Studio extensions may operate.
        /// </summary>
        /// <value>The document to read and modify, or a replacement document to assign.</value>
        /// <remarks>
        /// Starts out as a copy of <see cref="OriginalDocument" /> and carries forward the edits of every transform extension
        /// that has already run. An extension may edit it in place or assign an entirely new <see cref="XDocument" />.
        /// </remarks>
        public abstract XDocument CurrentDocument { get; set; }

        /// <summary>
        /// A list of errors that can be shown in the Visual Studio Error List when .edmx files are loaded or saved by the Entity
        /// Data Model Designer.
        /// </summary>
        /// <value>A mutable list to which an extension adds <see cref="ExtensionError" /> instances.</value>
        /// <remarks>
        /// Entries added here are written to the Error List. Adding anything to this list while loading causes the designer to
        /// discard the extension-produced document, whatever the severity of the entries; while saving, the entries are reported
        /// but the save still proceeds.
        /// </remarks>
        public abstract List<ExtensionError> Errors { get; }

        /// <summary>
        /// The original .edmx file that was loaded into memory.
        /// </summary>
        /// <value>The document as it stood before any serialization extension ran.</value>
        /// <remarks>
        /// Supplied so an extension can diff its own work against the starting point. Extensions must not modify this object.
        /// </remarks>
        public abstract XDocument OriginalDocument { get; }

        /// <summary>
        /// The current Visual Studio project item.
        /// </summary>
        /// <value>The <see cref="EnvDTE.ProjectItem" /> for the file being loaded or saved.</value>
        public abstract ProjectItem ProjectItem { get; }

        #endregion

    }

}
