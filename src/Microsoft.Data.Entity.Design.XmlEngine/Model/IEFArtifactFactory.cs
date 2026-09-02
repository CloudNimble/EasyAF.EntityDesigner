// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{
    internal interface IEFArtifactFactory
    {
        IList<EFArtifact> Create(ModelManager modelManager, Uri uri, XmlModelProvider xmlModelProvider);
    }
}
