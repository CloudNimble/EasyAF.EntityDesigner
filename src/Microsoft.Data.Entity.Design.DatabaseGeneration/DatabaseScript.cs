// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.DatabaseGeneration
{
    /// <summary>
    ///     The artifacts produced by <see cref="DatabaseScriptGenerator" /> for a conceptual model.
    /// </summary>
    public sealed class DatabaseScript
    {
        #region Properties

        /// <summary>
        ///     The data definition language that creates the database described by <see cref="Ssdl" />.
        /// </summary>
        public string Ddl { get; }

        /// <summary>
        ///     The mapping specification language that maps the conceptual model onto <see cref="Ssdl" />.
        /// </summary>
        public string Msl { get; }

        /// <summary>
        ///     The store schema definition language inferred from the conceptual model.
        /// </summary>
        public string Ssdl { get; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a <see cref="DatabaseScript" />.
        /// </summary>
        /// <param name="ssdl">The generated store schema definition language.</param>
        /// <param name="msl">The generated mapping specification language.</param>
        /// <param name="ddl">The generated data definition language.</param>
        public DatabaseScript(string ssdl, string msl, string ddl)
        {
            Ssdl = ssdl;
            Msl = msl;
            Ddl = ddl;
        }

        #endregion
    }
}
