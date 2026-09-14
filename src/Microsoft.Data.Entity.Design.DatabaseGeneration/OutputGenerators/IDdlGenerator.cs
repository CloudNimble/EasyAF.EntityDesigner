// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators
{
    /// <summary>
    ///     Produces the data definition language (DDL) that creates a database for a store model.
    /// </summary>
    /// <remarks>
    ///     The in-box implementation is <see cref="SsdlToDdl" />, which lives in this assembly so the Visual Studio wizard
    ///     and the command-line tool share one generator. A Visual Studio host may still supply a T4-backed implementation
    ///     when the user has chosen a custom <c>.tt</c> file.
    /// </remarks>
    public interface IDdlGenerator
    {
        /// <summary>
        ///     Generates the DDL that drops the objects described by <paramref name="existingSsdl" /> and creates those
        ///     described by <paramref name="ssdl" />.
        /// </summary>
        /// <param name="ssdl">The store model to create objects for.</param>
        /// <param name="existingSsdl">The store model currently recorded in the .edmx file, used to drop stale objects. May be empty.</param>
        /// <param name="edmParameterBag">The parameters the generator needs, such as the provider invariant name and database name.</param>
        /// <returns>The generated DDL script.</returns>
        string Generate(string ssdl, string existingSsdl, EdmParameterBag edmParameterBag);
    }
}
