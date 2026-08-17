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
    public class SsdlProviderAttributesHandlerTests
    {
        private const string EdmxTemplate =
            "<Edmx xmlns=\"{0}\">" +
            "  <Runtime>" +
            "    <StorageModels>" +
            "      <Schema xmlns=\"{1}\" Provider=\"{2}\" ProviderManifestToken=\"{3}\" />" +
            "    </StorageModels>" +
            "  </Runtime>" +
            "</Edmx>";

        [TestMethod]
        public void SsdlProviderAttributesHandler_updates_provider_invariant_name_and_manifest_token_for_SqlCE()
        {
            // Only Version3 is supported
            Version schemaVersion = new Version(3, 0, 0, 0);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(
                string.Format(
                    EdmxTemplate,
                    SchemaManager.GetEDMXNamespaceName(schemaVersion),
                    SchemaManager.GetSSDLNamespaceName(schemaVersion),
                    "System.Data.SqlServerCe.3.5",
                    "3.5"));

            new SsdlProviderAttributesHandler(schemaVersion).HandleConversion(xmlDoc);

            xmlDoc.SelectSingleNode("//*[local-name() = 'Schema']/@Provider").Value.Should().Be(
                "System.Data.SqlServerCe.4.0");

            xmlDoc.SelectSingleNode("//*[local-name() = 'Schema']/@ProviderManifestToken").Value.Should().Be(
                "4.0");
        }

        [TestMethod]
        public void SsdlProviderAttributesHandler_does_not_modify_manifest_token_for_non_SqlCE()
        {
            // Only Version3 is supported
            Version schemaVersion = new Version(3, 0, 0, 0);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(
                string.Format(
                    EdmxTemplate,
                    SchemaManager.GetEDMXNamespaceName(schemaVersion),
                    SchemaManager.GetSSDLNamespaceName(schemaVersion),
                    "MyProvider",
                    "3.5"));

            new SsdlProviderAttributesHandler(schemaVersion).HandleConversion(xmlDoc);

            xmlDoc.SelectSingleNode("//*[local-name() = 'Schema']/@Provider").Value.Should().Be(
                "MyProvider");

            xmlDoc.SelectSingleNode("//*[local-name() = 'Schema']/@ProviderManifestToken").Value.Should().Be(
                "3.5");
        }

        [TestMethod]
        public void SsdlProviderAttributesHandler_does_not_modify_manifest_token_for_SqlCE_if_not_3_5()
        {
            // Only Version3 is supported
            Version schemaVersion = new Version(3, 0, 0, 0);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(
                string.Format(
                    EdmxTemplate,
                    SchemaManager.GetEDMXNamespaceName(schemaVersion),
                    SchemaManager.GetSSDLNamespaceName(schemaVersion),
                    "System.Data.SqlServerCe.3.5",
                    "17.0"));

            new SsdlProviderAttributesHandler(schemaVersion).HandleConversion(xmlDoc);

            xmlDoc.SelectSingleNode("//*[local-name() = 'Schema']/@Provider").Value.Should().Be(
                "System.Data.SqlServerCe.4.0");

            xmlDoc.SelectSingleNode("//*[local-name() = 'Schema']/@ProviderManifestToken").Value.Should().Be(
                "17.0");
        }
    }
}
