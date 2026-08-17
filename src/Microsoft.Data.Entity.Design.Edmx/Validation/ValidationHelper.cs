// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Model.Validation
{
    internal static class ValidationHelper
    {
        internal static bool IsStorageModelEmpty(EFArtifact artifact)
        {
            var result = false;

            var storageModel = artifact.StorageModel();
            if (storageModel != null)
            {
                if (storageModel.FirstEntityContainer is StorageEntityContainer container)
                {
                    var element = container.Children.OfType<EFElement>().FirstOrDefault<EFElement>();
                    if (element == null)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }
    }
}
