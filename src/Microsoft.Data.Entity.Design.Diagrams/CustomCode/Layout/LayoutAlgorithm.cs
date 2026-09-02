// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     How <see cref="MsAglLayoutEngine" /> decides where to put the shapes.
    /// </summary>
    /// <remarks>
    ///     Named for what each one does rather than for the paper it came from. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal enum LayoutAlgorithm
    {

        /// <summary>
        ///     Arranges shapes in rows with the edges flowing one direction, like an org chart. An entity sits in
        ///     a row above the entities that reference it.
        /// </summary>
        /// <remarks>
        ///     The default. In a database a foreign key means "this table depends on that one", and rows put that
        ///     on screen as "up means depended-on".
        /// </remarks>
        Layered = 0,

        /// <summary>
        ///     A physics simulation: shapes repel each other and edges pull like springs. Produces organic
        ///     clusters with no consistent direction.
        /// </summary>
        ForceDirected = 1

    }

}
