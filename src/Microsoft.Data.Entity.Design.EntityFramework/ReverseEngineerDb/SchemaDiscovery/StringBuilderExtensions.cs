// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;

namespace Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendIfNotEmpty(this StringBuilder input, string value)
        {
            Debug.Assert(input != null, "input != null");

            return
                input.Length > 0
                    ? input.Append(value)
                    : input;
        }
    }
}
