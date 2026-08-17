// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide.ModelWizard.Engine
{
    internal interface IInitialModelContentsFactory
    {
        string GetInitialModelContents(Version targetSchemaVersion);
    }
}
