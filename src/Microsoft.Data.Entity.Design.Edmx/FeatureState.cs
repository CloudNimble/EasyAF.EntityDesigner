// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Edmx
{
    /// <summary>
    ///     Used directly by the EdmFeatureManager to test whether a feature
    ///     is supported for a given runtime and indirectly by the BehaviorService to test whether
    ///     a feature is supported for a given workflow.
    /// </summary>
    internal enum FeatureState
    {
        /// <summary>
        ///     A feature is fully visible and enabled
        /// </summary>
        VisibleAndEnabled,

        /// <summary>
        ///     A feature is fully visible but not enabled for user interaction.
        ///     UIs that understand this value should display tooltips, messages to the user.
        /// </summary>
        VisibleButDisabled,

        /// <summary>
        ///     A feature is neither visible nor enabled for user interaction.
        /// </summary>
        Invisible
    }
}
