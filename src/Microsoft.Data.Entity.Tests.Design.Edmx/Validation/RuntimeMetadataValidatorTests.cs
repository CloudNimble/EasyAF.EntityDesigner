// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

// Resharper wants to remove the below but do not - it causes build errors
// Resharper wants to remove the above but do not - it causes build errors

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.Edmx.Validation;
using Microsoft.Data.Entity.Design.XmlEngine.Common;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ComplexType = Microsoft.Data.Entity.Design.Edmx.Entity.ComplexType;
using DesignAssociationSetMapping = Microsoft.Data.Entity.Design.Edmx.Mapping.AssociationSetMapping;

namespace Microsoft.Data.Entity.Tests.Design.Edmx.Validation
{
    extern alias EntityDesignModel;
    [TestClass]
    public class RuntimeMetadataValidatorTests
    {
        private readonly IDbDependencyResolver _resolver;

        // Load SqlProviderServices dynamically
        private static DbProviderServices SqlProviderServicesInstance
        {
            get
            {
                Type type = Type.GetType("System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer");
                if (type != null)
                {
                    var instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    return (DbProviderServices)instanceProperty?.GetValue(null);
                }
                return null;
            }
        }

        public RuntimeMetadataValidatorTests()
        {
            Mock<IDbDependencyResolver> mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver.Setup(
                r => r.GetService(
                    It.Is<Type>(t => t == typeof(DbProviderServices)),
                    It.IsAny<string>())).Returns(SqlProviderServicesInstance);

            _resolver = mockResolver.Object;
        }

        [TestMethod]
        public void ValidateArtifactSet_does_not_validate_artifact_set_if_no_errors_and_forceValidation_false()
        {
            Mock<ModelManager> mockModelManager =
                new Mock<ModelManager>(new Mock<IEFArtifactFactory>().Object, new Mock<IEFArtifactSetFactory>().Object);

            using (var modelManager = mockModelManager.Object)
            {
                var artifact =
                    new Mock<EFArtifact>(
                        modelManager, new Uri("http://tempuri"), new Mock<XmlModelProvider>().Object).Object;

                Mock<EFArtifactSet> mockArtifactSet = new Mock<EFArtifactSet>(artifact);

                new RuntimeMetadataValidator(modelManager, new Version(2, 0, 0, 0), _resolver)
                    .ValidateArtifactSet(mockArtifactSet.Object, forceValidation: false, validateMsl: false, runViewGen: false);

                mockArtifactSet.Verify(m => m.AddError(It.IsAny<ErrorInfo>()), Times.Never());
            }
        }

