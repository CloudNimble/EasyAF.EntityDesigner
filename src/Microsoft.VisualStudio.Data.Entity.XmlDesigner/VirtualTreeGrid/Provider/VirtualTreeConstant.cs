// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     Constants used in various VirtualTree classes
    /// </summary>
    internal sealed class VirtualTreeConstant
    {
        private VirtualTreeConstant()
        {
        }

        /// <summary>
        ///     Constant used to represent any invalid index
        /// </summary>
        public const int NullIndex = -1;

        /// <summary>
        ///     The IBranch.GetObject and IBranch.LocateObject methods both
        ///     take an ObjectStyle parameter. There are a number of predefined
        ///     object styles that are recognized by the VirtualTreeGrid implementation.
        ///     However, the GetObject mechanism provides a natural entry point for
        ///     working with object styles not required by the core tree objects.
        ///     To enable the ObjectStyle enum to expand in future versions without
        ///     breaking code compiled against the original object styles, the user should
        ///     create readonly static ObjectStyle values that are greater than or equal to
        ///     VirtualTreeConstant.FirstUserObjectStyle ((ObjectStyle)(VirtualTreeConstant.FirstUserObjectStyle + 0),
        ///     (ObjectStyle)(VirtualTreeConstant.FirstUserObjectStyle + 1), etc).
        /// </summary>
        public static int FirstUserObjectStyle
        {
            get { return (int)ObjectStyle.SubItemExpansion + 1; }
        }

        /// <summary>
        ///     The first bit that can be used to store user flags in the BranchFeatures of
        ///     a given branch. User flags should be defined as static readonly BranchFeature
        ///     values shifted from this value.
        ///     public static readonly BranchFeatures CustomFeature1 = (BranchFeatures)(VirtualTreeConstant.FirstUserBranchFeature &lt;&lt; 0);
        ///     public static readonly BranchFeatures CustomFeature2 = (BranchFeatures)(VirtualTreeConstant.FirstUserBranchFeature &lt;&lt; 1);
        /// </summary>
        public static int FirstUserBranchFeature
        {
            get { return (int)(BranchFeatures.DisplayDataFixed) << 1; }
        }
    }

}
