// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package
{

    internal sealed class ModelChangeEventArgs : EventArgs
    {
        internal string OldFileName { get; set; }

        internal string NewFileName { get; set; }

        internal uint DocCookie { get; set; }

        internal string OldEntityContainerName { get; set; }

        internal string NewEntityContainerName { get; set; }

        internal string OldMetadataArtifactProcessing { get; set; }

        internal bool IsCurrentlyBuilding { get; set; }

        internal EFArtifact Artifact { get; set; }

        internal Project ProjectObj { get; set; }
    }

}
