// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     How <see cref="GroupingLayout" /> decided which shapes belong together.
    /// </summary>
    /// <remarks>
    ///     Tried in the order declared, first match winning, with <see cref="Structural" /> underneath them all
    ///     as a floor. The evidence the earlier ones need is evidence a person put there deliberately, so it is
    ///     preferred over anything inferred from the schema's shape. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal enum GroupingStrategy
    {

        /// <summary>
        ///     At least one shape already carries a <c>GroupName</c>, so the names in the file are used.
        /// </summary>
        Explicit = 0,

        /// <summary>
        ///     Enough shapes carry a non-default fill colour for the colouring to be a statement about grouping.
        /// </summary>
        FillColor = 1,

        /// <summary>
        ///     The schema has entities central enough to organize the rest around.
        /// </summary>
        HubAffinity = 2,

        /// <summary>
        ///     Nothing above applied, so groups come from which entities are connected to which.
        /// </summary>
        Structural = 3

    }

}
