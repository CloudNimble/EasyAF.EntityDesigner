// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties
{
    /// <summary>
    /// Represents a model configuration to set the precision and scale of a decimal property.
    /// </summary>
    public class PrecisionDecimalConfiguration : IFluentConfiguration
    {
        /// <summary>
        /// Gets or sets the precision of the property.
        /// </summary>
        public byte Precision { get; set; }

        /// <summary>
        /// Gets or sets the scale of the property.
        /// </summary>
        public byte Scale { get; set; }

        /// <inheritdoc />
        public virtual string GetMethodChain(CodeHelper code)
        {
            Debug.Assert(code != null, "code is null.");

            return ".HasPrecision(" + code.Literal(Precision) + ", " + code.Literal(Scale) + ")";
        }
    }
}
