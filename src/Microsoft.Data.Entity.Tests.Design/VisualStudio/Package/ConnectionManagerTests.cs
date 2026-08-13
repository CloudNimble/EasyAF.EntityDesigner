// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml;
using EnvDTE;
using Microsoft.VisualStudio.Data.Core;
using Microsoft.VisualStudio.DataTools.Interop;
using Microsoft.VSDesigner.Data.Local;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.Data.Entity.Design;
using VSLangProj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Package;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.Package
{
    [TestClass]
    public class ConnectionManagerTests
    {
        [TestMethod]
        public void InjectEFAttributesIntoConnectionString_adds_App_MARS_for_SqlServer()
        {
            ConnectionManager.InjectEFAttributesIntoConnectionString("Integrated Security=SSPI", "System.Data.SqlClient")
                .Should().Be("integrated security=SSPI;MultipleActiveResultSets=True;App=EntityFramework");
        }

        [TestMethod]
        public void
            InjectEFAttributesIntoConnectionString_does_not_touch_connection_string_if_not_SqlServer()
        {
            const string connectionString = "dummy";
            ConnectionManager.InjectEFAttributesIntoConnectionString(connectionString, "fakeProvider")
                .Should().BeSameAs(connectionString);
        }

        [TestMethod]
        public void
            InjectEFAttributesIntoConnectionString_does_not_app_App_MARS_to_connection_string_if_they_already_exist()
        {
            ConnectionManager.InjectEFAttributesIntoConnectionString(
                    "Integrated Security=SSPI;MultipleActiveResultSets=True;App=XYZ", "System.Data.SqlClient")
                .Should().Be("integrated security=SSPI;multipleactiveresultsets=True;app=XYZ");
        }

        [TestMethod]
        public void
            InjectEFAttributesIntoConnectionString_does_not_add_App_attribute_to_connection_string_if_Application_Name_attribute_already_exists()
        {
            ConnectionManager.InjectEFAttributesIntoConnectionString(
                    "Integrated Security=SSPI;MultipleActiveResultSets=True;Application Name=XYZ", "System.Data.SqlClient")
                .Should().Be("integrated security=SSPI;multipleactiveresultsets=True;application name=XYZ");
        }

        [TestMethod]
        public void InjectEFAttributesIntoConnectionString_returns_same_connection_string_if_it_is_invalid()
        {
            const string connectionString = "dummy";

            ConnectionManager.InjectEFAttributesIntoConnectionString(connectionString, "System.Data.SqlClient")
                .Should().BeSameAs(connectionString);
        }

        [TestMethod]
        public void GetMetadataFileNamesFromArtifactFileName_creates_metadata_file_names_for_non_null_edmx_ProjectItem()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            mockDte.SetProjectProperties(new Dictionary<string, object> { { "FullPath", @"D:\Projects\Project\Folder" } });
            Mock<ProjectItem> mockParentProjectItem = new Mock<ProjectItem>();
            mockParentProjectItem.Setup(p => p.Collection).Returns(Mock.Of<ProjectItems>());
            mockParentProjectItem.Setup(p => p.Name).Returns("Folder");

            Mock<ProjectItem> mockModelProjectItem = new Mock<ProjectItem>();
            Mock<ProjectItems> mockCollection = new Mock<ProjectItems>();
            mockCollection.Setup(p => p.Parent).Returns(mockParentProjectItem.Object);
            mockModelProjectItem.Setup(p => p.Collection).Returns(mockCollection.Object);

            var metadataFileNames =
                ConnectionManager.GetMetadataFileNamesFromArtifactFileName(
                mockDte.Project, @"c:\temp\myModel.edmx", mockDte.ServiceProvider, (_, __) => mockModelProjectItem.Object);

            metadataFileNames[0].Should().Be(@".\Folder\myModel.csdl");
            metadataFileNames[1].Should().Be(@".\Folder\myModel.ssdl");
            metadataFileNames[2].Should().Be(@".\Folder\myModel.msl");
        }

        [TestMethod]
        public void GetMetadataFileNamesFromArtifactFileName_creates_metadata_file_names_for_null_edmx_ProjectItem()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            mockDte.SetProjectProperties(new Dictionary<string, object> { { "FullPath", @"C:\Projects\Project\Folder" } });

            var metadataFileNames =
                ConnectionManager.GetMetadataFileNamesFromArtifactFileName(
                mockDte.Project, @"c:\temp\myModel.edmx", mockDte.ServiceProvider, (_, __) => null);

            metadataFileNames[0].Should().Be(@".\..\..\..\temp\myModel.csdl");
            metadataFileNames[1].Should().Be(@".\..\..\..\temp\myModel.ssdl");
            metadataFileNames[2].Should().Be(@".\..\..\..\temp\myModel.msl");
        }

        [TestMethod]
        public void TranslateConnectionString_returns_connectionstring_if_converter_service_not_available()
        {
            const string connString = "fakeConnString";

            ConnectionManager.TranslateConnectionStringFromDesignTime(
                    Mock.Of<IServiceProvider>(), Mock.Of<Project>(), "invariantName", connString)
                .Should().BeSameAs(connString);

            ConnectionManager.TranslateConnectionStringFromRunTime(
                    Mock.Of<IServiceProvider>(), Mock.Of<Project>(), "invariantName", connString)
                .Should().BeSameAs(connString);
        }

        [TestMethod]
        public void TranslateConnectionString_returns_connection_string_if_connection_string_null_or_empty()
        {
            ConnectionManager.TranslateConnectionStringFromDesignTime(
                    Mock.Of<IServiceProvider>(), Mock.Of<Project>(), "invariantName", null)
                .Should().BeNull();

            ConnectionManager.TranslateConnectionStringFromRunTime(
                    Mock.Of<IServiceProvider>(), Mock.Of<Project>(), "invariantName", string.Empty)
                .Should().BeSameAs(string.Empty);
        }

        [TestMethod]
        public void TranslateConnectionString_can_translate_designtime_connectionstring_to_runtime_connectionstring()
        {
            const string runtimeConnString = "runtimeConnString";

            Mock<IConnectionStringConverterService> mockConverter = new Mock<IConnectionStringConverterService>();
            mockConverter
                .Setup(c => c.ToRunTime(It.IsAny<Project>(), It.IsAny<string>(), "My.Db"))
                .Returns(runtimeConnString);

            Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IConnectionStringConverterService)))
                .Returns(mockConverter.Object);

            ConnectionManager.TranslateConnectionStringFromDesignTime(
                    mockServiceProvider.Object, Mock.Of<Project>(), "My.Db", "designTimeConnString")
                .Should().BeSameAs(runtimeConnString);
        }

        [TestMethod]
        public void TranslateConnectionString_can_translate_runtime_connectionstring_to_designtime_connectionstring()
        {
            const string designTimeConnString = "designTimeConnString";

            Mock<IConnectionStringConverterService> mockConverter = new Mock<IConnectionStringConverterService>();
            mockConverter
                .Setup(c => c.ToDesignTime(It.IsAny<Project>(), It.IsAny<string>(), "My.Db"))
                .Returns(designTimeConnString);

            Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IConnectionStringConverterService)))
                .Returns(mockConverter.Object);

            ConnectionManager.TranslateConnectionStringFromRunTime(
                    mockServiceProvider.Object, Mock.Of<Project>(), "My.Db", "runtimeTimeConnString")
                .Should().BeSameAs(designTimeConnString);
        }

        [TestMethod]
        public void TranslateConnectionString_handles_ConnectionStringConverterServiceException_from_translation()
        {
            ConnectionStringConverterServiceException converterException =
                (ConnectionStringConverterServiceException)
                typeof(ConnectionStringConverterServiceException).GetConstructor(
                    BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null)
                    .Invoke(new object[0]);

            Mock<IConnectionStringConverterService> mockConverter = new Mock<IConnectionStringConverterService>();
            mockConverter
                .Setup(c => c.ToDesignTime(It.IsAny<Project>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(converterException);
            mockConverter
                .Setup(c => c.ToRunTime(It.IsAny<Project>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(converterException);

            Mock<IVsDataProvider> mockDataProvider = new Mock<IVsDataProvider>();
            mockDataProvider
                .Setup(p => p.GetProperty("InvariantName"))
                .Returns("My.Db");

            Mock<IVsDataProviderManager> mockProviderManager = new Mock<IVsDataProviderManager>();
            mockProviderManager
                .Setup(m => m.Providers)
                .Returns(new Dictionary<Guid, IVsDataProvider> { { Guid.Empty, mockDataProvider.Object } });

            Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IConnectionStringConverterService)))
                .Returns(mockConverter.Object);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IVsDataProviderManager)))
                .Returns(mockProviderManager.Object);

            Action actRuntime = () => ConnectionManager.TranslateConnectionStringFromRunTime(mockServiceProvider.Object,
                Mock.Of<Project>(), "My.Db", "connectionString");
            actRuntime.Should().Throw<ArgumentException>()
                .WithMessage(string.Format(Resources.CannotTranslateRuntimeConnectionString, string.Empty, "connectionString"));

            Action actDesignTime = () => ConnectionManager.TranslateConnectionStringFromDesignTime(mockServiceProvider.Object,
                Mock.Of<Project>(), "My.Db", "connectionString");
            actDesignTime.Should().Throw<ArgumentException>()
                .WithMessage(string.Format(Resources.CannotTranslateDesignTimeConnectionString, string.Empty, "connectionString"));
        }

        [TestMethod]
        public void TranslateConnectionString_handles_checks_if_DDEX_provider_installed_when_handling_ConnectionStringConverterServiceException()
        {
            ConnectionStringConverterServiceException converterException =
                (ConnectionStringConverterServiceException)
                typeof(ConnectionStringConverterServiceException).GetConstructor(
                    BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null)
                    .Invoke(new object[0]);

            Mock<IConnectionStringConverterService> mockConverter = new Mock<IConnectionStringConverterService>();
            mockConverter
                .Setup(c => c.ToDesignTime(It.IsAny<Project>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(converterException);
            mockConverter
                .Setup(c => c.ToRunTime(It.IsAny<Project>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(converterException);

            Mock<IVsDataProviderManager> mockProviderManager = new Mock<IVsDataProviderManager>();
            mockProviderManager
                .Setup(m => m.Providers)
                .Returns(new Dictionary<Guid, IVsDataProvider>());

            // this is to ensure that even if the translation of the provider invariant name succeeded
            // we will use the runtime provider invariant name in the message
            Mock<IDTAdoDotNetProviderMapper2> mockProviderMapper = new Mock<IDTAdoDotNetProviderMapper2>();
            mockProviderMapper
                .Setup(m => m.MapRuntimeInvariantToInvariantName("My.Db", It.IsAny<string>(), It.IsAny<bool>()))
                .Returns("My.Db.DesignTime");

            Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IConnectionStringConverterService)))
                .Returns(mockConverter.Object);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IVsDataProviderManager)))
                .Returns(mockProviderManager.Object);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IDTAdoDotNetProviderMapper)))
                .Returns(mockProviderMapper.Object);

            var ddexNotInstalledMessage = string.Format(Resources.DDEXNotInstalled, "My.Db");

            Action actRuntime = () => ConnectionManager.TranslateConnectionStringFromRunTime(mockServiceProvider.Object,
                Mock.Of<Project>(), "My.Db", "connectionString");
            actRuntime.Should().Throw<ArgumentException>()
                .WithMessage(string.Format(Resources.CannotTranslateRuntimeConnectionString, ddexNotInstalledMessage, "connectionString"));

            mockProviderMapper
                .Verify(
                    m => m.MapRuntimeInvariantToInvariantName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
                    Times.Once());

            Action actDesignTime = () => ConnectionManager.TranslateConnectionStringFromDesignTime(mockServiceProvider.Object,
                Mock.Of<Project>(), "My.Db", "connectionString");
            actDesignTime.Should().Throw<ArgumentException>()
                .WithMessage(string.Format(Resources.CannotTranslateDesignTimeConnectionString, ddexNotInstalledMessage, "connectionString"));
        }

        [TestMethod]
        public void GetUniqueConnectionStringName_returns_candidate_connection_string_name_if_config_does_not_exist()
        {
            ConnectionManager.GetUniqueConnectionStringName(
                new Mock<ConfigFileUtils>(Mock.Of<Project>(), Mock.Of<IServiceProvider>(), null, Mock.Of<IVsUtils>(), null).Object,
                "myModel").Should().Be("myModel");
        }

        [TestMethod]
        public void GetUniqueConnectionStringName_uniquifies_proposed_connection_string_name()
        {
            XmlDocument configXml = new XmlDocument();
            configXml.LoadXml(@"<configuration>
  <connectionStrings>
    <add name=""myModel"" connectionString=""Data Source=(localdb)\v11.0;"" providerName=""System.Data.SqlClient"" />
    <add name=""myModel1"" connectionString=""metadata=res://*;"" providerName=""System.Data.EntityClient"" />
    <add name=""myModel2"" connectionString=""metadata=res://*;"" providerName=""System.Data.SqlCe"" />
  </connectionStrings>
</configuration>");

            Mock<ConfigFileUtils> mockConfig =
                new Mock<ConfigFileUtils>(Mock.Of<Project>(), Mock.Of<IServiceProvider>(), null, Mock.Of<IVsUtils>(), null);
            mockConfig
                .Setup(c => c.LoadConfig())
                .Returns(configXml);

            ConnectionManager.GetUniqueConnectionStringName(mockConfig.Object, "myModel").Should().Be("myModel3");
        }

        [TestMethod]
        public void CreateDefaultLocalDbConnectionString_returns_correct_default_connection_string()
        {
            // MSSQLLocalDB is the standard LocalDB instance name for VS2015+ (VS14 and later)
            ConnectionManager.CreateDefaultLocalDbConnectionString("App.MyContext")
                .Should().Be(@"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=App.MyContext;Integrated Security=True");
        }

        [TestMethod]
        public void AddConnectionStringElement_appends_connection_string()
        {
            XmlDocument configXml = new XmlDocument();
            configXml.LoadXml(@"<configuration>
  <connectionStrings>
    <add name=""myModel"" connectionString=""Data Source=(localdb)\v11.0;"" providerName=""System.Data.SqlClient"" />
  </connectionStrings>
</configuration>");

            ConnectionManager.AddConnectionStringElement(configXml, "MyDb", "db=mydb", "fancyDb");

            XmlElement addElement = configXml.SelectSingleNode("/configuration/connectionStrings/add[@name='MyDb']") as XmlElement;
            addElement.Should().NotBeNull();
            addElement.GetAttribute("connectionString").Should().Be("db=mydb");
            addElement.GetAttribute("providerName").Should().Be("fancyDb");
        }

        [TestMethod]
        public void AddConnectionStringElement_throws_if_config_invalid()
        {
            var configContents = new[]
            {
                @"<configuration1 />",
                @"<configuration xmlns=""fakexmlns""/>",
                @"<ns:configuration xmlns:ns=""fakexmlns""/>"
            };

            foreach (var config in configContents)
            {
                XmlDocument configXml = new XmlDocument();
                configXml.LoadXml(config);

                Action act = () => ConnectionManager.AddConnectionStringElement(configXml, "MyDb", "db=mydb", "fancyDb");
                act.Should().Throw<XmlException>().WithMessage(Resources.ConnectionManager_CorruptConfig);
            }
        }

        [TestMethod]
        public void UpdateEntityConnectionStringsInConfig_updates_config_correctly()
        {
            XmlDocument configXml = new XmlDocument();
            configXml.LoadXml(@"<configuration>
  <connectionStrings>
    <add name=""toBeRemoved"" connectionString=""Data Source=(localdb)\v11.0;"" providerName=""System.Data.EntityClient"" />
    <add name=""shouldNotBeTouched"" connectionString=""Data Source=(localdb)\v11.0;"" providerName=""System.Data.SqlClient"" />
  </connectionStrings>
</configuration>");

            Dictionary<string, ConnectionManager.ConnectionString> entityConnectionStrings = new Dictionary<string, ConnectionManager.ConnectionString>
            {
                {
                    "newEntityConnStr",
                    new ConnectionManager.ConnectionString(
                        "metadata=res://*/Model1.csdl|res://*/Model1.ssdl|res://*/Model1.msl;provider=System.Data.SqlClient;" +
                        "provider connection string=\"data source=(localdb)\v11.0;initial catalog=testDB;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework\"")
                }
            };

            ConnectionManager.UpdateEntityConnectionStringsInConfig(configXml, entityConnectionStrings);

            configXml.SelectSingleNode("/configuration/connectionStrings/add[@name = 'shouldNotBeTouched']").Should().NotBeNull();
            configXml.SelectSingleNode("/configuration/connectionStrings/add[@name = 'newEntityConnStr']").Should().NotBeNull();
            configXml.SelectSingleNode("/configuration/connectionStrings/add[@name = 'toBeRemoved']").Should().BeNull();
        }
    }
}
