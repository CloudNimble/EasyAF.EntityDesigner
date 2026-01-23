// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class ConfigurationFileSchemaTests
    {
        private static readonly XmlSchema ConfigurationFileSchema;

        // this schema enables testing types separately by allowing elements 
        // of types defined in the "main" schema to be top level
        private static readonly XmlSchema HelperSchema;

        // this schema enables testing extensions from 
        // http://schemas.microsoft.com/XML-Document-Transform namespace
        private static readonly XmlSchema FakeXmlDocumentTransformationSchema;

        private const string EntityFrameworkConfigSchemaName = "EntityFrameworkConfig_6_1_4.xsd";
        private const string EntityFrameworkCatalogFileName = "EntityFrameworkCatalog.xml";

        static ConfigurationFileSchemaTests()
        {
            const string helperSchemaString =
                @"<?xml version=""1.0"" encoding=""utf-8""?>" +
                @"<xs:schema attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">"
                +
                @"    <xs:element name=""providers"" type=""ProviderList_Type"" />" +
                @"    <xs:element name=""provider"" type=""Provider_Type"" />" +
                @"    <xs:element name=""contexts"" type=""ContextList_Type"" />" +
                @"    <xs:element name=""context"" type=""Context_Type"" />" +
                @"    <xs:element name=""interceptors"" type=""InterceptorList_Type"" />" +
                @"    <xs:element name=""interceptor"" type=""ElementWithTypeAndParameters_Type"" />" +
                @"    <xs:element name=""parameters"" type=""ParameterList_Type"" />" +
                @"    <xs:element name=""parameter"" type=""Parameter_Type"" />" +
                @"    <xs:element name=""elementWithTypeAndParameters"" type=""ElementWithTypeAndParameters_Type"" />" +
                @"</xs:schema>";

            const string fakeXmlDocumentTransformSchemaString =
                @"<?xml version=""1.0"" encoding=""utf-8""?>" +
                @"<xs:schema targetNamespace=""http://schemas.microsoft.com/XML-Document-Transform"" " +
                @"      elementFormDefault=""qualified"" attributeFormDefault=""unqualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">"
                +
                @"    <xs:attribute name=""Transform"" type=""xs:string"" />" +
                @"</xs:schema>";

            using (var reader = XmlReader.Create(EntityFrameworkConfigSchemaName))
            {
                ConfigurationFileSchema = XmlSchema.Read(reader, null);
            }

            HelperSchema = XmlSchema.Read(XmlReader.Create(new StringReader(helperSchemaString)), null);
            FakeXmlDocumentTransformationSchema = XmlSchema.Read(
                XmlReader.Create(new StringReader(fakeXmlDocumentTransformSchemaString)), null);
        }

        #region entityFramework configuration element

        [TestMethod]
        public void Schema_rejects_configuration_whose_root_element_is_not_entityFramework()
        {
            var invalidTopLevelElements = new[]
                {
                    // dummy element
                    "dummy",
                    // make sure we don't allow elements that 
                    // should be nested at the top level
                    "defaultConnectionFactory",
                    "context",
                    "contexts",
                    "databaseInitializer",
                    "parameters",
                    "parameter"
                };

            foreach (var topLevelElement in invalidTopLevelElements)
            {
                var validationEvent =
                    ValidateWithExpectedValidationEvents(
                        string.Format("<{0} />", topLevelElement),
                        allowAllTypesAtTopLevel: false)
                        .Single();

                validationEvent.Severity.Should().Be(XmlSeverityType.Error);
                validationEvent.Message.Should().Contain(topLevelElement);
            }
        }

        [TestMethod]
        public void Schema_rejects_entityFramework_configuration_section_that_is_not_in_empty_namespace()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<entityFramework xmlns='foo' />", allowAllTypesAtTopLevel: false)
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Warning);
            validationEvent.Message.Should().Contain("foo:entityFramework");
        }

        [TestMethod]
        public void Minimal_entityFramework_configuration_accepted()
        {
            Validate("<entityFramework />", allowAllTypesAtTopLevel: false);
        }

        [TestMethod]
        public void Schema_accepts_entityframework_element_with_non_empty_codeConfigurationType_atribute()
        {
            Validate("<entityFramework codeConfigurationType='MyConfig'/>", allowAllTypesAtTopLevel: false);
        }

        [TestMethod]
        public void Schema_rejects_entityframework_element_with_empty_codeConfigurationType_atribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents(
                    "<entityFramework codeConfigurationType=''/>",
                    allowAllTypesAtTopLevel: false)
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("codeConfigurationType");
        }

        [TestMethod]
        public void Schema_accepts_entityFramework_element_with_xdt_attributes()
        {
            Validate(@"<entityFramework 
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_rejects_entityFramework_configuration_with_unknown_child_elements()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents(
                    "<entityFramework><dummy /></entityFramework>",
                    allowAllTypesAtTopLevel: false)
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        [TestMethod]
        public void Schema_accepts_elements_under_entityFrameworkConfiguration_element_in_any_order()
        {
            Validate("<entityFramework><defaultConnectionFactory type='MyContext' /><contexts /><providers /></entityFramework>");
            Validate("<entityFramework><defaultConnectionFactory type='MyContext' /><providers /><contexts /><interceptors /></entityFramework>");

            Validate("<entityFramework><contexts /><providers /><defaultConnectionFactory type='MyContext' /></entityFramework>");
            Validate("<entityFramework><contexts /><interceptors /><defaultConnectionFactory type='MyContext' /><providers /></entityFramework>");

            Validate("<entityFramework><providers /><defaultConnectionFactory type='MyContext' /><contexts /></entityFramework>");
            Validate("<entityFramework><interceptors /><providers /><contexts /><defaultConnectionFactory type='MyContext' /></entityFramework>");
        }

        #endregion

        #region defaultConnectionFactory, databaseInitializer and interceptor elements

        // note defaultConnectionFactory and databaseInitializer are instances
        // of the same type so we need just to test an instance of this type.

        [TestMethod]
        public void Schema_accepts_empty_elementWithTypeAndParameters_element()
        {
            Validate("<elementWithTypeAndParameters type='ContextType' />");
        }

        [TestMethod]
        public void Schema_rejects_elementWithTypeAndParameters_element_without_type_attribute_element()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<elementWithTypeAndParameters />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        [TestMethod]
        public void Schema_rejects_elementWithTypeAndParameters_element_with_empty_type_attribute_element()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<elementWithTypeAndParameters type='' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        [TestMethod]
        public void Schema_rejects_elementWithTypeAndParameters_element_with_unrecognized_attributes()
        {
            var validationEvents = ValidateWithExpectedValidationEvents(
                "<elementWithTypeAndParameters type='MyContext' dummy='a' ns:dummy='2' xmlns:ns='dummy-ns-uri' />");

            validationEvents.Count.Should().Be(2);
            validationEvents[0].Severity.Should().Be(XmlSeverityType.Error);
            validationEvents[0].Message.Should().Contain("dummy");
            validationEvents[1].Severity.Should().Be(XmlSeverityType.Error);
            validationEvents[1].Message.Should().Contain("dummy-ns-uri:dummy");
        }

        [TestMethod]
        public void Schema_accepts_elementWithTypeAndParameters_element_with_xdt_attributes()
        {
            Validate(@"<elementWithTypeAndParameters 
                            type='MyContext' 
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_accepts_elementWithTypeAndParameters_element_with_parameters_child_element()
        {
            Validate("<elementWithTypeAndParameters type='ContextType'><parameters /></elementWithTypeAndParameters>");
        }

        [TestMethod]
        public void Schema_rejects_elementWithTypeAndParameters_with_multiple_parameters_element()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents(
                    "<elementWithTypeAndParameters type='ContextType'><parameters /><parameters /></elementWithTypeAndParameters>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("parameters");
        }

        [TestMethod]
        public void Schema_rejects_elementWithTypeAndParameters_child_elements_that_are_not_parameters_element()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents(
                    "<elementWithTypeAndParameters type='ContextType'><dummy /></elementWithTypeAndParameters>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        #endregion

        #region providers element

        public void Schema_accepts_empty_providers_element()
        {
            Validate("<providers />");
        }

        [TestMethod]
        public void Schema_accepts_providers_element_with_xdt_attributes()
        {
            Validate(@"<providers 
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_accepts_providers_element_with_many_provider_elements()
        {
            Validate("<providers><provider invariantName='a' type='b' /><provider invariantName='1' type='2' /></providers>");
        }

        [TestMethod]
        public void Schema_rejects_providers_element_child_elements_that_are_not_provider_elements()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<providers><dummy /></providers>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        #endregion

        #region provider element

        [TestMethod]
        public void Schema_accepts_valid_provider_element_without_invariantName_attribute()
        {
            Validate("<provider invariantName='My.Provider' type='MyProvider' />");
        }

        [TestMethod]
        public void Schema_accepts_provider_element_with_xdt_attributes()
        {
            Validate(@"<provider invariantName='My.Provider' type='MyProvider'  
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_rejects_provider_element_without_invariantName_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<provider type='MyProvider' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("invariantName");
        }

        [TestMethod]
        public void Schema_rejects_provider_element_without_type_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<provider invariantName='My.Provider' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        [TestMethod]
        public void Schema_rejects_provider_element_with_empty_type_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<provider type='' invariantName='My.Provider' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        [TestMethod]
        public void Schema_rejects_provider_element_with_child_elements()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<provider type='MyProvider' invariantName='My.Provider'><dummy /></provider>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        [TestMethod]
        public void Schema_rejects_provider_element_with_empty_invariantName_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<provider type='MyProvider' invariantName='' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("invariantName");
        }

        #endregion

        #region contexts element

        [TestMethod]
        public void Schema_accepts_empty_contexts_element()
        {
            Validate("<contexts />");
        }

        [TestMethod]
        public void Schema_accepts_contexts_element_with_xdt_attributes()
        {
            Validate(@"<contexts 
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_accepts_contexts_element_with_many_context_child_elements()
        {
            Validate("<contexts><context type='Context1' /><context type='Context1' /></contexts>");
        }

        [TestMethod]
        public void Schema_rejects_contexts_element_child_elements_that_are_not_context_elements()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<contexts><dummy /></contexts>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        #endregion

        #region context element

        [TestMethod]
        public void Schema_accepts_empty_context_element()
        {
            Validate("<context type='MyContext' />");
        }

        [TestMethod]
        public void Schema_accepts_context_element_with_xdt_attributes()
        {
            Validate(@"<context type='MyContext'
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_accepts_context_element_with_disableDatabaseInitialization_attribute()
        {
            Validate("<context type='MyContext' disableDatabaseInitialization='false' />");
            Validate("<context type='MyContext' disableDatabaseInitialization='true' />");
        }

        [TestMethod]
        public void Schema_rejects_context_element_with_disableDatabaseInitialization_attribute_that_is_not_bool()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<context type='MyContext' disableDatabaseInitialization='1' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("disableDatabaseInitialization");
        }

        [TestMethod]
        public void Schema_rejects_context_element_without_type_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<context />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        [TestMethod]
        public void Schema_rejects_context_element_with_empty_type_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<context type='' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        [TestMethod]
        public void Schema_accepts_context_with_databaseInitilizer_child_element()
        {
            Validate("<context type='MyContext'><databaseInitializer type='MyContext' /></context>");
        }

        [TestMethod]
        public void Schema_rejects_context_with_multiple_databaseInitializer_elements()
        {
            var validationEvent = ValidateWithExpectedValidationEvents(
                "<context type='MyContext'><databaseInitializer type='MyContext' /><databaseInitializer type='MyContext1' /></context>")
                .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("databaseInitializer");
        }

        [TestMethod]
        public void Schema_rejects_context_element_child_elements_that_are_not_databaseInitializer_elements()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<context type='MyContext'><dummy /></context>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        #endregion

        #region interceptors element

        [TestMethod]
        public void Schema_accepts_empty_interceptors_element()
        {
            Validate("<interceptors />");
        }

        [TestMethod]
        public void Schema_accepts_interceptors_element_with_xdt_attributes()
        {
            Validate(@"<interceptors 
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_accepts_interceptors_element_with_many_interceptor_child_elements()
        {
            Validate("<interceptors><interceptor type='Interceptor1' /><interceptor type='Interceptor2' /></interceptors>");
        }

        [TestMethod]
        public void Schema_rejects_interceptors_element_child_elements_that_are_not_interceptor_elements()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<interceptors><dummy /></interceptors>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        #endregion

        #region interceptor element

        [TestMethod]
        public void Schema_accepts_empty_interceptor_element()
        {
            Validate("<interceptor type='MyInterceptor' />");
        }

        [TestMethod]
        public void Schema_accepts_interceptor_element_with_xdt_attributes()
        {
            Validate(@"<interceptor type='MyInterceptor'
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_rejects_interceptor_element_without_type_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<interceptor />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        [TestMethod]
        public void Schema_rejects_interceptor_element_with_empty_type_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<interceptor type='' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        #endregion

        #region parameters element

        [TestMethod]
        public void Schema_accepts_empty_parameters_element()
        {
            Validate("<parameters />");
        }

        [TestMethod]
        public void Schema_accepts_parameters_element_with_xdt_attributes()
        {
            Validate(@"<parameters 
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_accepts_parameters_element_with_many_parameter_child_elements()
        {
            Validate("<parameters><parameter value='1' /><parameter value='2' /></parameters>");
        }

        [TestMethod]
        public void Schema_rejects_parameters_element_child_elements_that_are_not_parameter_elements()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<parameters><dummy /></parameters>")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("dummy");
        }

        #endregion

        #region parameter element

        [TestMethod]
        public void Schema_accepts_parameter_element_with_value_attribute()
        {
            Validate("<parameter value='test' />");
        }

        [TestMethod]
        public void Schema_accepts_parameter_element_with_xdt_attributes()
        {
            Validate(@"<parameter value='test' 
                            xdt:Transform='Replace'
                            xmlns:xdt='http://schemas.microsoft.com/XML-Document-Transform' />");
        }

        [TestMethod]
        public void Schema_rejects_parameter_element_without_value_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<parameter />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("value");
        }

        [TestMethod]
        public void Schema_rejects_parameter_element_with_empty_value_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<parameter value='' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("value");
        }

        [TestMethod]
        public void Schema_accepts_parameter_element_with_type_attribute()
        {
            Validate("<parameter value='test' type='type' />");
        }

        [TestMethod]
        public void Schema_rejects_parameter_element_with_empty_type_attribute()
        {
            var validationEvent =
                ValidateWithExpectedValidationEvents("<parameter value='test' type='' />")
                    .Single();

            validationEvent.Severity.Should().Be(XmlSeverityType.Error);
            validationEvent.Message.Should().Contain("type");
        }

        #endregion

        [TestMethod]
        public void EntityFrameworkCatalog_points_to_the_right_config_schema()
        {
            var catalogXml = XDocument.Load(EntityFrameworkCatalogFileName);
            XNamespace catalogNs = "http://schemas.microsoft.com/xsd/catalog";

            var association =
                catalogXml
                    .Element(catalogNs + "SchemaCatalog")
                    .Element(catalogNs + "Association");
            association.Should().NotBeNull();

            Assert.Equal("config", (string)association.Attribute("extension"));
            Assert.Equal(
                string.Format("%InstallRoot%/xml/schemas/{0}", EntityFrameworkConfigSchemaName),
                (string)association.Attribute("schema"));
        }

        private List<ValidationEventArgs> ValidateWithExpectedValidationEvents(string config, bool allowAllTypesAtTopLevel = true)
        {
            var validationEvents = new List<ValidationEventArgs>();
            Validate(config, allowAllTypesAtTopLevel, (o, e) => validationEvents.Add(e));

            return validationEvents;
        }

        private void Validate(string config, bool allowAllTypesAtTopLevel = true)
        {
            // this will throw for warnings while validating without 
            // handler will not throw for warings.
            Validate(config, allowAllTypesAtTopLevel, (o, e) => { throw e.Exception; });
        }

        private static void Validate(string config, bool allowAllTypesAtTopLevel, ValidationEventHandler validationEventHandler)
        {
            var readerSettings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            readerSettings.Schemas.Add(ConfigurationFileSchema);
            readerSettings.Schemas.Add(FakeXmlDocumentTransformationSchema);

            if (allowAllTypesAtTopLevel)
            {
                readerSettings.Schemas.Add(HelperSchema);
            }

            readerSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            readerSettings.ValidationEventHandler += validationEventHandler;

            using (var reader = XmlReader.Create(new StringReader(config), readerSettings))
            {
                while (reader.Read())
                {
                }
            }
        }
    }
}
