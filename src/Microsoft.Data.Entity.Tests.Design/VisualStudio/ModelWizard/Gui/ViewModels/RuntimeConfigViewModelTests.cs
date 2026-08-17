// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Gui.ViewModels;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Gui.ViewModels
{
    [TestClass]
    public class RuntimeConfigViewModelTests
    {
        [TestMethod]
        public void Ctor_initializes_correctly_when_no_EF_and_no_modern_provider()
        {
            // With simplified logic: no EF installed and no modern provider = error
            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: false,
                isCodeFirst: false);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(RuntimeVersion.Latest);
            first.Disabled.Should().BeTrue();
            first.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Error);
            viewModel.Message.Should().Be(ModelWizardResources.RuntimeConfig_NoProvider);
            viewModel.HelpUrl.Should().Be(ModelWizardResources.RuntimeConfig_LearnProvidersUrl);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_installed_version_six()
        {
            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2,
                installedEntityFrameworkVersion: RuntimeVersion.Version6,
                isModernProviderAvailable: true,
                isCodeFirst: false);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(RuntimeVersion.Version6);
            first.Disabled.Should().BeFalse();
            first.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Skip);
            viewModel.Message.Should().BeNull();
            viewModel.HelpUrl.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_installed_version_over_six()
        {
            var targetNetFrameworkVersion = NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2;
            Version installedEntityFrameworkVersion = new Version(7, 0, 0, 0);
            var isModernProviderAvailable = true;
            var isCodeFirst = false;

            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion,
                installedEntityFrameworkVersion,
                isModernProviderAvailable,
                isCodeFirst);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(installedEntityFrameworkVersion);
            first.Disabled.Should().BeFalse();
            first.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Skip);
            viewModel.Message.Should().BeNull();
            viewModel.HelpUrl.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_installed_version_six_but_no_modern_provider()
        {
            var targetFrameworkVersion = NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2;
            Version installedEntityFrameworkVersion = new Version(7, 0, 0, 0);
            var isModernProviderAvailable = false;
            var isCodeFirst = false;

            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetFrameworkVersion,
                installedEntityFrameworkVersion,
                isModernProviderAvailable,
                isCodeFirst);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(installedEntityFrameworkVersion);
            first.Disabled.Should().BeTrue();
            first.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Error);
            viewModel.Message.Should().Be(ModelWizardResources.RuntimeConfig_SixInstalledButNoProvider);
            viewModel.HelpUrl.Should().Be(ModelWizardResources.RuntimeConfig_LearnProvidersUrl);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_modern_provider_available_no_EF_installed()
        {
            // With simplified logic: modern provider available, no EF installed = use latest
            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: true,
                isCodeFirst: false);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(RuntimeVersion.Latest);
            first.Disabled.Should().BeFalse();
            first.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Normal);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_codefirst_and_no_EF_installed_and_modern_provider_available()
        {
            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: true,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Skip);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);
            var efVersion = viewModel.EntityFrameworkVersions.Single();
            efVersion.Version.Should().Be(RuntimeVersion.Latest);
            efVersion.Disabled.Should().BeFalse();
            efVersion.IsDefault.Should().BeTrue();

            viewModel.Message.Should().BeNull();
            viewModel.HelpUrl.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_codefirst_and_no_EF_installed_and_modern_provider_not_available()
        {
            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: false,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Error);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);
            var efVersion = viewModel.EntityFrameworkVersions.Single();
            efVersion.Version.Should().Be(RuntimeVersion.Latest);
            efVersion.Disabled.Should().BeTrue();
            efVersion.IsDefault.Should().BeTrue();

            viewModel.Message.Should().Be(ModelWizardResources.RuntimeConfig_NoProvider);
            viewModel.HelpUrl.Should().Be(ModelWizardResources.RuntimeConfig_LearnProvidersUrl);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_codefirst_and_EF6_installed_and_modern_provider_available()
        {
            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2,
                installedEntityFrameworkVersion: RuntimeVersion.Version6,
                isModernProviderAvailable: true,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Skip);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);
            var efVersion = viewModel.EntityFrameworkVersions.Single();
            efVersion.Version.Should().Be(RuntimeVersion.Latest);
            efVersion.Disabled.Should().BeFalse();
            efVersion.IsDefault.Should().BeTrue();

            viewModel.Message.Should().BeNull();
            viewModel.HelpUrl.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_codefirst_and_EF6_installed_and_modern_provider_not_available()
        {
            RuntimeConfigViewModel viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_7_2,
                installedEntityFrameworkVersion: RuntimeVersion.Version6,
                isModernProviderAvailable: false,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Error);

            viewModel.EntityFrameworkVersions.Count().Should().Be(1);
            var efVersion = viewModel.EntityFrameworkVersions.Single();
            efVersion.Version.Should().Be(RuntimeVersion.Latest);
            efVersion.Disabled.Should().BeTrue();
            efVersion.IsDefault.Should().BeTrue();

            viewModel.Message.Should().Be(ModelWizardResources.RuntimeConfig_SixInstalledButNoProvider);
            viewModel.HelpUrl.Should().Be(ModelWizardResources.RuntimeConfig_LearnProvidersUrl);
        }
    }
}
