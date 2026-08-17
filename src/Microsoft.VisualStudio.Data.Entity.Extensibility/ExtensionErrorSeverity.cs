// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// An enumeration that describes the severity of an <see cref="ExtensionError" />.
    /// </summary>
    /// <remarks>
    /// The severity controls which category the entry lands in when the designer writes it to the Visual Studio Error List. Note
    /// that during a model load the designer abandons the extension's work as soon as any error is reported, whatever severity
    /// was chosen.
    /// </remarks>
    public enum ExtensionErrorSeverity
    {

        /// <summary>
        /// Indicates that the severity of the <see cref="ExtensionError" /> is Warning. An <see cref="ExtensionError" /> with
        /// this severity will appear in the Visual Studio Error List as a warning.
        /// </summary>
        Warning = 0,

        /// <summary>
        /// Indicates that the severity of the <see cref="ExtensionError" /> is Error. An <see cref="ExtensionError" /> with this
        /// severity will appear in the Visual Studio Error List as an error.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Indicates that the severity of the <see cref="ExtensionError" /> is Message. An <see cref="ExtensionError" /> with
        /// this severity will appear in the Visual Studio Error List as a message.
        /// </summary>
        Message = 2,

    }

}
