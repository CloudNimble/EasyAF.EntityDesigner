// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Types
{
    /// <summary>
    /// Represents a model configuration to set the key of an entity.
    /// </summary>
    public class KeyConfiguration : IFluentConfiguration
    {
        private readonly ICollection<EdmProperty> _keyProperties = [];

        /// <summary>
        /// Gets the properties used for the key of the entity.
        /// </summary>
        public ICollection<EdmProperty> KeyProperties
        {
            get { return _keyProperties; }
        }

        /// <inheritdoc />
        public virtual string GetMethodChain(CodeHelper code)
        {
            // TODO: Throw instead?
            Debug.Assert(code != null, "code is null.");
            Debug.Assert(_keyProperties.Count != 0, "_keyProperties is empty.");

            return ".HasKey(" + code.Lambda(_keyProperties) + ")";
        }
    }
}
