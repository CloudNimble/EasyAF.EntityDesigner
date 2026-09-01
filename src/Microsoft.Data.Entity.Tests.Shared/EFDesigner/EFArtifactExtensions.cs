// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Tests.Shared.EFDesigner
{
    internal static class EFArtifactExtensions
    {
        public static EditingContext GetEditingContext(this EFArtifact artifact)
        {
            Debug.Assert(artifact != null, "artifact != null");

            EFArtifactService service = new EFArtifactService(artifact);
            EditingContext editingContext = new EditingContext();
            editingContext.SetEFArtifactService(service);
            return editingContext;
        }

        public static string LocalPath(this EFArtifact artifact)
        {
            Debug.Assert(artifact != null, "artifact != null");

            return artifact.Uri.LocalPath;
        }
    }
}
