// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using SystemDataCommon = System.Data.Common;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    using System;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.SqlServer;
    using System.IO;
    using System.Security;
    using System.Xml;
    using Moq;
    using Moq.Protected;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class StoreSchemaConnectionFactoryTests
    {
        [TestMethod]
        public void Create_creates_valid_EntityConnection_and_returns_EF_version()
        {
            var mockProviderServices = SetupMockProviderServices();
            mockProviderServices
                .Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(SqlProviderServices.Instance.GetProviderManifest("2008"));

            var mockResolver = SetupMockResolver(mockProviderServices);

            foreach (var targetEFVersion in EntityFrameworkVersion.GetAllVersions())
            {
                Version actualEFVersion;

                var entityConnection =
                    new StoreSchemaConnectionFactory().Create(
                        mockResolver.Object,
                        "System.Data.SqlClient",
                        "Server=test",
                        targetEFVersion,
                        out actualEFVersion);

                entityConnection.Should().NotBeNull();
                var expectedVersion =
                    targetEFVersion == EntityFrameworkVersion.Version2
                        ? EntityFrameworkVersion.Version1
                        : targetEFVersion;

                actualEFVersion.Should().Be(expectedVersion);
            }
        }

        [TestMethod]
        public void Create_throws_ArgumentException_for_unrecognized_porvider_invariant_name()
        {
            const string providerInvariantName = "abc";

            Assert.Equal(
                string.Format(Resources_VersioningFacade.EntityClient_InvalidStoreProvider, providerInvariantName),
                Assert.Throws<ArgumentException>(
                    () => new StoreSchemaConnectionFactory().Create(
                        new Mock<IDbDependencyResolver>().Object,
                        providerInvariantName,
                        "connectionString",
                        new Version(1, 0, 0, 0))).Message);
        }

        [TestMethod]
        public void Create_creates_valid_EntityConnection()
        {
            var mockProviderServices = SetupMockProviderServices();
            mockProviderServices
                .Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(SqlProviderServices.Instance.GetProviderManifest("2008"));

            var mockResolver = SetupMockResolver(mockProviderServices);

            foreach (var efVersion in EntityFrameworkVersion.GetAllVersions())
            {
                var entityConnection =
                    new StoreSchemaConnectionFactory()
                        .Create(
                            mockResolver.Object,
                            "System.Data.SqlClient",
                            "Server=test",
                            efVersion);

                entityConnection.Should().NotBeNull();
            }
        }

        [TestMethod]
        public void Create_throws_ProviderIncompatibleException_for_invalid_schema_Ssdl()
        {
            var mockProviderServices = SetupMockProviderServices();
            var mockProviderManifest = new Mock<DbProviderManifest>();
            mockProviderManifest
                .Protected()
                .Setup<XmlReader>("GetDbInformation", ItExpr.Is<string>(s => s == DbProviderManifest.StoreSchemaDefinitionVersion3))
                .Returns(XmlReader.Create(new StringReader("<root />")));

            mockProviderServices
                .Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(mockProviderManifest.Object);

            var mockResolver = SetupMockResolver(mockProviderServices);

            (Assert.Throws<ProviderIncompatibleException>(
                    () => new StoreSchemaConnectionFactory().Create(
                        mockResolver.Object,
                        "System.Data.SqlClient",
                        "Server=test",
                        EntityFrameworkVersion.Version3)).Message.StartsWith("Schema specified is not valid. Errors"));
        }

        [TestMethod]
        public void Create_throws_ProviderIncompatibleException_for_invalid_schema_Msl()
        {
            var mockProviderServices = SetupMockProviderServices();
            var mockProviderManifest = new Mock<DbProviderManifest>();

            mockProviderManifest
                .Protected()
                .Setup<XmlReader>("GetDbInformation", ItExpr.Is<string>(s => s != DbProviderManifest.StoreSchemaMappingVersion3))
                .Returns(
                    SqlProviderServices.Instance.GetProviderManifest("2008").GetInformation(DbProviderManifest.StoreSchemaMappingVersion3));

            mockProviderManifest
                .Protected()
                .Setup<XmlReader>("GetDbInformation", ItExpr.Is<string>(s => s == DbProviderManifest.StoreSchemaMappingVersion3))
                .Returns(XmlReader.Create(new StringReader("<root />")));

            mockProviderServices
                .Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(mockProviderManifest.Object);

            var mockResolver = SetupMockResolver(mockProviderServices);

            (Assert.Throws<ProviderIncompatibleException>(
                    () => new StoreSchemaConnectionFactory().Create(
                        mockResolver.Object,
                        "System.Data.SqlClient",
                        "Server=test",
                        EntityFrameworkVersion.Version3)).Message.StartsWith("Schema specified is not valid. Errors"));
        }

        private static Mock<DbProviderServices> SetupMockProviderServices()
        {
            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Protected()
                .Setup<string>("GetDbProviderManifestToken", ItExpr.IsAny<SystemDataCommon.DbConnection>())
                .Returns("2008");

            return mockProviderServices;
        }

        private static Mock<IDbDependencyResolver> SetupMockResolver(Mock<DbProviderServices> mockProviderServices)
        {
            var mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver
                .Setup(
                    r => r.GetService(
                        It.Is<Type>(t => t == typeof(DbProviderServices)),
                        It.IsAny<string>()))
                .Returns(mockProviderServices.Object);
            return mockResolver;
        }

        [TestMethod]
        public void IsCatchableExceptionType_filters_exceptions_correctly()
        {
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new StackOverflowException(.Should().BeFalse()));
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new OutOfMemoryException(.Should().BeFalse()));
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new NullReferenceException(.Should().BeFalse()));
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new AccessViolationException(.Should().BeFalse()));
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new SecurityException(.Should().BeFalse()));

            StoreSchemaConnectionFactory.IsCatchableExceptionType(new Exception(.Should().BeTrue()));
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new InvalidOperationException(.Should().BeTrue()));
        }
    }
}
