// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Microsoft.Data.Entity.Tests.Design.Edmx
{
    /// <summary>
    ///     Tests for EdmFeatureManager.
    ///     All features are now always enabled for EF6 (Version3 only).
    /// </summary>
    [TestClass]
    public class EdmFeatureManagerTests
    {
        [TestMethod]
        public void FunctionImportReturningComplexType_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetFunctionImportReturningComplexTypeFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void EnumTypes_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetEnumTypeFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void EnumTypes_always_enabled_for_artifacts()
        {
            // Version3 always returns enabled since we only support EF6
            IsFeatureEnabledForArtifact(EntityFrameworkVersion.Version3, EdmFeatureManager.GetEnumTypeFeatureState)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void FunctionImportColumnInformation_always_enabled()
        {
            foreach (var targetSchemaVersion in EntityFrameworkVersion.GetAllVersions())
            {
                IsFeatureEnabledForArtifact(
                    targetSchemaVersion,
                    EdmFeatureManager.GetFunctionImportColumnInformationFeatureState)
                    .Should().Be(FeatureState.VisibleAndEnabled);
            }
        }

        private static FeatureState IsFeatureEnabledForArtifact(Version schemaVersion, Func<EFArtifact, FeatureState> funcToTest)
        {
            using (EditingContext editingContext = new EditingContext())
            {
                var modelManager = new Mock<ModelManager>(null, null).Object;
                var modelProvider = new Mock<XmlModelProvider>().Object;

                Mock<EntityDesignArtifact> entityDesignArtifactMock =
                    new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

                editingContext.SetEFArtifactService(new EFArtifactService(entityDesignArtifactMock.Object));

                entityDesignArtifactMock.Setup(a => a.SchemaVersion).Returns(schemaVersion);
                entityDesignArtifactMock.Setup(a => a.EditingContext).Returns(editingContext);

                return funcToTest(entityDesignArtifactMock.Object);
            }
        }

        [TestMethod]
        public void FunctionImportMapping_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetFunctionImportMappingFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void ForeignKeys_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetForeignKeysInModelFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void UpdateViews_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetGenerateUpdateViewsFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void EntityContainerTypeAccess_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetEntityContainerTypeAccessFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void LazyLoading_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetLazyLoadingFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void ComposableFunctionImport_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetComposableFunctionImportFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }

        [TestMethod]
        public void SpatialTypes_always_enabled()
        {
            // Version3 always returns enabled since we only support EF6
            EdmFeatureManager.GetUseStrongSpatialTypesFeatureState(EntityFrameworkVersion.Version3)
                .Should().Be(FeatureState.VisibleAndEnabled);
        }
    }
}
