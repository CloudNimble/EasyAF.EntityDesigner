// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using VSLangProj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Resources = Microsoft.Data.Entity.Design.Resources;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Package;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class EdmUtilsTests
    {
        [TestMethod]
        public void SafeLoadXmlFromString_throws_if_xml_contains_entities()
        {
            Action act = () => EdmUtils.SafeLoadXmlFromString(
                "<!ENTITY network \"network\">\n<entity-framework>&network;</entity-framework>");
            var exception = act.Should().Throw<XmlException>().Which;

            exception.Message.Should().Contain("DTD");
            exception.Message.Should().Contain("DtdProcessing");
            exception.Message.Should().Contain("Parse");
        }

        [TestMethod]
        public void SafeLoadXmlFromString_can_load_xml_without_entities()
        {
            var xmlDoc = EdmUtils.SafeLoadXmlFromString("<entity-framework />");
            xmlDoc.Should().NotBeNull();
            xmlDoc.DocumentElement.Name.Should().Be("entity-framework");
        }

        [TestMethod]
        public void SafeLoadXmlFromPath_throws_if_xml_contains_entities()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "<!ENTITY network \"network\">\n<entity-framework>&network;</entity-framework>");

            Action act = () => EdmUtils.SafeLoadXmlFromPath(filePath);
            var exception = act.Should().Throw<XmlException>().Which;

            exception.Message.Should().Contain("DTD");
            exception.Message.Should().Contain("DtdProcessing");
            exception.Message.Should().Contain("Parse");
        }

        [TestMethod]
        public void SafeLoadXmlFromPath_can_load_xml_without_entities()
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "<entity-framework />");

            var xmlDoc = EdmUtils.SafeLoadXmlFromPath(filePath);
            xmlDoc.Should().NotBeNull();
            xmlDoc.DocumentElement.Name.Should().Be("entity-framework");
        }

        [TestMethod]
        public void IsValidModelNamespace_returns_false_for_invalid_namespaces()
        {
            // the version does not matter since the definition
            // of allowed strings for namespaces have not changed since v1
            EdmUtils.IsValidModelNamespace(null).Should().BeFalse();
            EdmUtils.IsValidModelNamespace(string.Empty).Should().BeFalse();
            EdmUtils.IsValidModelNamespace("\u0001\u0002").Should().BeFalse();
            EdmUtils.IsValidModelNamespace("<>").Should().BeFalse();
        }

        [TestMethod]
        public void IsValidModelNamespace_returns_true_for_valid_namespaces()
        {
            // the version does not matter since the definition
            // of allowed strings for namespaces have not changed since v1
            EdmUtils.IsValidModelNamespace("abc").Should().BeTrue();
        }

        [TestMethod]
        public void ConstructUniqueNamespaces_returns_proposed_namespace_if_existing_namespaces_null()
        {
            EdmUtils.ConstructUniqueNamespace("testNamespace", null).Should().Be("testNamespace");
        }

        [TestMethod]
        public void ConstructUniqueNamespaces_returns_uniquified_namespace_()
        {
            EdmUtils.ConstructUniqueNamespace("Model", ["Model"]).Should().Be("Model1");
        }

        [TestMethod]
        public void ConstructValidModelNamespace_returns_proposed_namespace_if_valid()
        {
            EdmUtils.ConstructValidModelNamespace("proposed", "default").Should().Be("proposed");
        }

        [TestMethod]
        public void ConstructValidModelNamespace_returns_default_namespace_if_proposed_namespace_null_or_empty_string()
        {
            EdmUtils.ConstructValidModelNamespace(null, "default").Should().Be("default");
            EdmUtils.ConstructValidModelNamespace(string.Empty, "default").Should().Be("default");
        }

        [TestMethod]
        public void ConstructValidModelNamespace_returns_default_namespace_if_sanitized_proposed_namespace_invalid_or_empty_string()
        {
            EdmUtils.ConstructValidModelNamespace("&", "default").Should().Be("default");
            EdmUtils.ConstructValidModelNamespace("&\u0001", "default").Should().Be("default");
            EdmUtils.ConstructValidModelNamespace("&123", "default").Should().Be("default");
            EdmUtils.ConstructValidModelNamespace("_a a", "default").Should().Be("default");
        }

        [TestMethod]
        public void ConstructValidModelNamespace_returns_sanitized_proposed_namespace_if_sanitized_proposed_namespace_valid()
        {
            EdmUtils.ConstructValidModelNamespace("<proposed>", "default").Should().Be("proposed");
            EdmUtils.ConstructValidModelNamespace("<123_proposed>", "default").Should().Be("proposed");
        }

        [TestMethod]
        public void GetEntityFrameworkVersion_returns_null_when_misc_files()
        {
            var project = MockDTE.CreateMiscFilesProject();
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();

            var schemaVersion = EdmUtils.GetEntityFrameworkVersion(project, serviceProvider.Object);

            schemaVersion.Should().BeNull();
        }

        [TestMethod]
        public void GetEntityFrameworkVersion_returns_version_when_ef_installed()
        {
            MockDTE helper = new MockDTE(
                ".NETFramework,Version=v4.5", references: new[] { MockDTE.CreateReference("EntityFramework", "6.0.0.0") });

            var schemaVersion = EdmUtils.GetEntityFrameworkVersion(helper.Project, helper.ServiceProvider);

            schemaVersion.Should().Be(EntityFrameworkVersion.Version3);
        }

        [TestMethod]
        public void GetEntityFrameworkVersion_always_returns_Version3_for_modern_development()
        {
            // For modern development, GetEntityFrameworkVersion always returns Version3
            var netFxVersions = new[]
                {
                    ".NETFramework,Version=v4.5",
                    ".NETFramework,Version=v4.0",
                    ".NETFramework,Version=v3.5"
                };

            foreach (var netFx in netFxVersions)
            {
                MockDTE helper = new MockDTE(netFx, references: new Reference[0]);
                EdmUtils.GetEntityFrameworkVersion(helper.Project, helper.ServiceProvider, useLatestIfNoEF: true)
                    .Should().Be(new Version(3, 0, 0, 0));
                EdmUtils.GetEntityFrameworkVersion(helper.Project, helper.ServiceProvider, useLatestIfNoEF: false)
                    .Should().Be(new Version(3, 0, 0, 0));
            }
        }

        [TestMethod]
        public void
            CreateUpdateCodeGenStrategyCommand_returns_UpdateDefaultableValueCommand_when_updating_CodeGenStrategy_value_from_empty_string()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                const string designerPropertyName = "CodeGenerationStrategy";
                designerInfoRoot
                    .AddDesignerInfo(
                        "Options",
                        SetupOptionsDesignerInfo(designerPropertyName, string.Empty));

                entityDesignArtifactMock
                    .Setup(a => a.DesignerInfo)
                    .Returns(designerInfoRoot);

                EdmUtils.SetCodeGenStrategyToNoneCommand(entityDesignArtifactMock.Object)
                    .Should().BeOfType<UpdateDefaultableValueCommand<string>>();
            }
        }

        [TestMethod]
        public void
            CreateUpdateCodeGenStrategyCommand_returns_UpdateDefaultableValueCommand_when_updating_CodeGenStrategy_value_from_non_empty_string
            ()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                const string designerPropertyName = "CodeGenerationStrategy";
                designerInfoRoot
                    .AddDesignerInfo(
                        "Options",
                        SetupOptionsDesignerInfo(designerPropertyName, "Default"));

                entityDesignArtifactMock
                    .Setup(a => a.DesignerInfo)
                    .Returns(designerInfoRoot);

                EdmUtils.SetCodeGenStrategyToNoneCommand(entityDesignArtifactMock.Object)
                    .Should().BeOfType<UpdateDefaultableValueCommand<string>>();
            }
        }

        [TestMethod]
        public void CreateUpdateCodeGenStrategyCommand_returns_null_when_attempting_to_update_CodeGenStrategy_to_same_value()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                const string designerPropertyName = "CodeGenerationStrategy";
                designerInfoRoot
                    .AddDesignerInfo(
                        "Options",
                        SetupOptionsDesignerInfo(designerPropertyName, "None"));

                entityDesignArtifactMock
                    .Setup(a => a.DesignerInfo)
                    .Returns(designerInfoRoot);

                EdmUtils.SetCodeGenStrategyToNoneCommand(entityDesignArtifactMock.Object).Should().BeNull();
            }
        }

        private DesignerInfo SetupOptionsDesignerInfo(string designerPropertyName, string designerPropertyValue)
        {
            OptionsDesignerInfo designerInfo =
                new OptionsDesignerInfo(
                    null,
                    XElement.Parse(
                        "<Options xmlns='http://schemas.microsoft.com/ado/2009/11/edmx' />"));
            DesignerInfoPropertySet designerInfoPropertySet =
                new DesignerInfoPropertySet(
                    designerInfo,
                    XElement.Parse(
                        "<DesignerInfoPropertySet xmlns='http://schemas.microsoft.com/ado/2009/11/edmx' />"));
            if (designerPropertyName != null)
            {
                DesignerProperty designerProperty =
                    new DesignerProperty(
                        designerInfoPropertySet,
                        XElement.Parse(
                            "<DesignerProperty Name='" + designerPropertyName + "' Value='" +
                            designerPropertyValue +
                            "' xmlns='http://schemas.microsoft.com/ado/2009/11/edmx' />"));
                designerInfoPropertySet.AddDesignerProperty(designerPropertyName, designerProperty);
            }

            designerInfo.PropertySet = designerInfoPropertySet;
            return designerInfo;
        }

        [TestMethod]
        public void UpdateConfigForSqlDbFileUpgrade_updates_and_saves_config()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(
                "<configuration>" +
                "  <connectionStrings>" +
                "    <add connectionString=\"Data source=.\\SQLExpress;AttachDbFilename=C:\\MyFolder\\MyDataFile.mdf;Database=dbname;Trusted_Connection=Yes;\" />" +
                "  </connectionStrings>" +
                "</configuration>");

            Mock<ConfigFileUtils> mockConfigFileUtils =
                new Mock<ConfigFileUtils>(Mock.Of<Project>(), Mock.Of<IServiceProvider>(), null, Mock.Of<IVsUtils>(), null);
            mockConfigFileUtils
                .Setup(u => u.LoadConfig())
                .Returns(xmlDoc);

            EdmUtils.UpdateConfigForSqlDbFileUpgrade(
                mockConfigFileUtils.Object,
                Mock.Of<Project>(),
                Mock.Of<IVsUpgradeLogger>());

            mockConfigFileUtils.Verify(u => u.SaveConfig(It.IsAny<XmlDocument>()), Times.Once());
        }

        [TestMethod]
        public void UpdateConfigForSqlDbFileUpgrade_does_not_save_config_if_content_not_loaded()
        {
            Mock<ConfigFileUtils> mockConfigFileUtils = new Mock<ConfigFileUtils>(
                Mock.Of<Project>(), Mock.Of<IServiceProvider>(), null, Mock.Of<IVsUtils>(), null);

            EdmUtils.UpdateConfigForSqlDbFileUpgrade(
                mockConfigFileUtils.Object,
                Mock.Of<Project>(),
                Mock.Of<IVsUpgradeLogger>());

            mockConfigFileUtils.Verify(u => u.SaveConfig(It.IsAny<XmlDocument>()), Times.Never());
        }

        [TestMethod]
        public void UpdateConfigForSqlDbFileUpgrade_logs_exceptions()
        {
            Mock<ConfigFileUtils> mockConfigFileUtils = new Mock<ConfigFileUtils>(
                Mock.Of<Project>(), Mock.Of<IServiceProvider>(), null, Mock.Of<IVsUtils>(), null);

            mockConfigFileUtils
                .Setup(u => u.LoadConfig())
                .Throws(new InvalidOperationException("Loading Failed"));

            Mock<IVsUpgradeLogger> mockLogger = new Mock<IVsUpgradeLogger>();

            EdmUtils.UpdateConfigForSqlDbFileUpgrade(
                mockConfigFileUtils.Object,
                Mock.Of<Project>(),
                mockLogger.Object);

            var expectedErrorMessage =
                string.Format(Resources.ErrorDuringSqlDatabaseFileUpgrade, null, "Loading Failed");

            mockLogger
                .Verify(l => l.LogMessage(2, It.IsAny<string>(), It.IsAny<string>(), expectedErrorMessage), Times.Once());
        }
    }
}
