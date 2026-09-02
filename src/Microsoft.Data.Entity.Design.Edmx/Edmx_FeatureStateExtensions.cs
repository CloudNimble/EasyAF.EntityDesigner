// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Edmx
{

    /// <summary>
    ///     These extension methods allow the simplicity of setting enum values as states and the simplicity
    ///     of checking mutually exclusive states (enabled/invisible) on clients.
    /// </summary>
    internal static class Edmx_FeatureStateExtensions
    {
        internal static bool IsEnabled(this FeatureState state)
        {
            return state == FeatureState.VisibleAndEnabled;
        }

        internal static bool IsVisible(this FeatureState state)
        {
            return state != FeatureState.Invisible;
        }
    }

}
