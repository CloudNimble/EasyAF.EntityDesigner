// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model
{
    [TestClass]
    public class NamespaceConverterHandlerTests
    {
        [TestMethod]
        public void NamespaceConverterHandlerTests_rewrites_document_to_use_namespaces_for_Version3()
        {
            // Only Version3 is supported
            Version schemaVersion = new Version(3, 0, 0, 0);
            var sourceEdmx = CreateSourceEdmx(schemaVersion);

            UsesNamespacesForTargetSchemaVersion(
                    new NamespaceConverterHandler(schemaVersion, schemaVersion).HandleConversion(sourceEdmx),
                    schemaVersion).Should().BeTrue();
        }

        private static XmlDocument CreateSourceEdmx(Version schemaVersion)
        {
            const string template =
                "<Edmx xmlns=\"{0}\">" +
                "  <Runtime>" +
                "    <StorageModels>" +
                "      <Schema xmlns=\"{1}\" />" +
                "    </StorageModels>" +
                "    <ConceptualModels>" +
                "      <Schema xmlns=\"{2}\" />" +
                "    </ConceptualModels>" +
                "    <Mappings>" +
                "      <Mapping xmlns=\"{3}\" />" +
                "    </Mappings>" +
                "  </Runtime>" +
                "  <Designer/>" +
                "</Edmx>";

            XmlDocument edmx = new XmlDocument();
            edmx.LoadXml(
                string.Format(
                    template,
                    SchemaManager.GetEDMXNamespaceName(schemaVersion),
                    SchemaManager.GetSSDLNamespaceName(schemaVersion),
                    SchemaManager.GetCSDLNamespaceName(schemaVersion),
                    SchemaManager.GetMSLNamespaceName(schemaVersion)));

            return edmx;
        }

        private static bool UsesNamespacesForTargetSchemaVersion(XmlDocument edmx, Version targetSchemaVersion)
        {
            var nsMgr = SchemaManager.GetEdmxNamespaceManager(edmx.NameTable, targetSchemaVersion);

            return
                edmx.SelectSingleNode("/edmx:Edmx/edmx:Runtime/edmx:StorageModels/ssdl:Schema", nsMgr) != null &&
                edmx.SelectSingleNode("/edmx:Edmx/edmx:Runtime/edmx:ConceptualModels/csdl:Schema", nsMgr) != null &&
                edmx.SelectSingleNode("/edmx:Edmx/edmx:Runtime/edmx:Mappings/msl:Mapping", nsMgr) != null &&
                edmx.SelectSingleNode("/edmx:Edmx/edmx:Designer", nsMgr) != null;
        }
    }
}
