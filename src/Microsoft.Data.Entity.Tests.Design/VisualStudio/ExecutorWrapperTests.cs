// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    using System;
    using System.Data.Entity.SqlServer;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class ExecutorWrapperTests
    {
        [TestMethod]
        public void GetProviderServices_returns_assembly_qualified_type_name()
        {
            var domain = AppDomain.CreateDomain("ExecutorWrapperTests", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var executor = new ExecutorWrapper(
                    domain,
                    Path.GetFileName(GetType().Assembly.CodeBase));

                var typeName = executor.GetProviderServices("System.Data.SqlClient");

                typeName.Should().Be(typeof(SqlProviderServices).AssemblyQualifiedName);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        [TestMethod]
        public void GetProviderServices_returns_null_when_unknown()
        {
            var domain = AppDomain.CreateDomain("ExecutorWrapperTests", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var executor = new ExecutorWrapper(
                    domain,
                    Path.GetFileName(GetType().Assembly.CodeBase));

                executor.GetProviderServices("My.Fake.Provider".Should().BeNull());
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
    }
}
