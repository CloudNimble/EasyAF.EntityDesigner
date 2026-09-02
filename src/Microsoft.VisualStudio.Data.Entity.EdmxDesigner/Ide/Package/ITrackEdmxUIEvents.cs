// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package
{

    internal interface ITrackEdmxUIEvents
    {
        // if we have no App.Config/Web.Config for the edmx file then we have to create it
        int OnBeforeGenerateDDL(Project project, EFArtifact artifact);
        // if we don't have an App.Config/Web.Config for the edmx file then we have to create it (this also gets raised during build)
        int OnBeforeValidateModel(Project project, EFArtifact artifact, bool onBuild);
    }

}