        [TestMethod]
        public void ValidateArtifactSet_returns_errors_for_empty_conceptual_and_storage_models()
        {
            SetupModelAndInvokeAction(
                null, null, null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;
                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(2, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: false, validateMsl: false, runViewGen: false);

                        var errors = artifactSet.GetAllErrors();
                        errors.Count.Should().Be(2);
                        errors.First().ErrorCode.Should().Be(ErrorCodes.ErrorValidatingArtifact_StorageModelMissing);
                        errors.First().Message.Should().Contain(EdmxResources.ErrorValidatingArtifact_StorageModelMissing);
                        errors.Last().ErrorCode.Should().Be(ErrorCodes.ErrorValidatingArtifact_ConceptualModelMissing);
                        errors.Last().Message.Should().Contain(EdmxResources.ErrorValidatingArtifact_ConceptualModelMissing);
                        errors.All(e => ReferenceEquals(e.Item, artifactSet.GetEntityDesignArtifact())).Should().BeTrue();

                        // these errors should not clear error class flags
                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_All).Should().BeTrue();

                        mockArtifactSet.Verify(m => m.AddError(It.IsAny<ErrorInfo>()), Times.Exactly(2));
                    });
        }

        [TestMethod]
        public void
            ValidateArtifactSet_returns_errors_for_conceptual_and_storage_models_whose_version_is_greater_than_target_runtime_version()
        {
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" />",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" />",
                null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;
                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(2, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: false, validateMsl: false, runViewGen: false);

                        var errors = artifactSet.GetAllErrors();
                        errors.Count.Should().Be(2);
                        errors.First().ErrorCode.Should().Be(ErrorCodes.ErrorValidatingArtifact_InvalidSSDLNamespaceForTargetFrameworkVersion);
                        errors.First().Message.Should().Contain(EdmxResources.ErrorValidatingArtifact_InvalidSSDLNamespaceForTargetFrameworkVersion);
                        errors.First().Item.Should().Be(artifactSet.GetEntityDesignArtifact().StorageModel);
                        errors.Last().ErrorCode.Should().Be(ErrorCodes.ErrorValidatingArtifact_InvalidCSDLNamespaceForTargetFrameworkVersion);
                        errors.Last().Message.Should().Contain(EdmxResources.ErrorValidatingArtifact_InvalidCSDLNamespaceForTargetFrameworkVersion);
                        errors.Last().Item.Should().Be(artifactSet.GetEntityDesignArtifact().ConceptualModel);

                        // these error should not clear error class flags
                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_All).Should().BeTrue();

                        mockArtifactSet.Verify(m => m.AddError(It.IsAny<ErrorInfo>()), Times.Exactly(2));
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_returns_errors_for_invalid_conceptual_and_storage_model()
        {
            // Both Csdl and Ssdl are missing the 'Namespace' attribute and therefore are invalid.
            // We don't really care about what errors we will get as long as they are thrown by
            // the runtime and one is for Csdl and the other one is for Ssdl.
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" />",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;
                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: false, validateMsl: false, runViewGen: false);

                        var errors = artifactSet.GetAllErrors();
                        errors.Count.Should().Be(2);
                        errors.Count(e => e.ErrorClass == ErrorClass.Runtime_CSDL).Should().Be(1);
                        errors.Count(e => e.ErrorClass == ErrorClass.Runtime_SSDL).Should().Be(1);

                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_CSDL | ErrorClass.Runtime_SSDL).Should().BeFalse();
                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_MSL | ErrorClass.Runtime_ViewGen).Should().BeTrue();

                        mockArtifactSet.Verify(m => m.AddError(It.IsAny<ErrorInfo>()), Times.Exactly(2));
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_returns_errors_cached_when_reverse_engineering_db()
        {
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" Namespace=\"Model\">" +
                "  <EntityContainer Name=\"ModelContainer\" />" +
                "</Schema>",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\">"
                +
                "  <EntityContainer Name=\"StoreContainer\" />" +
                "</Schema>",
                "<Mapping Space=\"C-S\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/mapping/cs\">" +
                "  <EntityContainerMapping StorageEntityContainer=\"StoreContainer\" CdmEntityContainer=\"ModelContainer\" />" +
                "</Mapping>",
                (mockModelManager, mockArtifactSet) =>
                    {
                        EdmSchemaError error = new EdmSchemaError("test", 42, EdmSchemaErrorSeverity.Error);

                        var artifactSet = mockArtifactSet.Object;
                        Mock<EntityDesignArtifact> mockArtifact = Mock.Get(artifactSet.GetEntityDesignArtifact());
                        mockArtifact
                            .Setup(m => m.GetModelGenErrors())
                            .Returns(new List<EdmSchemaError>(new[] { error }));

                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: true, validateMsl: true, runViewGen: true);

                        var errors = artifactSet.GetAllErrors();
                        errors.Count.Should().Be(1);
                        errors.Count(e => e.ErrorClass == ErrorClass.Runtime_SSDL).Should().Be(1);
                        errors.Single().Message.Should().Contain(error.Message);

                        mockArtifactSet.Verify(m => m.AddError(It.IsAny<ErrorInfo>()), Times.Exactly(1));
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_does_not_validate_mapping_if_mapping_validation_disabled()
        {
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" Namespace=\"Model\" />",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                "<dummy />",
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;
                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: true, validateMsl: false, runViewGen: false);

                        artifactSet.GetAllErrors().Should().BeEmpty();

                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_CSDL | ErrorClass.Runtime_SSDL).Should().BeFalse();
                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_MSL | ErrorClass.Runtime_ViewGen).Should().BeTrue();
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_returns_errors_for_empty_mapping_model()
        {
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" Namespace=\"Model\" />",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;
                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: true, validateMsl: true, runViewGen: false);

                        var errors = artifactSet.GetAllErrors();
                        errors.Count.Should().Be(1);
                        errors.First().ErrorCode.Should().Be(ErrorCodes.ErrorValidatingArtifact_MappingModelMissing);
                        errors.First().Item.Should().BeSameAs(artifactSet.GetEntityDesignArtifact());

                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Escher_CSDL | ErrorClass.Escher_SSDL).Should().BeFalse();
                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Escher_MSL | ErrorClass.Runtime_ViewGen).Should().BeTrue();

                        mockArtifactSet.Verify(m => m.AddError(It.IsAny<ErrorInfo>()), Times.Once());
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_validates_Version3_models_successfully()
        {
            // Only Version3 is supported - test that Version3 models validate without version mismatch errors
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" Namespace=\"Model\" />",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                "<Mapping Space=\"C-S\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/mapping/cs\" />",
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;
                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: true, validateMsl: true, runViewGen: false);

                        // No version mismatch errors should occur since all models use Version3
                        var errors = artifactSet.GetAllErrors();
                        errors.Any(e => e.ErrorCode == ErrorCodes.ErrorValidatingArtifact_InvalidMSLNamespaceForTargetFrameworkVersion).Should().BeFalse();
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_returns_errors_for_invalid_mapping_model_if_mapping_validation_enabled()
        {
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" Namespace=\"Model\" />",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                "<dummy />",
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;

                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: true, validateMsl: true, runViewGen: true);

                        var errors = artifactSet.GetAllErrors();
                        errors.Count.Should().Be(1);
                        errors.All(e => e.ErrorClass == ErrorClass.Runtime_MSL).Should().BeTrue();

                        artifactSet.IsValidityDirtyForErrorClass(
                            ErrorClass.Runtime_CSDL | ErrorClass.Runtime_SSDL | ErrorClass.Runtime_MSL).Should().BeFalse();

                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_ViewGen).Should().BeTrue();
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_returns_errors_if_validation_with_view_generation_fails()
        {
            // The mapping here is invalid - both C-Space properties are mapped to one S-Space property.
            // This condition is not detected when loading StorageMappingItemCollection but it make view gen
            // throw. In this case we don't really care about what error will be thrown as long as it is
            // thrown by view gen
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" xmlns:cg=\"http://schemas.microsoft.com/ado/2006/04/codegeneration\" xmlns:store=\"http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator\" Namespace=\"Model\" Alias=\"Self\" xmlns:annotation=\"http://schemas.microsoft.com/ado/2009/02/edm/annotation\" annotation:UseStrongSpatialTypes=\"false\">"
                +
                "  <EntityContainer Name=\"ModelContainer\" annotation:LazyLoadingEnabled=\"true\">" +
                "      <EntitySet Name=\"Entities\" EntityType=\"Model.Entity\" />" +
                "  </EntityContainer>" +
                "  <EntityType Name=\"Entity\">" +
                "      <Key>" +
                "      <PropertyRef Name=\"Id\" />" +
                "      </Key>" +
                "      <Property Type=\"Int32\" Name=\"Id\" Nullable=\"false\" annotation:StoreGeneratedPattern=\"Identity\" />" +
                "      <Property Type=\"Int32\" Name=\"IntProperty\" Nullable=\"false\" />" +
                "  </EntityType>" +
                "</Schema>",
                "<Schema Namespace=\"Model.Store\" Alias=\"Self\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2008\" xmlns:store=\"http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\">"
                +
                "  <EntityContainer Name=\"ModelStoreContainer\">" +
                "    <EntitySet Name=\"Entity\" EntityType=\"Model.Store.Entity\" store:Type=\"Tables\" Schema=\"dbo\" />" +
                "  </EntityContainer>" +
                "  <EntityType Name=\"Entity\">" +
                "    <Key>" +
                "      <PropertyRef Name=\"Id\" />" +
                "    </Key>" +
                "    <Property Name=\"Id\" Type=\"int\" StoreGeneratedPattern=\"Identity\" Nullable=\"false\" />" +
                "    <Property Name=\"IntProperty\" Type=\"int\" Nullable=\"false\" />" +
                "  </EntityType>" +
                "</Schema>",
                "<Mapping Space=\"C-S\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/mapping/cs\">" +
                "  <EntityContainerMapping StorageEntityContainer=\"ModelStoreContainer\" CdmEntityContainer=\"ModelContainer\">" +
                "    <EntitySetMapping Name=\"Entities\">" +
                "      <EntityTypeMapping TypeName=\"IsTypeOf(Model.Entity)\">" +
                "        <MappingFragment StoreEntitySet=\"Entity\">" +
                "          <ScalarProperty Name=\"Id\" ColumnName=\"Id\" />" +
                "          <ScalarProperty Name=\"IntProperty\" ColumnName=\"Id\" />" +
                "        </MappingFragment>" +
                "      </EntityTypeMapping>" +
                "    </EntitySetMapping>" +
                "  </EntityContainerMapping>" +
                "</Mapping>",
                (mockModelManager, mockArtifactSet) =>
                    {
                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(mockArtifactSet.Object, forceValidation: true, validateMsl: true, runViewGen: true);

                        var errors = mockArtifactSet.Object.GetAllErrors();
                        errors.Count.Should().Be(1);
                        errors.All(e => e.ErrorClass == ErrorClass.Runtime_ViewGen).Should().BeTrue();

                        mockArtifactSet.Object.IsValidityDirtyForErrorClass(ErrorClass.Runtime_All).Should().BeFalse();
                    });
        }

        [TestMethod]
        public void ValidateArtifactSet_returns_no_errors_for_valid_artifacts()
        {
            SetupModelAndInvokeAction(
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm\" Namespace=\"Model\">" +
                "  <EntityContainer Name=\"ModelContainer\" />" +
                "</Schema>",
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\">"
                +
                "  <EntityContainer Name=\"StoreContainer\" />" +
                "</Schema>",
                "<Mapping Space=\"C-S\" xmlns=\"http://schemas.microsoft.com/ado/2009/11/mapping/cs\">" +
                "  <EntityContainerMapping StorageEntityContainer=\"StoreContainer\" CdmEntityContainer=\"ModelContainer\" />" +
                "</Mapping>",
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;

                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ValidateArtifactSet(artifactSet, forceValidation: true, validateMsl: true, runViewGen: true);

                        artifactSet.GetAllErrors().Should().BeEmpty();
                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_All).Should().BeFalse();

                        mockArtifactSet.Verify(m => m.AddError(It.IsAny<ErrorInfo>()), Times.Never());
                    });
        }

        [TestMethod]
        public void Validate_calls_into_ValidateArtifactSet_with_correct_parameter_values()
        {
            SetupModelAndInvokeAction(
                null, null, null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        Mock<RuntimeMetadataValidator> mockValidator =
                            new Mock<RuntimeMetadataValidator>(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver);

                        mockValidator.Object.Validate(mockArtifactSet.Object);

                        mockValidator
                            .Verify(m => m.ValidateArtifactSet(mockArtifactSet.Object, true, true, false), Times.Once());
                    });
        }

        [TestMethod]
        public void ValidateAndCompileMappings_calls_into_ValidateArtifactSet_with_correct_parameter_values()
        {
            SetupModelAndInvokeAction(
                null, null, null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        Mock<RuntimeMetadataValidator> mockValidator =
                            new Mock<RuntimeMetadataValidator>(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver);

                        foreach (var validateMapping in new[] { true, false })
                        {
                            mockValidator.Object.ValidateAndCompileMappings(mockArtifactSet.Object, validateMapping);

                            mockValidator
                                .Verify(
                                    m =>
                                    m.ValidateArtifactSet(mockArtifactSet.Object, false, validateMapping, validateMapping),
                                    Times.Once());
                        }
                    });
        }

        [TestMethod]
        public void ProcessErrors_adds_errors_to_artifact_set()
        {
            SetupModelAndInvokeAction(
                null, null, null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifactSet = mockArtifactSet.Object;
                        var artifact = artifactSet.GetEntityDesignArtifact();
                        artifact.SetValidityDirtyForErrorClass(ErrorClass.Runtime_CSDL, true);

                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ProcessErrors(
                                new List<EdmSchemaError>
                                    {
                                        new EdmSchemaError("abc", 42, EdmSchemaErrorSeverity.Error)
                                    },
                                artifact, ErrorClass.Runtime_CSDL);

                        artifactSet.GetAllErrors().Count.Should().Be(1);
                        var error = artifactSet.GetAllErrors().Single();
                        error.Message.Should().Contain("abc");
                        error.ErrorCode.Should().Be(42);
                        error.ErrorClass.Should().Be(ErrorClass.Runtime_CSDL);
                        error.Level.Should().Be(ErrorInfo.Severity.ERROR);

                        artifactSet.IsValidityDirtyForErrorClass(ErrorClass.Runtime_CSDL).Should().BeFalse();
                    });
        }

        [TestMethod]
        public void ProcessErrors_changes_severity_to_warning_for_NotSpecifiedInstanceForEntitySetOrAssociationSet_if_store_model_empty()
        {
            SetupModelAndInvokeAction(
                null,
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        const int NotSpecifiedInstanceForEntitySetOrAssociationSet = 2062;

                        var artifactSet = mockArtifactSet.Object;
                        var artifact = artifactSet.GetEntityDesignArtifact();
                        Mock.Get(artifact.StorageModel)
                            .Setup(m => m.FirstEntityContainer)
                            .Returns(new Mock<StorageEntityContainer>(artifact.StorageModel, new XElement("dummy")).Object);

                        artifact.SetValidityDirtyForErrorClass(ErrorClass.Runtime_CSDL, true);

                        new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                            .ProcessErrors(
                                new List<EdmSchemaError>
                                    {
                                        new EdmSchemaError(
                                            "abc", NotSpecifiedInstanceForEntitySetOrAssociationSet,
                                            EdmSchemaErrorSeverity.Error)
                                    },
                                artifact, ErrorClass.Runtime_CSDL);

                        artifactSet.GetAllErrors().Count.Should().Be(1);
                        var error = artifactSet.GetAllErrors().Single();
                        error.Message.Should().Contain("abc");
                        error.ErrorCode.Should().Be(NotSpecifiedInstanceForEntitySetOrAssociationSet);
                        error.Level.Should().Be(ErrorInfo.Severity.WARNING);
                    });
        }

        [TestMethod]
        public void ProcessErrors_rewrites_NotQualifiedTypeErrorCode()
        {
            SetupModelAndInvokeAction(
                null,
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        const int NotQualifiedTypeErrorCode = 40;

                        var artifactSet = mockArtifactSet.Object;
                        var artifact = artifactSet.GetEntityDesignArtifact();
                        var complexType = new Mock<ComplexType>(null, new XElement("dummy")).Object;
                        Mock<ComplexConceptualProperty> mockProperty = new Mock<ComplexConceptualProperty>(complexType, new XElement("dummy"))
                            {
                                CallBase = true
                            };
                        mockProperty.Setup(m => m.Artifact).Returns(artifact);
                        var property = mockProperty.Object;
                        property.ComplexType.SetXObject(new XAttribute("typeName", EdmxResources.ComplexPropertyUndefinedType));

                        Mock.Get(artifact)
                            .Setup(m => m.FindEFObjectForLineAndColumn(It.IsAny<int>(), It.IsAny<int>()))
                            .Returns(property);

                        artifact.SetValidityDirtyForErrorClass(ErrorClass.Runtime_CSDL, true);

                        try
                        {
                            new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                                .ProcessErrors(
                                    new List<EdmSchemaError>
                                        {
                                            new EdmSchemaError(
                                                "abc", NotQualifiedTypeErrorCode,
                                                EdmSchemaErrorSeverity.Error)
                                        },
                                    artifact, ErrorClass.Runtime_CSDL);

                            artifactSet.GetAllErrors().Count.Should().Be(1);
                            var error = artifactSet.GetAllErrors().Single();
                            error.Message.Should().Contain(
                                string.Format(EdmxResources.EscherValidation_UndefinedComplexPropertyType, string.Empty));
                            error.ErrorCode.Should().Be(ErrorCodes.ESCHER_VALIDATOR_UNDEFINED_COMPLEX_PROPERTY_TYPE);
                            error.Item.Should().Be(property);
                            error.Level.Should().Be(ErrorInfo.Severity.ERROR);
                        }
                        finally
                        {
                            property.LocalName.Dispose();
                            property.ComplexType.Dispose();
                        }
                    });
        }

        [TestMethod]
        public void ProcessErrors_rewrites_NonValidAssociationSet_warning()
        {
            SetupModelAndInvokeAction(
                null,
                "<Schema xmlns=\"http://schemas.microsoft.com/ado/2009/11/edm/ssdl\" Namespace=\"Model.Store\" Provider=\"System.Data.SqlClient\" ProviderManifestToken=\"2012\"/>",
                null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        const int NonValidAssociationSet = 2005;

                        var artifactSet = mockArtifactSet.Object;
                        var artifact = artifactSet.GetEntityDesignArtifact();

                        Mock<DesignAssociationSetMapping> mockAssociationSetMapping = new Mock<DesignAssociationSetMapping>(null, new XElement("dummy"));
                        mockAssociationSetMapping
                            .Setup(m => m.Artifact)
                            .Returns(artifact);

                        try
                        {
                            Mock.Get(artifact)
                                .Setup(m => m.FindEFObjectForLineAndColumn(It.IsAny<int>(), It.IsAny<int>()))
                                .Returns(mockAssociationSetMapping.Object);

                            artifact.SetValidityDirtyForErrorClass(ErrorClass.Runtime_CSDL, true);

                            new RuntimeMetadataValidator(mockModelManager.Object, new Version(3, 0, 0, 0), _resolver)
                                .ProcessErrors(
                                    new List<EdmSchemaError>
                                        {
                                            new EdmSchemaError(
                                                "abc", NonValidAssociationSet,
                                                EdmSchemaErrorSeverity.Warning)
                                        },
                                    artifact, ErrorClass.Runtime_CSDL);

                            artifactSet.GetAllErrors().Count.Should().Be(1);
                            var error = artifactSet.GetAllErrors().Single();
                            error.Message.Should().Contain(
                                string.Format(EdmxResources.EscherValidation_IgnoreMappedFKAssociation, string.Empty));
                            error.ErrorCode.Should().Be(NonValidAssociationSet);
                            error.Level.Should().Be(ErrorInfo.Severity.WARNING);
                        }
                        finally
                        {
                            mockAssociationSetMapping.Object.Name.Dispose();
                        }
                    });
        }

        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_non_runtime_errors()
        {
            SetupModelAndInvokeAction(
                null, null, null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        ErrorInfo error = new ErrorInfo(
                            ErrorInfo.Severity.ERROR, null, mockArtifactSet.Object.GetEntityDesignArtifact(),
                            42, ErrorClass.ParseError);
                        RuntimeMetadataValidator.IsOpenInEditorError(error, mockArtifactSet.Object.Artifacts.First())
                            .Should().BeFalse();
                    });
        }

