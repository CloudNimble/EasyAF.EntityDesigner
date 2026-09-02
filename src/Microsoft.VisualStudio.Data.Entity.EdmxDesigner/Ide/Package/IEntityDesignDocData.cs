// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package
{
    internal interface IEntityDesignDocData
    {
        bool CreateAndLoadBuffer();
        string GetBufferTextForSaving();
        void EnableDiagramEdits(bool canEdit);
        void EnsureDiagramIsCreated(EFArtifact artifact);
        IVsHierarchy Hierarchy { get; }
        uint ItemId { get; }
        string BackupFileName { get; }
    }
}
