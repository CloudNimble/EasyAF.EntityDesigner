// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Package;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide.Model
{
    internal class VSArtifactSet : EntityDesignArtifactSet
    {
        internal VSArtifactSet(EFArtifact artifact)
            : base(artifact)
        {
        }

        internal Project GetProjectForArtifactSet()
        {
            Project project = null;
            string documentPath = null;
            var artifact = this.GetEntityDesignArtifact();
            if (artifact != null)
            {
                documentPath = artifact.Uri.LocalPath;
                project = VSHelpers.GetProjectForDocument(documentPath, PackageManager.Package);
            }
            return project;
        }
    }
}
