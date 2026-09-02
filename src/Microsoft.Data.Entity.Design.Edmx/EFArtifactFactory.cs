// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Edmx
{
    internal class EFArtifactFactory : IEFArtifactFactory
    {
        /// <summary>
        ///     Factory method for EntityDesignArtifact.
        ///     Note that this method will not create DiagramArtifact.
        ///     Please use VSArtifactFactory instead if DiagramArtifact needs to be created and loaded.
        /// </summary>
        public IList<EFArtifact> Create(ModelManager modelManager, Uri uri, XmlModelProvider xmlModelProvider)
        {
            return [new EntityDesignArtifact(modelManager, uri, xmlModelProvider)];
        }
    }
}
