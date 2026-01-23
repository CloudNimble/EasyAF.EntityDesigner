// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.Model
{
    using System.Xml;
    using Microsoft.Data.Entity.Design.VersioningFacade;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class MetadataConverterDriverTests
    {
        [TestMethod]
        public void Convert_returns_null_for_non_SqlCE()
        {
            var xmlDoc = new XmlDocument();

            foreach (var edmxNs in SchemaManager.GetEDMXNamespaceNames())
            {
                xmlDoc.LoadXml(string.Format("<Edmx xmlns=\"{0}\" />", edmxNs));
                MetadataConverterDriver.Instance.Convert(xmlDoc.Should().BeNull());
            }
        }

        [TestMethod]
        public void Convert_returns_converted_xml_for_SqlCE()
        {
            var xmlDoc = new XmlDocument();

            foreach (var edmxNs in SchemaManager.GetEDMXNamespaceNames())
            {
                xmlDoc.LoadXml(string.Format("<Edmx xmlns=\"{0}\" />", edmxNs));
                MetadataConverterDriver.SqlCeInstance.Convert(xmlDoc.Should().NotBeNull());
            }
        }
    }
}
