// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model
{
    [TestClass]
    public class ProviderNamesTests
    {
        [TestMethod]
        public void IsSqlServerProvider_returns_true_for_System_Data_SqlClient()
        {
            ProviderNames.IsSqlServerProvider("System.Data.SqlClient").Should().BeTrue();
        }

        [TestMethod]
        public void IsSqlServerProvider_returns_true_for_Microsoft_Data_SqlClient()
        {
            ProviderNames.IsSqlServerProvider("Microsoft.Data.SqlClient").Should().BeTrue();
        }

        [TestMethod]
        public void IsSqlServerProvider_returns_true_case_insensitive()
        {
            ProviderNames.IsSqlServerProvider("SYSTEM.DATA.SQLCLIENT").Should().BeTrue();
            ProviderNames.IsSqlServerProvider("microsoft.data.sqlclient").Should().BeTrue();
            ProviderNames.IsSqlServerProvider("System.Data.SQLCLIENT").Should().BeTrue();
        }

        [TestMethod]
        public void IsSqlServerProvider_returns_false_for_other_providers()
        {
            ProviderNames.IsSqlServerProvider("System.Data.SqlServerCe.4.0").Should().BeFalse();
            ProviderNames.IsSqlServerProvider("Oracle.ManagedDataAccess.Client").Should().BeFalse();
            ProviderNames.IsSqlServerProvider("Npgsql").Should().BeFalse();
            ProviderNames.IsSqlServerProvider("MySql.Data.MySqlClient").Should().BeFalse();
        }

        [TestMethod]
        public void IsSqlServerProvider_returns_false_for_null()
        {
            ProviderNames.IsSqlServerProvider(null).Should().BeFalse();
        }

        [TestMethod]
        public void IsSqlServerProvider_returns_false_for_empty_string()
        {
            ProviderNames.IsSqlServerProvider(string.Empty).Should().BeFalse();
        }

        [TestMethod]
        public void IsSqlServerProvider_returns_false_for_partial_match()
        {
            // Should not match partial provider names
            ProviderNames.IsSqlServerProvider("System.Data.SqlClient.Extended").Should().BeFalse();
            ProviderNames.IsSqlServerProvider("My.System.Data.SqlClient").Should().BeFalse();
        }

        [TestMethod]
        public void Constants_have_correct_values()
        {
            ProviderNames.SystemDataSqlClient.Should().Be("System.Data.SqlClient");
            ProviderNames.MicrosoftDataSqlClient.Should().Be("Microsoft.Data.SqlClient");
        }
    }
}
