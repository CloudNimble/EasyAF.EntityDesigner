// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Data.Entity.Design.VersioningFacade.Metadata
{
    internal static class EdmTypeExtensions
    {
        public static DataSpace GetDataSpace(this EdmType edmType)
        {
            Debug.Assert(edmType != null, "edmType != null");

            return
                (DataSpace)typeof(EdmType)
                               .GetProperty("DataSpace", BindingFlags.Instance | BindingFlags.NonPublic)
                               .GetValue(edmType);
        }
    }
}
