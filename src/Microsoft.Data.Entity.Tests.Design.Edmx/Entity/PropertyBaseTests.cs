// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Model.Entity;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model.Entity
{
    [TestClass]
    public class PropertyBaseTests
    {
        [TestMethod]
        public void PreviousSiblingInPropertyXElementOrder_returns_previous_property_if_exists()
        {
            var properties = CreateProperties();
            properties[1].PreviousSiblingInPropertyXElementOrder.Should().BeSameAs(properties[0]);
        }

        [TestMethod]
        public void PreviousSiblingInPropertyXElementOrder_returns_null_if_previous_property_does_not_exist()
        {
            var properties = CreateProperties();
            properties[0].PreviousSiblingInPropertyXElementOrder.Should().BeNull();
        }

        [TestMethod]
        public void NextSiblingInPropertyXElementOrder_returns_next_property_if_exists()
        {
            var properties = CreateProperties();
            properties[0].NextSiblingInPropertyXElementOrder.Should().BeSameAs(properties[1]);
        }

        [TestMethod]
        public void NextSiblingInPropertyXElementOrder_returns_null_if_next_property_does_not_exist()
        {
            var properties = CreateProperties();
            properties[1].NextSiblingInPropertyXElementOrder.Should().BeNull();
        }

        private static PropertyBase[] CreateProperties()
        {
            XElement complexType = XElement.Parse(
                "<ComplexType Name=\"Category\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\">" +
                "  <Property Name=\"CategoryID\" Type=\"Int32\" Nullable=\"false\" />" +
                "  <Property Name=\"Description\" Type=\"String\" MaxLength=\"4000\" FixedLength=\"false\" Unicode=\"true\" />" +
                "</ComplexType>");

            Mock<PropertyBase> mockCategoryId = new Mock<PropertyBase>(null, complexType.Elements().First(), null);
            mockCategoryId.Setup(m => m.EFTypeName).Returns("Property");

            Mock<PropertyBase> mockDescription = new Mock<PropertyBase>(null, complexType.Elements().Last(), null);
            mockDescription.Setup(m => m.EFTypeName).Returns("Property");

            return new[] { mockCategoryId.Object, mockDescription.Object };
        }
    }
}
