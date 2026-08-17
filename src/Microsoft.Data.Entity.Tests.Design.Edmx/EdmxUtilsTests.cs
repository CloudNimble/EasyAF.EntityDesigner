// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model
{
    [TestClass]
    public class EdmxUtilsTests
    {
        [TestMethod]
        public void GetEDMXXsdResource_returns_valid_xsd_for_Version3()
        {
            // Only Version3 is supported
            Version version = new Version(3, 0, 0, 0);
            var reader = EdmxUtils.GetEDMXXsdResource(version);

            reader.Should().NotBeNull();
            XDocument edmxXsd = XDocument.Load(reader);

            ((string)edmxXsd.Root.Attribute("targetNamespace")).Should().Be(
                SchemaManager.GetEDMXNamespaceName(version));
        }
    }
}
