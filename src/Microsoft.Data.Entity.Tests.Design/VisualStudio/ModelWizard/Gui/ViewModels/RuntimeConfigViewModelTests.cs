// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Gui.ViewModels
{
    using System;
    using System.Linq;
    using Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Properties;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class RuntimeConfigViewModelTests
    {
        [TestMethod]
        public void Ctor_initializes_correctly_when_net35()
        {
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion3_5,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: false, 
                isCodeFirst: false);

            Assert.Equal(2, viewModel.EntityFrameworkVersions.Count());

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(RuntimeVersion.Latest);
            first.Disabled.Should().BeTrue();
            first.IsDefault.Should().BeFalse();

            var last = viewModel.EntityFrameworkVersions.Last();
            last.Version.Should().Be(RuntimeVersion.Version1);
            last.Disabled.Should().BeFalse();
            last.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Normal);
            viewModel.Message.Should().Be(Resources.RuntimeConfig_Net35);
            viewModel.HelpUrl.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_installed_version_below_six()
        {
            var targetNetFrameworkVersion = NetFrameworkVersioningHelper.NetFrameworkVersion4;
            var installedEntityFrameworkVersion = new Version(4, 0, 0, 0);
            var isModernProviderAvailable = false;

            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion,
                installedEntityFrameworkVersion,
                isModernProviderAvailable,
                isCodeFirst: false);

            Assert.Equal(2, viewModel.EntityFrameworkVersions.Count());

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(RuntimeVersion.Latest);
            first.Disabled.Should().BeTrue();
            first.IsDefault.Should().BeFalse();

            var last = viewModel.EntityFrameworkVersions.Last();
            Assert.Equal(new Version(4, 4, 0, 0), last.Version);
            last.Disabled.Should().BeFalse();
            last.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Normal);
            viewModel.Message.Should().Be(Resources.RuntimeConfig_BelowSixInstalled);
            viewModel.HelpUrl.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_installed_version_six()
        {
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_5,
                installedEntityFrameworkVersion: RuntimeVersion.Version6,
                isModernProviderAvailable: true,
                isCodeFirst: false);

            Assert.Equal(1, viewModel.EntityFrameworkVersions.Count());

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
            var targetNetFrameworkVersion = NetFrameworkVersioningHelper.NetFrameworkVersion4_5;
            var installedEntityFrameworkVersion = new Version(7, 0, 0, 0);
            var isModernProviderAvailable = true;
            var isCodeFirst = false;

            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion,
                installedEntityFrameworkVersion,
                isModernProviderAvailable,
                isCodeFirst);

            Assert.Equal(1, viewModel.EntityFrameworkVersions.Count());

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
            var targetFrameworkVersion = NetFrameworkVersioningHelper.NetFrameworkVersion4;
            var installedEntityFrameworkVersion = new Version(7, 0, 0, 0);
            var isModernProviderAvailable = false;
            var isCodeFirst = false;

            var viewModel = new RuntimeConfigViewModel(
                targetFrameworkVersion,
                installedEntityFrameworkVersion,
                isModernProviderAvailable,
                isCodeFirst);

            Assert.Equal(1, viewModel.EntityFrameworkVersions.Count());

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(installedEntityFrameworkVersion);
            first.Disabled.Should().BeTrue();
            first.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Error);
            viewModel.Message.Should().Be(Resources.RuntimeConfig_SixInstalledButNoProvider);
            viewModel.HelpUrl.Should().Be(Resources.RuntimeConfig_LearnProvidersUrl);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_no_modern_provider()
        {
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: false,
                isCodeFirst: false);

            Assert.Equal(2, viewModel.EntityFrameworkVersions.Count());

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(RuntimeVersion.Latest);
            first.Disabled.Should().BeTrue();
            first.IsDefault.Should().BeFalse();

            var last = viewModel.EntityFrameworkVersions.Last();
            last.Version.Should().Be(RuntimeVersion.Version5Net40);
            last.Disabled.Should().BeFalse();
            last.IsDefault.Should().BeTrue();

            viewModel.State.Should().Be(RuntimeConfigState.Normal);
            viewModel.Message.Should().Be(Resources.RuntimeConfig_NoProvider);
            viewModel.HelpUrl.Should().Be(Resources.RuntimeConfig_LearnProvidersUrl);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_modern_provider()
        {
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_5,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: true,
                isCodeFirst: false);

            Assert.Equal(2, viewModel.EntityFrameworkVersions.Count());

            var first = viewModel.EntityFrameworkVersions.First();
            first.Version.Should().Be(RuntimeVersion.Latest);
            first.Disabled.Should().BeFalse();
            first.IsDefault.Should().BeTrue();

            var last = viewModel.EntityFrameworkVersions.Last();
            last.Version.Should().Be(RuntimeVersion.Version5Net45);
            last.Disabled.Should().BeFalse();
            last.IsDefault.Should().BeFalse();

            viewModel.State.Should().Be(RuntimeConfigState.Normal);
            viewModel.Message.Should().Be(Resources.RuntimeConfig_TargetingHint);
            viewModel.HelpUrl.Should().Be(Resources.RuntimeConfig_LearnTargetingUrl);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_codefirst_and_no_EF_installed_and_modern_provider_available()
        {
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_5,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: true,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Skip);

            Assert.Equal(1, viewModel.EntityFrameworkVersions.Count());
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
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_5,
                installedEntityFrameworkVersion: null,
                isModernProviderAvailable: false,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Error);

            Assert.Equal(1, viewModel.EntityFrameworkVersions.Count());
            var efVersion = viewModel.EntityFrameworkVersions.Single();
            efVersion.Version.Should().Be(RuntimeVersion.Latest);
            efVersion.Disabled.Should().BeTrue();
            efVersion.IsDefault.Should().BeTrue();

            viewModel.Message.Should().Be(Resources.RuntimeConfig_NoProvider);
            viewModel.HelpUrl.Should().Be(Resources.RuntimeConfig_LearnProvidersUrl);
        }

        [TestMethod]
        public void Ctor_initializes_correctly_when_codefirst_and_EF6_installed_and_modern_provider_available()
        {
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_5,
                installedEntityFrameworkVersion: RuntimeVersion.Version6,
                isModernProviderAvailable: true,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Skip);

            Assert.Equal(1, viewModel.EntityFrameworkVersions.Count());
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
            var viewModel = new RuntimeConfigViewModel(
                targetNetFrameworkVersion: NetFrameworkVersioningHelper.NetFrameworkVersion4_5,
                installedEntityFrameworkVersion: RuntimeVersion.Version6,
                isModernProviderAvailable: false,
                isCodeFirst: true);

            viewModel.State.Should().Be(RuntimeConfigState.Error);

            Assert.Equal(1, viewModel.EntityFrameworkVersions.Count());
            var efVersion = viewModel.EntityFrameworkVersions.Single();
            efVersion.Version.Should().Be(RuntimeVersion.Latest);
            efVersion.Disabled.Should().BeTrue();
            efVersion.IsDefault.Should().BeTrue();

            viewModel.Message.Should().Be(Resources.RuntimeConfig_SixInstalledButNoProvider);
            viewModel.HelpUrl.Should().Be(Resources.RuntimeConfig_LearnProvidersUrl);
        }
    }
}
