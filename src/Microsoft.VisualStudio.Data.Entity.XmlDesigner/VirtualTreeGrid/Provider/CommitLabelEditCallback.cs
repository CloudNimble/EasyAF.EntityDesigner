// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     A callback delegate to enable a richer label edit commit experience than IBranch.CommitLabelEdit.
    ///     The callback receives information about the item that was commit, and the instance of the control
    ///     used to commit the edit.
    /// </summary>
    internal delegate LabelEditResult CommitLabelEditCallback(VirtualTreeItemInfo itemInfo, Control editControl);

}
