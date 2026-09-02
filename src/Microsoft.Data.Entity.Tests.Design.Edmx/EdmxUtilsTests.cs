// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Tests.Design.Edmx
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
