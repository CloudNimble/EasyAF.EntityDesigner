// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityFramework
{
    /// <summary>
    ///     The language a generated file is written in.
    /// </summary>
    /// <remarks>
    ///     The values previously mirrored System.Data.Entity.Design.LanguageOption, which lives in the .NET Framework's
    ///     in-box assembly and was removed along with ObjectContext code generation. They are declared explicitly here
    ///     so the enum keeps its numeric values without that dependency.
    /// </remarks>
    internal enum LanguageOption
    {
        /// <summary>
        ///     Generate C#.
        /// </summary>
        GenerateCSharpCode = 0,

        /// <summary>
        ///     Generate Visual Basic.
        /// </summary>
        GenerateVBCode = 1
    }
}
