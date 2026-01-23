// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.Model.Entity
{
    using System.Linq;
    using System.Xml.Linq;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

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
            CreateProperties(.Should().BeNull()[0].PreviousSiblingInPropertyXElementOrder);
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
            CreateProperties(.Should().BeNull()[1].NextSiblingInPropertyXElementOrder);
        }

        private static PropertyBase[] CreateProperties()
        {
            var complexType = XElement.Parse(
                "<ComplexType Name=\"Category\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\">" +
                "  <Property Name=\"CategoryID\" Type=\"Int32\" Nullable=\"false\" />" +
                "  <Property Name=\"Description\" Type=\"String\" MaxLength=\"4000\" FixedLength=\"false\" Unicode=\"true\" />" +
                "</ComplexType>");

            var mockCategoryId = new Mock<PropertyBase>(null, complexType.Elements().First(), null);
            mockCategoryId.Setup(m => m.EFTypeName).Returns("Property");

            var mockDescription = new Mock<PropertyBase>(null, complexType.Elements().Last(), null);
            mockDescription.Setup(m => m.EFTypeName).Returns("Property");

            return new[] { mockCategoryId.Object, mockDescription.Object };
        }
    }
}
