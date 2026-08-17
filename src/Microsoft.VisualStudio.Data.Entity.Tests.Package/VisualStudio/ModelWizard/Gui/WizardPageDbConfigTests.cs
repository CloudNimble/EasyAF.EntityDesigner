// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Gui;
using Microsoft.VisualStudio.Data.Core;
using Moq;
using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using VSLangProj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Gui;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine;

namespace Microsoft.VisualStudio.Data.Entity.Tests.Package.VisualStudio.ModelWizard.Gui
{
    [TestClass]
    public class WizardPageDbConfigTests
    {
        static WizardPageDbConfigTests()
        {
            // The code below is required to avoid test failures due to:
            // Due to limitations in CLR, DynamicProxy was unable to successfully replicate non-inheritable attribute
            // System.Security.Permissions.UIPermissionAttribute on
            // Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Gui.WizardPageStart.ProcessDialogChar.
            // To avoid this error you can chose not to replicate this attribute type by calling
            // 'Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(typeof(System.Security.Permissions.UIPermissionAttribute))'.
            //
            // Note that the same pattern need to be used when creating tests for other wizard pages to avoid
            // issues related to the order the tests are run. Alternatively we could have code that is always being run
            // before any tests (e.g. a ctor of a class all test classes would derive from) where we would do that
            Castle.DynamicProxy.Generators
                .AttributesToAvoidReplicating
                .Add(typeof(System.Security.Permissions.UIPermissionAttribute));
        }

        [TestMethod]
        public void OnActivate_result_depends_on_FileAlreadyExistsError()
        {
            var wizard = ModelBuilderWizardFormHelper.CreateWizard();
            WizardPageDbConfig wizardPageDbConfig = new WizardPageDbConfig(wizard);

            wizard.FileAlreadyExistsError = true;
            wizardPageDbConfig.OnActivate().Should().BeFalse();

            wizard.FileAlreadyExistsError = false;
            wizardPageDbConfig.OnActivate().Should().BeTrue();
        }

        [TestMethod]
        public void GetTextBoxConnectionStringValue_returns_entity_connection_string_for_EDMX_DatabaseFirst()
        {
            Guid guid = new Guid("42424242-4242-4242-4242-424242424242");

            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            mockDte.SetProjectProperties(new Dictionary<string, object> { { "FullPath", @"C:\Project" } });
            Mock<ProjectItem> mockParentProjectItem = new Mock<ProjectItem>();
            mockParentProjectItem.Setup(p => p.Collection).Returns(Mock.Of<ProjectItems>());
            mockParentProjectItem.Setup(p => p.Name).Returns("Folder");

            Mock<ProjectItem> mockModelProjectItem = new Mock<ProjectItem>();
            Mock<ProjectItems> mockCollection = new Mock<ProjectItems>();
            mockCollection.Setup(p => p.Parent).Returns(mockParentProjectItem.Object);
            mockModelProjectItem.Setup(p => p.Collection).Returns(mockCollection.Object);

            WizardPageDbConfig wizardPageDbConfig =
                new WizardPageDbConfig(
                    ModelBuilderWizardFormHelper.CreateWizard(ModelGenerationOption.GenerateFromDatabase, mockDte.Project, @"C:\Project\myModel.edmx"));

            wizardPageDbConfig.GetTextBoxConnectionStringValue(
                CreateDataProviderManager(guid),
                guid,
                "Integrated Security=SSPI").Should().Be(
                "metadata=res://*/myModel.csdl|res://*/myModel.ssdl|res://*/myModel.msl;provider=System.Data.SqlClient;" +
                "provider connection string=\"integrated security=SSPI;MultipleActiveResultSets=True;App=EntityFramework\"");
        }

        [TestMethod]
        public void GetTextBoxConnectionStringValue_returns_entity_connection_string_for_EDMX_ModelFirst()
        {
            Guid guid = new Guid("42424242-4242-4242-4242-424242424242");

            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            mockDte.SetProjectProperties(new Dictionary<string, object> { { "FullPath", @"C:\Project" } });
            Mock<ProjectItem> mockParentProjectItem = new Mock<ProjectItem>();
            mockParentProjectItem.Setup(p => p.Collection).Returns(Mock.Of<ProjectItems>());
            mockParentProjectItem.Setup(p => p.Name).Returns("Folder");

            Mock<ProjectItem> mockModelProjectItem = new Mock<ProjectItem>();
            Mock<ProjectItems> mockCollection = new Mock<ProjectItems>();
            mockCollection.Setup(p => p.Parent).Returns(mockParentProjectItem.Object);
            mockModelProjectItem.Setup(p => p.Collection).Returns(mockCollection.Object);

            WizardPageDbConfig wizardPageDbConfig =
                new WizardPageDbConfig(
                    ModelBuilderWizardFormHelper.CreateWizard(ModelGenerationOption.GenerateDatabaseScript, mockDte.Project, @"C:\Project\myModel.edmx"));

            wizardPageDbConfig.GetTextBoxConnectionStringValue(
                CreateDataProviderManager(guid),
                guid,
                "Integrated Security=SSPI").Should().Be(
                "metadata=res://*/myModel.csdl|res://*/myModel.ssdl|res://*/myModel.msl;provider=System.Data.SqlClient;" +
                "provider connection string=\"integrated security=SSPI;MultipleActiveResultSets=True;App=EntityFramework\"");
        }

        [TestMethod]
        public void GetTextBoxConnectionStringValue_returns_regular_connection_string_for_CodeFirst_from_Database()
        {
            Guid guid = new Guid("42424242-4242-4242-4242-424242424242");
            WizardPageDbConfig wizardPageDbConfig = new WizardPageDbConfig(
                ModelBuilderWizardFormHelper.CreateWizard(ModelGenerationOption.CodeFirstFromDatabase));

            wizardPageDbConfig.GetTextBoxConnectionStringValue(
                CreateDataProviderManager(guid),
                guid,
                "Integrated Security=SSPI").Should().Be(
                "integrated security=SSPI;MultipleActiveResultSets=True;App=EntityFramework");
        }

        private static IVsDataProviderManager CreateDataProviderManager(Guid vsDataProviderGuid)
        {
            Mock<IVsDataProvider> mockDataProvider = new Mock<IVsDataProvider>();
            mockDataProvider
                .Setup(p => p.GetProperty("InvariantName"))
                .Returns("System.Data.SqlClient");

            Mock<IVsDataProviderManager> mockProviderManager = new Mock<IVsDataProviderManager>();
            mockProviderManager
                .Setup(m => m.Providers)
                .Returns(new Dictionary<Guid, IVsDataProvider> { { vsDataProviderGuid, mockDataProvider.Object } });

            return mockProviderManager.Object;
        }
    }
}
