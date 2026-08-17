// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Data.Entity.Design.VersioningFacade
{
    internal static class EntityFrameworkVersion
    {
        // Only Version3 (EF6) is supported
        public static readonly Version Version3 = new Version(3, 0, 0, 0);

        private static readonly HashSet<Version> AllVersionsSet =
        [
            Version3
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

        internal static Version DoubleToVersion(double version)
        {
            Version v = Version.Parse(version.ToString("F1", CultureInfo.InvariantCulture));
            return new Version(v.Major, v.Minor, 0, 0);
        }

        internal static double VersionToDouble(Version version)
        {
            Debug.Assert(IsValidVersion(version), "invalid EF version");

            return double.Parse(version.ToString(2), CultureInfo.InvariantCulture);
        }

        public static Version Latest
        {
            get { return Version3; }
        }
    }
}
