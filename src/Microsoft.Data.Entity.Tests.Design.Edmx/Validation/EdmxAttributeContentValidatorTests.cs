// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Data.Entity.Tests.Design.Edmx.Validation
{
    [TestClass]
    public class EdmxAttributeContentValidatorTests
    {
        [TestMethod]
        public void IsValidAttributeValue_returns_false_if_the_value_contains_invalid_xml_characters()
        {
            EdmxAttributeContentValidator.IsValidXmlAttributeValue("\u0000").Should().BeFalse();
        }

        [TestMethod]
        public void IsValidAttributeValue_returns_true_if_the_value_contains_only_valid_xml_characters()
        {
            EdmxAttributeContentValidator.IsValidXmlAttributeValue("<>&AAA").Should().BeTrue();
        }

        [TestMethod]
        public void IsValidCsdlNamespaceName_returns_true_for_valid_Csdl_namespace()
        {
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName("Model1.Namespace.Edm").Should().BeTrue();
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName("Model1NamespaceEdm").Should().BeTrue();
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName(new string('a', 512)).Should().BeTrue();
        }

        [TestMethod]
        public void IsValidCsdlNamespaceName_returns_false_for_invalid_Csdl_namespace()
        {
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName(new string('a', 513)).Should().BeFalse();
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName("Name\u0000space").Should().BeFalse();
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName("").Should().BeFalse();
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName(".Namespace").Should().BeFalse();
            EdmxAttributeContentValidator.IsValidCsdlNamespaceName("Namespace.").Should().BeFalse();
        }

        [TestMethod]
        public void IsValidCsdlEntityContainerName_returns_true_for_valid_container_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEntityContainerName);
        }

        [TestMethod]
        public void IsValidCsdlEntityContainerName_returns_false_for_invalid_container_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEntityContainerName);
        }

        [TestMethod]
        public void IsValidCsdlEntitySetName_returns_true_for_valid_entityset_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEntitySetName);
        }

        [TestMethod]
        public void IsValidCsdlEntitySetName_returns_false_for_invalid_entityset_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEntitySetName);
        }

        [TestMethod]
        public void IsValidCsdlEntityTypeName_returns_true_for_valid_entity_type_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEntityTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEntityTypeName_returns_false_for_invalid_entity_type_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEntityTypeName);
        }

        [TestMethod]
        public void IsValidCsdlComplexTypeName_returns_true_for_valid_complex_type_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlComplexTypeName);
        }

        [TestMethod]
        public void IsValidCsdlComplexTypeName_returns_false_for_invalid_complex_type_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlComplexTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEnumTypeName_returns_true_for_valid_enum_type_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEnumTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEnumTypeName_returns_false_for_invalid_enum_type_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEnumTypeName);
        }

        [TestMethod]
        public void IsValidCsdlEnumMemberName_returns_true_for_valid_enum_member_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEnumMemberName);
        }

        [TestMethod]
        public void IsValidCsdlEnumMemberName_returns_false_for_invalid_enum_member_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlEnumMemberName);
        }

        [TestMethod]
        public void IsValidCsdlPropertyName_returns_true_for_valid_property_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlPropertyName_returns_false_for_invalid_property_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlNavigationPropertyName_returns_true_for_valid_navigation_property_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlNavigationPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlNavigationPropertyName_returns_false_for_invalid_navigation_property_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlNavigationPropertyName);
        }

        [TestMethod]
        public void IsValidCsdlAssociationName_returns_true_for_valid_association_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlAssociationName);
        }

        [TestMethod]
        public void IsValidCsdlAssociationName_returns_false_for_invalid_association_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlAssociationName);
        }

        [TestMethod]
        public void IsValidCsdlFunctionImportName_returns_true_for_valid_function_import_name()
        {
            NameVerificationReturnsTrueForFunction(
                EdmxAttributeContentValidator.IsValidCsdlFunctionImportName);
        }

        [TestMethod]
        public void IsValidCsdlFunctionImportName_returns_false_for_invalid_function_import_name()
        {
            NameVerificationReturnsFalseForFunction(
                EdmxAttributeContentValidator.IsValidCsdlFunctionImportName);
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
