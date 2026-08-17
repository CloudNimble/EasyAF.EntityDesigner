// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal static class FunctionImportNameNormalizer
    {
        internal static NormalizedName NameNormalizer(EFElement parent, string refName)
        {
            Debug.Assert(parent != null, "parent should not be null");

            if (refName == null)
            {
                return null;
            }

            var entityContainerName = string.Empty;

            Symbol symbol = null;

            // are we trying to normalize the name of actual FunctionImport in the EC?
            if (parent is FunctionImport parentFunctionImport)
            {
                if (parentFunctionImport.Parent is BaseEntityContainer ec)
                {
                    entityContainerName = ec.EntityContainerName;
                }
            }
            else if (parent is FunctionImportMapping parentFunctionImportMapping)
            {
                if (parentFunctionImportMapping.Parent is EntityContainerMapping ecm)
                {
                    entityContainerName = ecm.CdmEntityContainer.RefName;
                }
            }

            if (!string.IsNullOrEmpty(entityContainerName))
            {
                symbol = new Symbol(entityContainerName, refName);
            }

            symbol ??= new Symbol(refName);

            NormalizedName normalizedName = new NormalizedName(symbol, null, null, refName);
            return normalizedName;
        }
    }
}
