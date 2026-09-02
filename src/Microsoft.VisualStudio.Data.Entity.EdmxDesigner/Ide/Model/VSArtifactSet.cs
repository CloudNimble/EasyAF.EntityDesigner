// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Model
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
