// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide
{
    // <summary>
    //     This is the error task we use for open documents.  The DocumentTask will keep text ranges up to date when the buffer changes.
    // </summary>
    internal class EFModelDocumentTask : XmlModelDocumentTask
    {
        internal EFModelDocumentTask(
            IServiceProvider site, IVsTextLines buffer, MARKERTYPE markerType, TextSpan span, string document, uint itemID,
            string errorMessage, IVsHierarchy hierarchy)
            : base(site, buffer, markerType, span, document, itemID, errorMessage, hierarchy)
        {
        }

        protected override void OnNavigate(EventArgs e)
        {
            EFModelErrorTaskNavigator.NavigateTo(this, e);
        }
    }
}