#if (VS11 || VS12)
        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_recoverable_runtime_errors()
        {
            IsOpenInEditorError<EFObject>(-1).Should().BeFalse();
        }
#endif

        [TestMethod]
        public void IsOpenInEditorError_returns_true_for_unrecoverable_errors_if_additional_conditions_not_met()
        {
            SetupModelAndInvokeAction(
                null, null, null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifact = mockArtifactSet.Object.GetEntityDesignArtifact();

                        Mock<EFObject> mockEfObject = new Mock<EFObject>(null, new XElement("dummy"));
                        mockEfObject.Setup(m => m.Artifact).Returns(artifact);

                        var unrecoverableErrorCodes =
                            UnrecoverableRuntimeErrors
                                .SchemaObjectModelErrorCodes.Cast<int>()
                                .Concat(UnrecoverableRuntimeErrors.StorageMappingErrorCodes.Cast<int>());

                        foreach (var errorCode in unrecoverableErrorCodes)
                        {
                            ErrorInfo error = new ErrorInfo(
                                ErrorInfo.Severity.ERROR, null, mockEfObject.Object, errorCode, ErrorClass.Runtime_CSDL);

                            RuntimeMetadataValidator.IsOpenInEditorError(error, artifact).Should().BeTrue();
                        }
                    });
        }

