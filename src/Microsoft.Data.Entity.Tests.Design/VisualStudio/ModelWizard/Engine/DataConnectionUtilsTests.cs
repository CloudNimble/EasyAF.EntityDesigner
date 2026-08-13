// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.Data.Core;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.ModelWizard.engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VSLangProj;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Engine
{
    [TestClass]
    public class DataConnectionUtilsTests
    {
        [TestMethod]
        public void HasEntityFrameworkProvider_returns_true_when_has_legacy_provider()
        {
            Mock<IVsDataProvider> provider = new Mock<IVsDataProvider>();
            provider.Setup(p => p.GetProperty("InvariantName")).Returns("System.Data.SqlClient");
            Guid providerGuid = Guid.NewGuid();
            Dictionary<Guid, IVsDataProvider> providers = new Dictionary<Guid, IVsDataProvider> { { providerGuid, provider.Object } };
            Mock<IVsDataProviderManager> dataProviderManager = new Mock<IVsDataProviderManager>();
            dataProviderManager.SetupGet(m => m.Providers).Returns(providers);
            MockDTE dte = new MockDTE(".NETFramework,Version=v4.5");

            DataConnectionUtils.HasEntityFrameworkProvider(
                    dataProviderManager.Object,
                    providerGuid,
                    dte.Project,
                    dte.ServiceProvider)
                .Should().BeTrue();
        }

        [TestMethod]
        public void HasEntityFrameworkProvider_returns_false_when_no_adonet_provider_or_ef_reference()
        {
            Mock<IVsDataProvider> provider = new Mock<IVsDataProvider>();
            provider.Setup(p => p.GetProperty("InvariantName")).Returns("My.Fake.Provider");
            Guid providerGuid = Guid.NewGuid();
            Dictionary<Guid, IVsDataProvider> providers = new Dictionary<Guid, IVsDataProvider> { { providerGuid, provider.Object } };
            Mock<IVsDataProviderManager> dataProviderManager = new Mock<IVsDataProviderManager>();
            dataProviderManager.SetupGet(m => m.Providers).Returns(providers);
            MockDTE dte = new MockDTE(".NETFramework,Version=v4.5", references: Enumerable.Empty<Reference>());

            DataConnectionUtils.HasEntityFrameworkProvider(
                    dataProviderManager.Object,
                    providerGuid,
                    dte.Project,
                    dte.ServiceProvider)
                .Should().BeFalse();
        }

        [TestMethod]
        public void HasEntityFrameworkProvider_returns_false_when_no_legacy_provider_or_ef_reference()
        {
            Mock<IVsDataProvider> provider = new Mock<IVsDataProvider>();
            provider.Setup(p => p.GetProperty("InvariantName")).Returns("System.Data.OleDb");
            Guid providerGuid = Guid.NewGuid();
            Dictionary<Guid, IVsDataProvider> providers = new Dictionary<Guid, IVsDataProvider> { { providerGuid, provider.Object } };
            Mock<IVsDataProviderManager> dataProviderManager = new Mock<IVsDataProviderManager>();
            dataProviderManager.SetupGet(m => m.Providers).Returns(providers);
            MockDTE dte = new MockDTE(".NETFramework,Version=v4.5", references: Enumerable.Empty<Reference>());

            DataConnectionUtils.HasEntityFrameworkProvider(
                    dataProviderManager.Object,
                    providerGuid,
                    dte.Project,
                    dte.ServiceProvider)
                .Should().BeFalse();
        }
    }
}
