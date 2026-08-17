// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Xml;
using Microsoft.Data.Entity.Design.VersioningFacade;

namespace Microsoft.Data.Entity.Design.Model
{
    internal static class EdmxUtils
    {
        public static XmlReader GetEDMXXsdResource(Version schemaVersion)
        {
            Debug.Assert(schemaVersion != null, "schemaVersion != null");
            Debug.Assert(schemaVersion == EntityFrameworkVersion.Version3, "Only Version3 is supported.");

            var assembly = typeof(EdmxUtils).Assembly;

            return
                XmlReader.Create(
                    assembly.GetManifestResourceStream("Microsoft.Data.Entity.Design.Model.Microsoft.Data.Entity.Design.Edmx_3.xsd"));
        }
    }
}
