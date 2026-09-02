// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider
{

    /// <summary>
    ///     The result of a label edit action
    /// </summary>
    internal enum LabelEditResult
    {
        /// <summary>
        ///     The edit is acknowledged and new text retrieved will reflect this value
        /// </summary>
        AcceptEdit = 1,

        /// <summary>
        ///     The edit should be canceled
        /// </summary>
        CancelEdit = 2,

        /// <summary>
        ///     Block deactivation of the edit (NYI, defers to CancelEdit)
        /// </summary>
        BlockDeactivate = 3
    };

}
