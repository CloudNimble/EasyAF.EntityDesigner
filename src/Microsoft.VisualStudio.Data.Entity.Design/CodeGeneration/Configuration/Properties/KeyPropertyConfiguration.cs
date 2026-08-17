// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Properties
{
    /// <summary>
    /// Represents a model configuration to include a property as part of the entity's key.
    /// </summary>
    public class KeyPropertyConfiguration : IAttributeConfiguration
    {
        /// <inheritdoc />
        public virtual string GetAttributeBody(CodeHelper code)
        {
            Debug.Assert(code != null, "code is null.");

            return "Key";
        }
    }
}
