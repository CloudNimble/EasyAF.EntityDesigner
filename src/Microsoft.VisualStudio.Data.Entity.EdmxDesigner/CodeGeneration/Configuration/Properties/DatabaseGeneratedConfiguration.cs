// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Extensions;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties
{
    /// <summary>
    /// Represents a model configuration to set the database generated option of a property.
    /// </summary>
    public class DatabaseGeneratedConfiguration : IAttributeConfiguration, IFluentConfiguration
    {
        /// <summary>
        /// Gets or sets the pattern used to generate values for the property in the database.
        /// </summary>
        public StoreGeneratedPattern StoreGeneratedPattern { get; set; }

        /// <inheritdoc />
        public virtual string GetAttributeBody(CodeHelper code)
        {
            Debug.Assert(code != null, "code is null.");

            return "DatabaseGenerated(DatabaseGeneratedOption."
                + StoreGeneratedPattern.ToDatabaseGeneratedOption()
                + ")";
        }

        /// <inheritdoc />
        public virtual string GetMethodChain(CodeHelper code)
        {
            Debug.Assert(code != null, "code is null.");

            return ".HasDatabaseGeneratedOption(DatabaseGeneratedOption."
                + StoreGeneratedPattern.ToDatabaseGeneratedOption()
                + ")";
        }
    }
}
