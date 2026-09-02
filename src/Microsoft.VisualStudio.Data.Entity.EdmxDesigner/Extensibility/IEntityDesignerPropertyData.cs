// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Extensibility;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility
{
    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public interface IEntityDesignerPropertyData : IEntityDesignerLayerData
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        EntityDesignerSelection EntityDesignerSelection { get; }
    }
}
