// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using SystemDataCommon = System.Data.Common;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.IO;
using Microsoft.Data.Entity.Design.VersioningFacade;
using System.Security;
using System.Xml;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Moq;
using Moq.Protected;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
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
                .Returns(Utils.SqlProviderServicesInstance.GetProviderManifest("2008"));

            var mockResolver = SetupMockResolver(mockProviderServices);

            foreach (var targetEFVersion in EntityFrameworkVersion.GetAllVersions())
            {

                var entityConnection =
                    new StoreSchemaConnectionFactory().Create(
                        mockResolver.Object,
                        "System.Data.SqlClient",
                        "Server=test",
                        targetEFVersion,
                        out Version actualEFVersion);

                entityConnection.Should().NotBeNull();
                // Since only Version3 is supported, the actual version should match the target version
                actualEFVersion.Should().Be(targetEFVersion);
            }
        }

        [TestMethod]
        public void Create_throws_ArgumentException_for_unrecognized_porvider_invariant_name()
        {
            const string providerInvariantName = "abc";

            Action act = () => new StoreSchemaConnectionFactory().Create(
                new Mock<IDbDependencyResolver>().Object,
                providerInvariantName,
                "connectionString",
                new Version(1, 0, 0, 0));

            act.Should().Throw<ArgumentException>()
                .WithMessage(string.Format(Resources_VersioningFacade.EntityClient_InvalidStoreProvider, providerInvariantName));
        }

        [TestMethod]
        public void Create_creates_valid_EntityConnection()
        {
            var mockProviderServices = SetupMockProviderServices();
            mockProviderServices
                .Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(Utils.SqlProviderServicesInstance.GetProviderManifest("2008"));

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
            Mock<DbProviderManifest> mockProviderManifest = new Mock<DbProviderManifest>();
            mockProviderManifest
                .Protected()
                .Setup<XmlReader>("GetDbInformation", ItExpr.Is<string>(s => s == DbProviderManifest.StoreSchemaDefinitionVersion3))
                .Returns(XmlReader.Create(new StringReader("<root />")));

            mockProviderServices
                .Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(mockProviderManifest.Object);

            var mockResolver = SetupMockResolver(mockProviderServices);

            Action act = () => new StoreSchemaConnectionFactory().Create(
                mockResolver.Object,
                "System.Data.SqlClient",
                "Server=test",
                EntityFrameworkVersion.Version3);

            act.Should().Throw<ProviderIncompatibleException>()
                .Where(ex => ex.Message.StartsWith("Schema specified is not valid. Errors"));
        }

        [TestMethod]
        public void Create_throws_ProviderIncompatibleException_for_invalid_schema_Msl()
        {
            var mockProviderServices = SetupMockProviderServices();
            Mock<DbProviderManifest> mockProviderManifest = new Mock<DbProviderManifest>();

            mockProviderManifest
                .Protected()
                .Setup<XmlReader>("GetDbInformation", ItExpr.Is<string>(s => s != DbProviderManifest.StoreSchemaMappingVersion3))
                .Returns(
                    Utils.SqlProviderServicesInstance.GetProviderManifest("2008").GetInformation(DbProviderManifest.StoreSchemaMappingVersion3));

            mockProviderManifest
                .Protected()
                .Setup<XmlReader>("GetDbInformation", ItExpr.Is<string>(s => s == DbProviderManifest.StoreSchemaMappingVersion3))
                .Returns(XmlReader.Create(new StringReader("<root />")));

            mockProviderServices
                .Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(mockProviderManifest.Object);

            var mockResolver = SetupMockResolver(mockProviderServices);

            Action act = () => new StoreSchemaConnectionFactory().Create(
                mockResolver.Object,
                "System.Data.SqlClient",
                "Server=test",
                EntityFrameworkVersion.Version3);

            act.Should().Throw<ProviderIncompatibleException>()
                .Where(ex => ex.Message.StartsWith("Schema specified is not valid. Errors"));
        }

        private static Mock<DbProviderServices> SetupMockProviderServices()
        {
            Mock<DbProviderServices> mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Protected()
                .Setup<string>("GetDbProviderManifestToken", ItExpr.IsAny<SystemDataCommon.DbConnection>())
                .Returns("2008");

            return mockProviderServices;
        }

        private static Mock<IDbDependencyResolver> SetupMockResolver(Mock<DbProviderServices> mockProviderServices)
        {
            Mock<IDbDependencyResolver> mockResolver = new Mock<IDbDependencyResolver>();
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
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new StackOverflowException()).Should().BeFalse();
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new OutOfMemoryException()).Should().BeFalse();
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new NullReferenceException()).Should().BeFalse();
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new AccessViolationException()).Should().BeFalse();
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new SecurityException()).Should().BeFalse();

            StoreSchemaConnectionFactory.IsCatchableExceptionType(new Exception()).Should().BeTrue();
            StoreSchemaConnectionFactory.IsCatchableExceptionType(new InvalidOperationException()).Should().BeTrue();
        }
    }
}
