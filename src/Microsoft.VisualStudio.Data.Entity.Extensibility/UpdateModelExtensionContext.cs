// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;
using EnvDTE;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Provides file and project information to Visual Studio extensions that extend the .edmx file update process of the Update
    /// Model Wizard.
    /// </summary>
    /// <remarks>
    /// Handed to <see cref="IModelGenerationExtension.OnAfterModelUpdated(UpdateModelExtensionContext)" />. It adds the
    /// before-and-after documents of the update to what <see cref="ModelGenerationExtensionContext" /> already exposes, which is
    /// what an extension needs in order to re-apply information the merge did not carry forward. As during generation, only
    /// <see cref="ModelGenerationExtensionContext.CurrentDocument" /> may be modified.
    /// </remarks>
    public abstract class UpdateModelExtensionContext : ModelGenerationExtensionContext
    {

        #region Properties

        /// <summary>
        /// Represents the .edmx file before the Update Model Wizard has run.
        /// </summary>
        /// <value>The model exactly as it was on disk before the update started.</value>
        /// <remarks>
        /// Read-only. This is where an extension looks for its own annotations, so it can put them back into
        /// <see cref="ModelGenerationExtensionContext.CurrentDocument" /> if the merge dropped them.
        /// </remarks>
        public abstract XDocument OriginalDocument { get; }

        /// <summary>
        /// The current Visual Studio project item.
        /// </summary>
        /// <value>The <see cref="EnvDTE.ProjectItem" /> for the model file being updated.</value>
        public abstract ProjectItem ProjectItem { get; }

        /// <summary>
        /// Represents the .edmx file after the Update Model Wizard has run.
        /// </summary>
        /// <value>The merged model.</value>
        /// <remarks>
        /// This is the model that was generated from the database, plus modifications from extensions, plus the designer's
        /// internal "update model" merge logic. Read-only.
        /// </remarks>
        public abstract XDocument UpdateModelDocument { get; }

        #endregion

    }

}
