// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// An enumeration that provides information about which wizard started an .edmx file generation or update process.
    /// </summary>
    /// <remarks>
    /// Reported by <see cref="ModelGenerationExtensionContext.WizardKind" />. An
    /// <see cref="IModelGenerationExtension" /> can use it to tell a first-time generation apart from a refresh of an existing
    /// model without having to inspect the type of the context it was given.
    /// </remarks>
    public enum WizardKind
    {

        /// <summary>
        /// Indicates that no wizard started an .edmx file modification process.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the Entity Data Model Wizard started an .edmx file generation process.
        /// </summary>
        Generate = 1,

        /// <summary>
        /// Indicates that the Update Model Wizard started an .edmx file update process.
        /// </summary>
        UpdateModel = 2,

    }

}
