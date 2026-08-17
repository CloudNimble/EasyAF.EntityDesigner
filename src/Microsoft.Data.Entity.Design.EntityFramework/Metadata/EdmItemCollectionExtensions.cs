// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Metadata.Edm;

namespace Microsoft.Data.Entity.Design.VersioningFacade.Metadata
{
    internal static class EdmItemCollectionExtensions
    {
        public static Version CsdlVersion(this EdmItemCollection edmItemCollection)
        {
            return EntityFrameworkVersion.DoubleToVersion(edmItemCollection.EdmVersion);
        }
    }
}
