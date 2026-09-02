// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Edmx.Designer
{

    /// <summary>
    ///     Which engine arranges a diagram, persisted as the <c>LayoutMode</c> attribute on <c>Diagram</c>.
    /// </summary>
    /// <remarks>
    ///     Named for what the user chose rather than for the implementation behind it, because this is what they
    ///     see in the property window and what they read in the EDMX. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal enum LayoutMode
    {

        /// <summary>
        ///     The Modeling SDK's own layout. The default, so a file written before this attribute existed arranges
        ///     exactly as it always did.
        /// </summary>
        Legacy = 0,

        /// <summary>
        ///     MSAGL places the shapes and routes the connectors, and the routes are persisted to the EDMX.
        /// </summary>
        Modern = 1

    }

}
