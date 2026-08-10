// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;

namespace Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators
{
    /// <summary>
    ///     Produces a schema artifact from a conceptual model.
    /// </summary>
    /// <remarks>
    ///     Implemented by <see cref="CsdlToSsdl" /> and <see cref="CsdlToMsl" />, which
    ///     <see cref="DatabaseScriptGenerator" /> composes into the store model and mappings for a database script.
    ///     This replaces the former <c>IGenerateActivityOutput</c>, whose signature was shaped by the Windows Workflow
    ///     activity that used to host it; the generators never used the workflow context for anything but parameter lookup.
    /// </remarks>
    public interface ISchemaGenerator
    {
        /// <summary>
        ///     Generates a schema artifact from the supplied conceptual model.
        /// </summary>
        /// <param name="edmItemCollection">The conceptual model to generate from.</param>
        /// <param name="edmParameterBag">The parameters the generator needs, such as the target Entity Framework version.</param>
        /// <returns>The serialized schema artifact.</returns>
        string Generate(EdmItemCollection edmItemCollection, EdmParameterBag edmParameterBag);
    }
}
