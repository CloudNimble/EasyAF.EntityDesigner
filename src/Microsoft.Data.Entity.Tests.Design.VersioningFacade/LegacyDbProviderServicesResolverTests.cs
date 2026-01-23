// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    using System;
    using System.Data.Entity.Core.Common;
    using Microsoft.Data.Entity.Design.VersioningFacade.LegacyProviderWrapper;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class LegacyDbProviderServicesResolverTests
    {
        [TestMethod]
        public void LegacyDbProviderServicesResolver_creates_wrapper_for_legacy_providers()
        {
            Assert.IsType<LegacyDbProviderServicesWrapper>(
                new LegacyDbProviderServicesResolver().GetService(typeof(DbProviderServices), "System.Data.SqlClient"));
        }

        [TestMethod]
        public void DefaultDbProviderServicesResolver_returns_null_for_unknown_type()
        {
            Assert.Null(
                new LegacyDbProviderServicesResolver().GetService(typeof(Object), "System.Data.SqlClient"));
        }

        [TestMethod]
        public void DefaultDbProviderServicesResolver_returns_null_for_non_string_key()
        {
            Assert.Null(
                new LegacyDbProviderServicesResolver().GetService(typeof(DbProviderServices), new object()));
        }

        [TestMethod]
        public void LegacyDbProviderServicesResolver_returns_null_for_MicrosoftDataSqlClient()
        {
            // Microsoft.Data.SqlClient should NOT be wrapped by LegacyDbProviderServicesWrapper
            // because it doesn't implement the legacy System.Data.Common.DbProviderServices interface.
            // Instead, it should be handled by the pre-registered SqlProviderServices in DependencyResolver.
            Assert.Null(
                new LegacyDbProviderServicesResolver().GetService(typeof(DbProviderServices), "Microsoft.Data.SqlClient"));
        }

        [TestMethod]
        public void LegacyDbProviderServicesResolver_returns_null_for_MicrosoftDataSqlClient_case_insensitive()
        {
            Assert.Null(
                new LegacyDbProviderServicesResolver().GetService(typeof(DbProviderServices), "microsoft.data.sqlclient"));
            Assert.Null(
                new LegacyDbProviderServicesResolver().GetService(typeof(DbProviderServices), "MICROSOFT.DATA.SQLCLIENT"));
        }
    }
}
