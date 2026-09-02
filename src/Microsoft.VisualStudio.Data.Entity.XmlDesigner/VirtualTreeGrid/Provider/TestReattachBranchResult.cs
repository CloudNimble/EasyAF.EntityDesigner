// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <remarks>
    ///     Return codes for the ILevelShiftAdjuster.TestReattachBranch method.
    ///     The results allow any branch in the kill zone to be discarded (the default
    ///     if ILevelShiftAdjuster is not specified), or added to the reattach list for
    ///     processing when the branch structure is put back together. The other options
    ///     specify what needs to be done if the branch is kept.
    /// </remarks>
    internal enum TestReattachBranchResult
    {
        /// <summary>
        ///     The branch is discarded and its children (below the kill zone) are
        ///     reattached (to new branches, if necessary).
        /// </summary>
        Discard,

        /// <summary>
        ///     The branch is kept with its child branches and tracking objects
        ///     are not included in the reattach phase.
        /// </summary>
        ReattachIntact,

        /// <summary>
        ///     The child branches and tracking objects are detached from the parent,
        ///     but the parent branch is not discarded, and maintains its expansion state.
        /// </summary>
        ReattachChildren,

        /// <summary>
        ///     Includes behavior of ReattachChildren. The child branches and
        ///     tracking objects are detached and the item count of the branch is requeried.
        ///     This is very similar to a realign on the branch,
        ///     except that the existing child branches can potentially end up
        ///     reattached to other branches, with is not possible with an ITree.Realign call.
        /// </summary>
        Realign,
    }

}
