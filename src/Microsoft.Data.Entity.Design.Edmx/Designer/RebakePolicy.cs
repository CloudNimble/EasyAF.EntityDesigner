// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Edmx.Designer
{

    /// <summary>
    ///     What a modern re-layout does to connectors a human has hand-routed, persisted as the
    ///     <c>ManualRouteRebakePolicy</c> attribute on <c>Diagram</c>.
    /// </summary>
    /// <remarks>
    ///     A re-bake regenerates the automatic connectors regardless; this only governs the hand-routed ones. In
    ///     Visual Studio <see cref="Ask" /> prompts the user; a "don't ask again" choice writes <see cref="Keep" />
    ///     or <see cref="Clear" />. Headless never prompts and treats <see cref="Ask" /> as <see cref="Keep" />, so
    ///     it never destroys a manual route. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal enum RebakePolicy
    {

        /// <summary>
        ///     Ask the user on each re-bake. The default, so nothing is silently discarded.
        /// </summary>
        Ask = 0,

        /// <summary>
        ///     Keep hand-routed connectors; a re-bake regenerates only the automatic ones.
        /// </summary>
        Keep = 1,

        /// <summary>
        ///     Clear hand-routed connectors and lay everything out fresh.
        /// </summary>
        Clear = 2

    }

}
