// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityFramework;
using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine
{
    internal class InitialModelContentsFactory : IInitialModelContentsFactory
    {
        public string GetInitialModelContents(Version targetSchemaVersion)
        {
            Debug.Assert(
                EntityFrameworkVersion.IsValidVersion(targetSchemaVersion),
                "invalid schema version");

            return EdmUtils.CreateEdmxString(targetSchemaVersion, string.Empty, string.Empty, string.Empty);
        }
    }
}
