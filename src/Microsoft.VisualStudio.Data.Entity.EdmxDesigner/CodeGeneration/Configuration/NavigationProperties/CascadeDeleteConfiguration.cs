// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Text;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.NavigationProperties
{
    /// <summary>
    /// Represents a model configuration to set the cascade delete option of an association.
    /// </summary>
    public class CascadeDeleteConfiguration : IFluentConfiguration
    {
        /// <summary>
        /// Gets or sets the cascade delete option.
        /// </summary>
        public OperationAction DeleteBehavior { get; set; }

        /// <inheritdoc />
        public virtual string GetMethodChain(CodeHelper code)
        {
            Debug.Assert(code != null, "code is null.");

            StringBuilder builder = new StringBuilder();

            builder.Append(".WillCascadeOnDelete(");

            if (DeleteBehavior != OperationAction.Cascade)
            {
                Debug.Assert(DeleteBehavior == OperationAction.None, "DeleteBehavior is not None.");

                builder.Append(code.Literal(false));
            }

            builder.Append(")");

            return builder.ToString();
        }
    }
}
