// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility
{
    /// <summary>
    ///     Parent interface used by the LayerManager to distinguish different layers.
    /// </summary>
    public interface IEntityDesignerLayerData
    {
        /// <summary>
        ///     The name of this extensibility layer.
        /// </summary>
        [DefaultValue(null)]
        string LayerName { get; }
    }
}
