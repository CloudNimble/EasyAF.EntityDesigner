// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Properties
{
    /// <summary>
    /// Represents a model configuration to mark a string property as non-Unicode.
    /// </summary>
    public class NonUnicodeConfiguration : IFluentConfiguration
    {
        /// <inheritdoc />
        public virtual string GetMethodChain(CodeHelper code)
        {
            return ".IsUnicode(" + code.Literal(false) + ")";
        }
    }
}
