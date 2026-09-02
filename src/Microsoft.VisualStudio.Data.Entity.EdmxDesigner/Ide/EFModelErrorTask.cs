// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide
{
    // <summary>
    //     This is the error task we use when a document is not opened.
    // </summary>
    internal class EFModelErrorTask : XmlModelErrorTask
    {
        internal EFModelErrorTask(
            string document, string errorMessage, int lineNumber, int columnNumber, TaskErrorCategory category, IVsHierarchy hierarchy,
            uint itemID)
            : base(document, errorMessage, lineNumber, columnNumber, category, hierarchy, itemID)
        {
            Navigate += EFModelErrorTaskNavigator.NavigateTo;
        }
    }
}
