// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Model.Designer;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.Views.Explorer
{
    internal interface IDiagramManager
    {
        void OpenDiagram(string diagramMoniker, bool openInNewTab);
        void CloseDiagram(string diagramMoniker);
        void CloseAllDiagrams();
        IViewDiagram ActiveDiagram { get; }
        IViewDiagram FirstOpenDiagram { get; }
        IEnumerable<IViewDiagram> OpenDiagrams { get; }
    }
}
