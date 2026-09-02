// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using System;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// A base class for the <see cref="ModelGenerationExtensionContext" />, <see cref="PropertyExtensionContext" />,
    /// <see cref="ModelTransformExtensionContext" /> and <see cref="ModelConversionExtensionContext" /> classes.
    /// </summary>
    /// <remarks>
    /// The designer creates the context, fills it in and passes it to an extension for the duration of a single callback. It is
    /// the only supported way for an extension to learn which project and which Entity Framework version the operation applies
    /// to, and extensions must not hold on to a context after the callback that supplied it returns.
    /// </remarks>
    public abstract class ExtensionContext
    {

        #region Properties

        /// <summary>
        /// The targeted version of the Entity Framework.
        /// </summary>
        /// <value>The version of the Entity Framework the project targets, which determines the EDMX schema namespaces in use.</value>
        /// <remarks>
        /// Use this to decide which schema namespaces to read and emit. An extension that writes EDMX content must match the
        /// namespaces for this version or the designer will not recognise what it produced.
        /// </remarks>
        public abstract Version EntityFrameworkVersion { get; }

        /// <summary>
        /// The current Visual Studio project.
        /// </summary>
        /// <value>The <see cref="EnvDTE.Project" /> that contains the model being loaded, saved or generated.</value>
        /// <remarks>
        /// This is the live automation object, so an extension can read project properties, references and items - for example
        /// to look up a configuration file or to decide whether the extension applies to this project at all.
        /// </remarks>
        public abstract Project Project { get; }

        #endregion

    }

}
