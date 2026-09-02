// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators
{
    /// <summary>
    ///     Produces the data definition language (DDL) that creates a database for a store model.
    /// </summary>
    /// <remarks>
    ///     The in-box implementation runs a T4 template through Visual Studio's text templating service, which is why it
    ///     lives in Microsoft.Data.Entity.Design rather than here. Inverting the dependency this way keeps this assembly
    ///     free of any Visual Studio reference, so it can target .NET Standard alongside .NET Framework.
    /// </remarks>
    public interface IDdlGenerator
    {
        /// <summary>
        ///     Generates the DDL that drops the objects described by <paramref name="existingSsdl" /> and creates those
        ///     described by <paramref name="ssdl" />.
        /// </summary>
        /// <param name="ssdl">The store model to create objects for.</param>
        /// <param name="existingSsdl">The store model currently recorded in the .edmx file, used to drop stale objects. May be empty.</param>
        /// <param name="edmParameterBag">The parameters the generator needs, such as the provider invariant name and DDL template path.</param>
        /// <returns>The generated DDL script.</returns>
        string Generate(string ssdl, string existingSsdl, EdmParameterBag edmParameterBag);
    }
}