#if (VS11 || VS12)
        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_SchemaValidationError_for_ModificationFunctionMapping()
        {
            IsOpenInEditorError<ModificationFunctionMapping>((int)MappingErrorCode.XmlSchemaValidationError).Should().BeFalse();
        }

        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_XmlError_for_ReferentialConstraintRole()
        {
            IsOpenInEditorError<ReferentialConstraintRole>((int)ErrorCode.XmlError).Should().BeFalse();
        }

        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_ConditionError_for_Condition()
        {
            IsOpenInEditorError<Condition>((int)MappingErrorCode.ConditionError).Should().BeFalse();
        }

        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_InvalidPropertyInRelationshipConstraint_for_ReferentialConstraint()
        {
            IsOpenInEditorError<ReferentialConstraint>((int)ErrorCode.InvalidPropertyInRelationshipConstraint).Should().BeFalse();
        }

        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_ESCHER_VALIDATOR_UNDEFINED_COMPLEX_PROPERTY_TYPE()
        {
            IsOpenInEditorError<EFObject>(ErrorCodes.ESCHER_VALIDATOR_UNDEFINED_COMPLEX_PROPERTY_TYPE).Should().BeFalse();
        }

        [TestMethod]
        public void IsOpenInEditorError_returns_false_for_NotInNamespace_for_ComplexConceptualProperty()
        {
            IsOpenInEditorError<ComplexConceptualProperty>((int)ErrorCode.NotInNamespace).Should().BeFalse();
        }
