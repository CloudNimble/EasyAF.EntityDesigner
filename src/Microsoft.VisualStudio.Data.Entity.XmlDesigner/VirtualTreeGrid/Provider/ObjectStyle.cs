// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Determines the style of object to be returned by the IBranch.GetObject function.
    /// </summary>
    internal enum ObjectStyle
    {
        /// <summary>
        ///     Get the expanded branch at this location. The returned object
        ///     should be an IBranch or ITree implementation. Returning
        ///     a tree will clone all branches from the tree into the current
        ///     location. If the ITree is an alien implementation to the tree
        ///     making the request, then only currently expanded items can be
        ///     included in the current tree. Use the ExpansionOptions enum
        ///     to specify more options with GetObject, and the BranchLocationAction
        ///     settings with LocateObject.
        /// </summary>
        ExpandedBranch,

        /// <summary>
        ///     Get an object that uniquely identifies this item. Used to
        ///     maintain item selection when a list is rearranged. Use the
        ///     TrackingObjectAction enum values with LocateObject and this style.
        /// </summary>
        TrackingObject,

        /// <summary>
        ///     Get the root branch for a complex subitem. Root branches are
        ///     requested for each row in a column with a cell style of Complex
        ///     or Mixed. Use the ExpansionOptions enum to specify more options.
        /// </summary>
        SubItemRootBranch,

        /// <summary>
        ///     Get the expanded branch for a subitem cell. Use ExpansionOptions enum
        ///     to specify more options.
        /// </summary>
        SubItemExpansion,
        // If an item is added here, VirtualTreeConstant.FirstUserObjectStyle must be updated
    }

}
