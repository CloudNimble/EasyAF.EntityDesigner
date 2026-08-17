// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Model
{
    internal class VSArtifactSetFactory : IEFArtifactSetFactory
    {
        public EFArtifactSet CreateArtifactSet(EFArtifact artifact)
        {
            return new VSArtifactSet(artifact);
        }
    }
}
