// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Data.Tools.XmlDesignerBase.Model;

namespace Microsoft.Data.Entity.Design.Model
{
    internal interface IEFArtifactFactory
    {
        IList<EFArtifact> Create(ModelManager modelManager, Uri uri, XmlModelProvider xmlModelProvider);
    }
}
