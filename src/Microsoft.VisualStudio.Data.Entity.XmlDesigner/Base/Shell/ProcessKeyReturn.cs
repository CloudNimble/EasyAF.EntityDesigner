// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell
{

    /// <summary>
    ///     Enum returned from TreeGridDesignerTreeControl.ProcessKeyDown and TreeGridDesignerTreeControl.ProcessKeyPress.
    /// </summary>
    internal enum ProcessKeyReturn
    {
        /// <summary>
        ///     Branch indicated it did not want to handle the key.
        /// </summary>
        NotHandled = 0,

        /// <summary>
        ///     Branch indicated it wanted the key, but no action occurred as a result of handling.
        /// </summary>
        KeyHandledNoAction = 1,

        /// <summary>
        ///     Branch indicated it wanted the key, and an action occurred as a result of handling.
        /// </summary>
        KeyHandledActionOccurred = 2
    }

}
