// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Extensibility;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility
{
    internal class LayerState
    {
        internal bool IsEnabled { get; set; }
        internal EntityDesignerCommand EnableCommand { get; set; }
    }
}
