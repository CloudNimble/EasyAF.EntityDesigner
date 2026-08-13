// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class ExecutorWrapperTests
    {
        [TestMethod]
        public void GetProviderServices_returns_assembly_qualified_type_name()
        {
            AppDomain domain = AppDomain.CreateDomain("ExecutorWrapperTests", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                ExecutorWrapper executor = new ExecutorWrapper(
                    domain,
                    Path.GetFileName(GetType().Assembly.CodeBase));

                var typeName = executor.GetProviderServices("System.Data.SqlClient");

                // Use reflection to get SqlProviderServices type since compile assets are excluded
                Type sqlProviderServicesType = Type.GetType(
                    "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer",
                    throwOnError: true);
                typeName.Should().Be(sqlProviderServicesType.AssemblyQualifiedName);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        [TestMethod]
        public void GetProviderServices_returns_null_when_unknown()
        {
            AppDomain domain = AppDomain.CreateDomain("ExecutorWrapperTests", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                ExecutorWrapper executor = new ExecutorWrapper(
                    domain,
                    Path.GetFileName(GetType().Assembly.CodeBase));

                executor.GetProviderServices("My.Fake.Provider").Should().BeNull();
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
    }
}
