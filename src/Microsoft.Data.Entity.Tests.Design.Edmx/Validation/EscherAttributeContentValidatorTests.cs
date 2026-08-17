// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model.Validation
{
    [TestClass]
    public class EscherAttributeContentValidatorTests
    {
        [TestMethod]
        public void IsValidAttributeValue_returns_false_if_the_value_contains_invalid_xml_characters()
        {
            EscherAttributeContentValidator.IsValidXmlAttributeValue("\u0000").Should().BeFalse();
        }

        [TestMethod]
        public void IsValidAttributeValue_returns_true_if_the_value_contains_only_valid_xml_characters()
        {
            EscherAttributeContentValidator.IsValidXmlAttributeValue("<>&AAA").Should().BeTrue();
        }

        [TestMethod]
        public void IsValidCsdlNamespaceName_returns_true_for_valid_Csdl_namespace()
        {
            EscherAttributeContentValidator.IsValidCsdlNamespaceName("Model1.Namespace.Edm").Should().BeTrue();
            EscherAttributeContentValidator.IsValidCsdlNamespaceName("Model1NamespaceEdm").Should().BeTrue();
            EscherAttributeContentValidator.IsValidCsdlNamespaceName(new string('a', 512)).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidCsdlNamespaceName_returns_false_for_invalid_Csdl_namespace()
        {
            EscherAttributeContentValidator.IsValidCsdlNamespaceName(new string('a', 513)).Should().BeFalse();
            EscherAttributeContentValidator.IsValidCsdlNamespaceName("Name\u0000space").Should().BeFalse();
            EscherAttributeContentValidator.IsValidCsdlNamespaceName("").Should().BeFalse();
            EscherAttributeContentValidator.IsValidCsdlNamespaceName(".Namespace").Should().BeFalse();
            EscherAttributeContentValidator.IsValidCsdlNamespaceName("Namespace.").Should().BeFalse();
        }

        [TestMethod]
        public void IsValidCsdlEntityContainerName_returns_true_for_valid_container_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlEntityContainerName);
        }

        [TestMethod]
        public void IsValidCsdlEntityContainerName_returns_false_for_invalid_container_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlEntityContainerName);
        }

        [TestMethod]
        public void IsValidCsdlEntitySetName_returns_true_for_valid_entityset_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlEntitySetName);
        }

        [TestMethod]
        public void IsValidCsdlEntitySetName_returns_false_for_invalid_entityset_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlEntitySetName);
        }

        [TestMethod]
        public void IsValidCsdlEntityTypeName_returns_true_for_valid_entity_type_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlEntityTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEntityTypeName_returns_false_for_invalid_entity_type_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlEntityTypeName);
        }

        [TestMethod]
        public void IsValidCsdlComplexTypeName_returns_true_for_valid_complex_type_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlComplexTypeName);
        }

        [TestMethod]
        public void IsValidCsdlComplexTypeName_returns_false_for_invalid_complex_type_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlComplexTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEnumTypeName_returns_true_for_valid_enum_type_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlEnumTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEnumTypeName_returns_false_for_invalid_enum_type_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlEnumTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEnumMemberName_returns_true_for_valid_enum_member_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlEnumMemberName);
        }

        [TestMethod]
        public void IsValidCsdlEnumMemberName_returns_false_for_invalid_enum_member_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlEnumMemberName);
        }

        [TestMethod]
        public void IsValidCsdlPropertyName_returns_true_for_valid_property_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlPropertyName_returns_false_for_invalid_property_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlNavigationPropertyName_returns_true_for_valid_navigation_property_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlNavigationPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlNavigationPropertyName_returns_false_for_invalid_navigation_property_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlNavigationPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlAssociationName_returns_true_for_valid_association_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlAssociationName);
        }

        [TestMethod]
        public void IsValidCsdlAssociationName_returns_false_for_invalid_association_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlAssociationName);
        }

        [TestMethod]
        public void IsValidCsdlFunctionImportName_returns_true_for_valid_function_import_name()
        {
            NameVerificationReturnsTrueForFunction(
                EscherAttributeContentValidator.IsValidCsdlFunctionImportName);
        }

        [TestMethod]
        public void IsValidCsdlFunctionImportName_returns_false_for_invalid_function_import_name()
        {
            NameVerificationReturnsFalseForFunction(
                EscherAttributeContentValidator.IsValidCsdlFunctionImportName);
        }

        private static void NameVerificationReturnsTrueForFunction(Func<string, bool> nameVerificationFunc)
        {
            nameVerificationFunc(new string('c', 480)).Should().BeTrue();
        }

        private static void NameVerificationReturnsFalseForFunction(Func<string, bool> nameVerificationFunc)
        {
            nameVerificationFunc(string.Empty).Should().BeFalse();
            nameVerificationFunc(new string('c', 481)).Should().BeFalse();
            nameVerificationFunc("na\0000me").Should().BeFalse();
            nameVerificationFunc(".name").Should().BeFalse();
            nameVerificationFunc("na.me").Should().BeFalse();
        }
    }
}
