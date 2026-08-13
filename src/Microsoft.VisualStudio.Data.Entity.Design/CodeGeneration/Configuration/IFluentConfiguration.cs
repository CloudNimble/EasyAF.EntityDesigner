// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration
{
    /// <summary>
    /// Represents a model configuration that can be applied using the Code First Fluent API.
    /// </summary>
    public interface IFluentConfiguration : IConfiguration
    {
        /// <summary>
        /// Gets the Fluent API method chain to apply the configuration.
        /// </summary>
        /// <param name="code">The helper used to generate code.</param>
        /// <returns>The method chain.</returns>
        string GetMethodChain(CodeHelper code);
    }
}