#endif

        private static bool IsOpenInEditorError<T>(int errorCode) where T : EFObject
        {
            var openInEditor = true;

            SetupModelAndInvokeAction(
                null, null, null,
                (mockModelManager, mockArtifactSet) =>
                    {
                        var artifact = mockArtifactSet.Object.GetEntityDesignArtifact();

                        Mock<T> mockEfObject =
                            new Mock<T>(null, new XElement("dummy"));
                        mockEfObject.Setup(m => m.Artifact).Returns(artifact);

                        Mock.Get(artifact)
                            .Setup(m => m.FindEFObjectForLineAndColumn(It.IsAny<int>(), It.IsAny<int>()))
                            .Returns(mockEfObject.Object);

                        ErrorInfo error = new ErrorInfo(
                            ErrorInfo.Severity.ERROR, null, mockEfObject.Object,
                            errorCode, ErrorClass.Runtime_CSDL);

                        openInEditor = RuntimeMetadataValidator.IsOpenInEditorError(error, artifact);
                    });

            return openInEditor;
        }

        private static void SetupModelAndInvokeAction(
            string conceptualModel, string storeModel, string mappingModel, Action<Mock<ModelManager>, Mock<EFArtifactSet>> action)
        {
            Uri tempUri = new Uri("http://tempuri");

            Mock<ModelManager> mockModelManager =
                new Mock<ModelManager>(new Mock<IEFArtifactFactory>().Object, new Mock<IEFArtifactSetFactory>().Object);

            using (var modelManager = mockModelManager.Object)
            {
                Mock<XmlModelProvider> xmlModelProvider = new Mock<XmlModelProvider>();
                xmlModelProvider
                    .Setup(m => m.GetXmlModel(tempUri))
                    .Returns(new Mock<XmlModel>().Object);

                Mock<EntityDesignArtifact> mockArtifact =
                    new Mock<EntityDesignArtifact>(modelManager, tempUri, xmlModelProvider.Object);

                mockArtifact
                    .Setup(m => m.Artifact)
                    .Returns(mockArtifact.Object);

                mockArtifact
                    .Setup(m => m.FindEFObjectForLineAndColumn(It.IsAny<int>(), It.IsAny<int>()))
                    .Returns(mockArtifact.Object);

                mockArtifact
                    .Setup(m => m.Uri)
                    .Returns(tempUri);

                SetupConceptualModel(conceptualModel, mockArtifact);
                SetupStoreModel(storeModel, mockArtifact);
                SetupMappingModel(mappingModel, mockArtifact);

                mockArtifact.Object.SetValidityDirtyForErrorClass(ErrorClass.Runtime_All, true);

                Mock<EFArtifactSet> mockArtifactSet = new Mock<EFArtifactSet>(mockArtifact.Object) { CallBase = true };
                mockArtifact
                    .Setup(m => m.ArtifactSet)
                    .Returns(mockArtifactSet.Object);

                action(mockModelManager, mockArtifactSet);
            }
        }

        private static void SetupStoreModel(string storeModel, Mock<EntityDesignArtifact> mockArtifact)
        {
            if (storeModel != null)
            {
                Mock<StorageEntityModel> mockStorageModel =
                    new Mock<StorageEntityModel>(
                        mockArtifact.Object,
                        XElement.Parse(storeModel));

                mockStorageModel
                    .Setup(m => m.Artifact)
                    .Returns(mockArtifact.Object);

                mockArtifact
                    .Setup(m => m.StorageModel)
                    .Returns(mockStorageModel.Object);
            }
        }

        private static void SetupConceptualModel(string conceptualModel, Mock<EntityDesignArtifact> mockArtifact)
        {
            if (conceptualModel != null)
            {
                Mock<ConceptualEntityModel> mockConceptualModel =
                    new Mock<ConceptualEntityModel>(
                        mockArtifact.Object,
                        XElement.Parse(conceptualModel));

                mockConceptualModel
                    .Setup(m => m.Artifact)
                    .Returns(mockArtifact.Object);

                mockArtifact
                    .Setup(m => m.ConceptualModel)
                    .Returns(mockConceptualModel.Object);
            }
        }

        private static void SetupMappingModel(string mappingModel, Mock<EntityDesignArtifact> mockArtifact)
        {
            if (mappingModel != null)
            {
                Mock<MappingModel> mockMappingModel =
                    new Mock<MappingModel>(
                        mockArtifact.Object,
                        XElement.Parse(mappingModel));

                mockMappingModel
                    .Setup(m => m.Artifact)
                    .Returns(mockArtifact.Object);

                mockArtifact
                    .Setup(m => m.MappingModel)
                    .Returns(mockMappingModel.Object);
            }
        }
    }
}
