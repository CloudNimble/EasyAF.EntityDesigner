// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.UI;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell
{

    /// <summary>
    ///     Represents a tri-state checkbox
    /// </summary>
    internal enum CheckBoxState
    {
        Unsupported = -1,
        // values below correspond to indexes into the state image list
        Checked = StandardCheckBoxImage.Checked,
        Unchecked = StandardCheckBoxImage.Unchecked,
        Indeterminate = StandardCheckBoxImage.Indeterminate,
        Inactive = StandardCheckBoxImage.Inactive,
        CheckedDisabled = StandardCheckBoxImage.CheckedDisabled,
        UncheckedDisabled = StandardCheckBoxImage.UncheckedDisabled
    }

}
