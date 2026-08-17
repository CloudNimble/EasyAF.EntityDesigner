// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.UI
{

    /// <summary>
    ///     Keeps track of state of current iteration being processed, the total number of iterations to be processed
    ///     and the current status message to be displayed.
    /// </summary>
    internal struct ProgressDialogUserState
    {
        internal int NumberIterations;
        internal int CurrentIteration;
        internal string CurrentStatusMessage;
        internal bool IsError;
    }

}
