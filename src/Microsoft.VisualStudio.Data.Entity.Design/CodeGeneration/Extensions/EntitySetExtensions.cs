// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.VersioningFacade;
using System.Data.Entity.Core.Metadata.Edm;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Extensions
{
    internal static class EntitySetExtensions
    {
        public static string GetStoreModelBuilderMetadataProperty(this EntitySet entitySet, string name)
        {
            if (!entitySet.MetadataProperties.TryGetValue(
                SchemaManager.EntityStoreSchemaGeneratorNamespace + ":" + name,
                false,
                out MetadataProperty metadataProperty))
            {
                return null;
            }

            return metadataProperty.Value as string;
        }
    }
}
