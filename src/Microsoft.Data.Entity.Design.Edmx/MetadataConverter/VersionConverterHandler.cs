// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Xml;
using Microsoft.Data.Entity.Design.VersioningFacade;

namespace Microsoft.Data.Entity.Design.Model
{
    /// <summary>
    ///     Handles version conversion for EDMX files.
    ///     Only supports UPGRADE to Version3 (EF6).
    /// </summary>
    internal sealed class VersionConverterHandler : MetadataConverterHandler
    {
        private readonly Version _targetSchemaVersion;

        internal VersionConverterHandler(Version targetSchemaVersion)
        {
            Debug.Assert(
                targetSchemaVersion == EntityFrameworkVersion.Version3,
                "VersionConverterHandler only supports upgrade to Version3");
            _targetSchemaVersion = targetSchemaVersion;
        }

        /// <summary>
        ///     Update the EDMX version in the file to Version3 (EF6)
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected override XmlDocument DoHandleConversion(XmlDocument doc)
        {
            doc.DocumentElement.SetAttribute("Version", _targetSchemaVersion.ToString(2)); // only record Major.Minor version
            return doc;
        }
    }
}
