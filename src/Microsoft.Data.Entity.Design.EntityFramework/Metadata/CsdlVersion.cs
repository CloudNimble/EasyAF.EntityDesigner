// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.VersioningFacade.Metadata
{
    internal class CsdlVersion
    {
        // Kept for backward compatibility with existing EDMX files
        internal static readonly Version Version1 = new Version(1, 0, 0, 0);
        internal static readonly Version Version1_1 = new Version(1, 1, 0, 0);
        internal static readonly Version Version2 = new Version(2, 0, 0, 0);
        public static readonly Version Version3 = new Version(3, 0, 0, 0);

        private static readonly HashSet<Version> AllVersionsSet =
        [
            Version3, Version2, Version1, Version1_1
        ];

        public static IEnumerable<Version> GetAllVersions()
        {
            // Only return Version3 for new model creation
            yield return Version3;
        }

        public static bool IsValidVersion(Version version)
        {
            // Still validate old versions for backward compatibility with existing files
            return version != null && AllVersionsSet.Contains(version);
        }
    }
}
