// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using EnvDTE;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// Provides file and project information to Visual Studio extensions that add custom properties to objects visible in the
    /// Entity Data Model Designer or the Model Browser.
    /// </summary>
    /// <remarks>
    /// Handed to <see cref="IEntityDesignerExtendedProperty.CreateProperty(XElement, PropertyExtensionContext)" /> each time the
    /// selection changes. Beyond the project information inherited from <see cref="ExtensionContext" />, it is the source of the
    /// change scope an extension needs in order to write anything back into the model.
    /// </remarks>
    public abstract class PropertyExtensionContext : ExtensionContext
    {

        #region Properties

        /// <summary>
        /// The current Visual Studio project item.
        /// </summary>
        /// <value>The <see cref="EnvDTE.ProjectItem" /> for the model file whose contents are selected.</value>
        public abstract ProjectItem ProjectItem { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates an <see cref="EntityDesignerChangeScope" /> object and sets the string that will appear in the dropdown lists
        /// for the Undo and Redo buttons in Visual Studio.
        /// </summary>
        /// <param name="undoRedoDescription">The string that will appear in the dropdown lists for the Undo and Redo buttons in Visual Studio.</param>
        /// <returns>An instance of an EntityDesignerChangeScope.</returns>
        /// <remarks>
        /// The returned scope is used to add, delete and modify EDMX file content; all changes made within it form a single unit
        /// of work that the user can undo with one Undo command, so the description should read like a command name.
        /// <para>
        /// An <see cref="InvalidOperationException" /> is thrown when a scope returned by a previous call to
        /// <see cref="CreateChangeScope(string)" /> on this context is still active, or when an extension tries to add, delete
        /// or change XML content that lives in an XML namespace owned by Microsoft or by the Entity Data Model. Extensions may
        /// only write their own annotations.
        /// </para>
        /// </remarks>
        public abstract EntityDesignerChangeScope CreateChangeScope(string undoRedoDescription);

        #endregion

    }

}
