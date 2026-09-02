// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx.MetadataConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml;

namespace Microsoft.Data.Entity.Tests.Design.Edmx.MetadataConverter
{
    [TestClass]
    public class VersionConverterHandlerTests
    {
        [TestMethod]
        public void VersionConverterHandler_sets_correct_Version_attribute()
        {
            for (var i = 1; i <= 3; i++)
            {
                Version schemaVersion = new Version(i, 0, 0, 0);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml("<root />");

                new VersionConverterHandler(schemaVersion).HandleConversion(xmlDoc);

                xmlDoc.DocumentElement.Attributes["Version"].Value.Should().Be(schemaVersion.ToString(2));
            }
        }

        [TestMethod]
        public void VersionConverterHandler_overrides_existing_Version_attribute()
        {
            for (var i = 1; i <= 3; i++)
            {
                Version schemaVersion = new Version(i, 0, 0, 0);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml("<root Version=\"X.Y\"/>");

                new VersionConverterHandler(schemaVersion).HandleConversion(xmlDoc);

                xmlDoc.DocumentElement.Attributes["Version"].Value.Should().Be(schemaVersion.ToString(2));
            }
        }
    }
}
