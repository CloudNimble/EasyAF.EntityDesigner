// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Infrastructure;
using System.IO;
using EnvDTE;
using Microsoft.Data.Tools.XmlDesignerBase;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Xml;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Reflection;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.Design.Ide;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.ModelWizard;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.ModelWizard.Engine;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Validation;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard
{
    [TestClass]
    public class OneEFWizardTests
    {
        [TestMethod]
        public void RunFinished_should_not_add_connection_string_to_config_file_if_SaveConnectionStringInAppConfig_false()
        {
            Mock<ConfigFileUtils> mockConfig = new Mock<ConfigFileUtils>(Mock.Of<Project>(), Mock.Of<IServiceProvider>(), 
                VisualStudioProjectSystem.WindowsApplication, null, null);

            new OneEFWizard(mockConfig.Object, Mock.Of<IVsUtils>())
                .RunFinished(new ModelBuilderSettings { SaveConnectionStringInAppConfig = false}, null);

            mockConfig.Verify(m => m.GetOrCreateConfigFile(), Times.Never());
            mockConfig.Verify(m => m.LoadConfig(), Times.Never());
            mockConfig.Verify(m => m.SaveConfig(It.IsAny<XmlDocument>()), Times.Never());
        }

        [TestMethod]
        public void RunFinished_should_not_add_connection_string_to_config_file_if_config_file_already_contains_same_connection()
        {
            Mock<ConfigFileUtils> mockConfig = new Mock<ConfigFileUtils>(Mock.Of<Project>(), Mock.Of<IServiceProvider>(),
                VisualStudioProjectSystem.WindowsApplication, null, null);

            XmlDocument configXml = new XmlDocument();
            configXml.LoadXml(
                @"<configuration>" + Environment.NewLine +
                @"  <connectionStrings>" + Environment.NewLine +
                @"    <add name=""myConnStr"" connectionString=""data source=(localdb)\v11.0;initial catalog=App.MyContext;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework"" providerName=""System.Data.SqlClient"" />" + Environment.NewLine +
                @"  </connectionStrings>" + Environment.NewLine +
                @"</configuration>"
            );
            mockConfig.Setup(u => u.LoadConfig()).Returns(configXml);

            Mock<ModelBuilderSettings> mockSettings = new Mock<ModelBuilderSettings> { CallBase = true };
            mockSettings
                .Setup(s => s.AppConfigConnectionString)
                .Returns(@"data source=(localdb)\v11.0;initial catalog=App.MyContext;integrated security=True");
            mockSettings.Setup(s => s.RuntimeProviderInvariantName).Returns("System.Data.SqlClient");

            var settings = mockSettings.Object;
            settings.SaveConnectionStringInAppConfig = true;
            settings.AppConfigConnectionPropertyName = "myConnStr";

            new OneEFWizard(mockConfig.Object, new Mock<IVsUtils>().Object)
                .RunFinished(settings, null);

            mockConfig.Verify(m => m.GetOrCreateConfigFile(), Times.Once());
            mockConfig.Verify(m => m.LoadConfig(), Times.Once());
            mockConfig.Verify(m => m.SaveConfig(It.IsAny<XmlDocument>()), Times.Never());
        }

        [TestMethod]
        public void RunFinished_adds_EF_attributes_and_saves_connection_string_to_config_file()
        {
            Mock<ConfigFileUtils> mockConfig = new Mock<ConfigFileUtils>(Mock.Of<Project>(), Mock.Of<IServiceProvider>(),
                VisualStudioProjectSystem.WindowsApplication, null, null);

            XmlDocument configXml = new XmlDocument();
            configXml.LoadXml("<configuration />");
            mockConfig.Setup(u => u.LoadConfig()).Returns(configXml);

            Mock<ModelBuilderSettings> mockSettings = new Mock<ModelBuilderSettings> { CallBase = true };
            mockSettings
                .Setup(s => s.AppConfigConnectionString)
                .Returns(@"data source=(localdb)\v11.0;initial catalog=App.MyContext;integrated security=True");
            mockSettings.Setup(s => s.RuntimeProviderInvariantName).Returns("System.Data.SqlClient");

            var settings = mockSettings.Object;
            settings.SaveConnectionStringInAppConfig = true;
            settings.AppConfigConnectionPropertyName = "myConnStr";

            new OneEFWizard(mockConfig.Object, Mock.Of<IVsUtils>())
                .RunFinished(settings, null);

            mockConfig.Verify(m => m.GetOrCreateConfigFile(), Times.Once());
            mockConfig.Verify(m => m.LoadConfig(), Times.Exactly(2));
            mockConfig.Verify(m => m.SaveConfig(
                It.Is<XmlDocument>(config => config.SelectSingleNode("/configuration/connectionStrings/add[@name='myConnStr']/@connectionString").Value ==
                @"data source=(localdb)\v11.0;initial catalog=App.MyContext;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework")), 
                Times.Once());
        }

        [TestMethod]
        public void RunFinished_checks_out_files_and_creates_project_items()
        {
            Mock<ProjectItems> mockProjectItems = new Mock<ProjectItems>();
            Mock<Project> mockProject = new Mock<Project>();
            mockProject.Setup(p => p.ProjectItems).Returns(mockProjectItems.Object);

            Mock<IVsUtils> mockVsUtils = new Mock<IVsUtils>();
            ModelBuilderSettings settings = new ModelBuilderSettings {Project = mockProject.Object };
            Mock<ConfigFileUtils> mockConfig = new Mock<ConfigFileUtils>(Mock.Of<Project>(), Mock.Of<IServiceProvider>(),
                VisualStudioProjectSystem.WindowsApplication, null, null);

            List<KeyValuePair<string, string>> generatedCode = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("context", string.Empty),
                new KeyValuePair<string, string>(Path.GetFileName(Assembly.GetExecutingAssembly().Location), string.Empty),
                new KeyValuePair<string, string>(Path.GetRandomFileName(), string.Empty)
            };

            new OneEFWizard(configFileUtils: mockConfig.Object, vsUtils: mockVsUtils.Object, generatedCode: generatedCode)
                .RunFinished(settings, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            // need to Skip(1) since the first item is the DbContext file which is being added as a project item
            mockVsUtils.Verify(
                u => u.WriteCheckoutTextFilesInProject(
                    It.Is<Dictionary<string, object>>(
                        fileMap => fileMap.Keys.Select(Path.GetFileName)
                            .SequenceEqual(generatedCode.Skip(1).Select(i => i.Key)))));

            var existingFilePath = Assembly.GetExecutingAssembly().Location;
            mockProjectItems.Verify(i => i.AddFromFile(existingFilePath), Times.Once());

            // verify we only added the file that existed 
            mockProjectItems.Verify(i => i.AddFromFile(It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void RunStarted_saves_context_generated_code_replacementsDictionary_as_contextfilecontents()
        {
            Mock<CodeFirstModelGenerator> mockCodeGenerator = new Mock<CodeFirstModelGenerator>(MockDTE.CreateProject());
            mockCodeGenerator
                .Setup(g => g.Generate(It.IsAny<DbModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { new KeyValuePair<string, string>("MyContext", "context code") });

            ModelBuilderSettings modelBuilderSettings =
                new ModelBuilderSettings { SaveConnectionStringInAppConfig = true, AppConfigConnectionPropertyName = "ConnString" };

            Dictionary<string, string> replacementsDictionary =
                new Dictionary<string, string>
                {
                    { "$safeitemname$", "MyContext" },
                    { "$rootnamespace$", "Project.Data" }
                };

            new OneEFWizard(vsUtils:Mock.Of<IVsUtils>())
                .RunStarted(modelBuilderSettings, mockCodeGenerator.Object, replacementsDictionary);

            replacementsDictionary["$contextfilecontents$"].Should().Be("context code");
        }

        [TestMethod]
        public void RunStarted_uses_AppConfigConnectionPropertyName_if_SaveConnectionStringInAppConfig_true()
        {
            Mock<CodeFirstModelGenerator> mockCodeGenerator = new Mock<CodeFirstModelGenerator>(MockDTE.CreateProject());
            mockCodeGenerator
                .Setup(g => g.Generate(It.IsAny<DbModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { new KeyValuePair<string, string>() });

            ModelBuilderSettings modelBuilderSettings = 
                new ModelBuilderSettings { SaveConnectionStringInAppConfig = true, AppConfigConnectionPropertyName = "ConnString"};

            Dictionary<string, string> replacementsDictionary =
                new Dictionary<string, string>
                {
                    { "$safeitemname$", "MyContext" },
                    { "$rootnamespace$", "Project.Data" }
                };

            new OneEFWizard(vsUtils: Mock.Of<IVsUtils>())
                .RunStarted(modelBuilderSettings, mockCodeGenerator.Object, replacementsDictionary);

            mockCodeGenerator.Verify(g => g.Generate(It.IsAny<DbModel>(), "Project.Data", "MyContext", "ConnString"), Times.Once());
        }

        [TestMethod]
        public void RunStarted_uses_context_class_name_if_SaveConnectionStringInAppConfig_false()
        {
            Mock<CodeFirstModelGenerator> mockCodeGenerator = new Mock<CodeFirstModelGenerator>(MockDTE.CreateProject());
            mockCodeGenerator
                .Setup(g => g.Generate(It.IsAny<DbModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { new KeyValuePair<string, string>() });

            ModelBuilderSettings modelBuilderSettings =
                new ModelBuilderSettings { SaveConnectionStringInAppConfig = false, AppConfigConnectionPropertyName = "ConnString" };

            Dictionary<string, string> replacementsDictionary =
                new Dictionary<string, string>
                {
                    { "$safeitemname$", "MyContext" },
                    { "$rootnamespace$", "Project.Data" }
                };

            new OneEFWizard(vsUtils: Mock.Of<IVsUtils>())
                .RunStarted(modelBuilderSettings, mockCodeGenerator.Object, replacementsDictionary);

            mockCodeGenerator.Verify(g => g.Generate(It.IsAny<DbModel>(), "Project.Data", "MyContext", "MyContext"), Times.Once());
        }

        [TestMethod]
        public void ProjectItemFinishedGenerating_adds_errors_to_error_pane_if_any()
        {

            const string itemPath = @"C:\Project\MyContext.cs";

            CreateOneEFWizard(
                itemPath, 
                new[]
                {
                    new EdmSchemaError("error", 20, EdmSchemaErrorSeverity.Error),
                    new EdmSchemaError("warning", 10, EdmSchemaErrorSeverity.Warning)
                },
                out Mock<IErrorListHelper> mockErrorListHelper, 
                out Mock<ProjectItem> mockProjectItem)
                    .ProjectItemFinishedGenerating(mockProjectItem.Object);

            Func<ICollection<ErrorInfo>, bool> errorInfoCollectionVerification = c =>
            {
                if (c.Count != 2)
                {
                    return false;
                }

                var first = c.First();
                var second = c.Last();

                return c.All(i => i.ItemPath == itemPath && i.ErrorClass == ErrorClass.Runtime_All) &&
                    first.IsError() && first.Message == string.Format(Resources.Error_Message_With_Error_Code_Prefix, 20, "error") && first.ErrorCode == 20 &&
                    second.IsWarning() && second.Message == string.Format(Resources.Error_Message_With_Error_Code_Prefix, 10, "warning") && second.ErrorCode == 10;
            };

            mockErrorListHelper.Verify(
                h => h.AddErrorInfosToErrorList(
                    It.Is<ICollection<ErrorInfo>>(c => errorInfoCollectionVerification(c)),
                    It.IsAny<IVsHierarchy>(),
                    It.IsAny<uint>(),
                    false),
                Times.Once());
        }

        [TestMethod]
        public void ProjectItemFinishedGenerating_does_not_add_errors_to_error_pane_if_no_errors()
        {
            const string itemPath = @"C:\Project\MyContext.cs";


            CreateOneEFWizard(itemPath, null, out Mock<IErrorListHelper> mockErrorListHelper, out Mock<ProjectItem> mockProjectItem)
                .ProjectItemFinishedGenerating(mockProjectItem.Object);

            mockErrorListHelper.Verify(
                h => h.AddErrorInfosToErrorList(
                    It.IsAny<ICollection<ErrorInfo>>(),
                    It.IsAny<IVsHierarchy>(),
                    It.IsAny<uint>(),
                    false),
                Times.Never());
        }

        private static OneEFWizard CreateOneEFWizard(string itemPath, IEnumerable<EdmSchemaError> edmSchemaErrors, out Mock<IErrorListHelper> mockErrorListHelper, out Mock<ProjectItem> mockProjectItem)
        {
            Mock<DTE> mockDte = new Mock<DTE>();
            mockDte.As<IOleServiceProvider>();
            Mock<Project> mockProject = new Mock<Project>();
            mockProject.Setup(p => p.DTE).Returns(mockDte.Object);

            Mock<IVsUtils> mockVsUtils = new Mock<IVsUtils>();
            mockErrorListHelper = new Mock<IErrorListHelper>();
            ModelGenErrorCache errorCache = new ModelGenErrorCache();

            if (edmSchemaErrors != null)
            {
                errorCache.AddErrors(
                    itemPath,
                    edmSchemaErrors.ToList());
            }

            mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.Setup(p => p.get_FileNames(1)).Returns(itemPath);
            mockProjectItem.Setup(p => p.ContainingProject).Returns(mockProject.Object);

            return new OneEFWizard(
                vsUtils: mockVsUtils.Object, errorListHelper: mockErrorListHelper.Object, errorCache: errorCache);
        }

        [TestMethod]
        public void RunStarted_handles_CodeFirstModelGenerationException_and_shows_error_dialog()
        {
            var project = MockDTE.CreateProject();
            Mock<CodeFirstModelGenerator> mockCodeGenerator = new Mock<CodeFirstModelGenerator>(project);

            Exception innerException = new Exception("InnerException", new InvalidOperationException("nested InnerException"));

            mockCodeGenerator
                .Setup(g => g.Generate(It.IsAny<DbModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new CodeFirstModelGenerationException("Failed generating code.", innerException));

            Mock<IVsUtils> mockVsUtils = new Mock<IVsUtils>();

            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string>
            {
                { "$safeitemname$", "context.cs" },
                { "$rootnamespace$", "My.Namespace" }
            };

            new OneEFWizard(vsUtils: mockVsUtils.Object)
                .RunStarted(new ModelBuilderSettings(), mockCodeGenerator.Object, replacementsDictionary);

            mockVsUtils.Verify(u => u.ShowErrorDialog("Failed generating code.\r\n" + innerException), Times.Once());
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_false_if_code_could_not_be_generated()
        {
            new OneEFWizard(vsUtils: Mock.Of<IVsUtils>(), generatedCode: null)
                    .ShouldAddProjectItem(string.Empty).Should().BeFalse();

            new OneEFWizard(vsUtils: Mock.Of<IVsUtils>(), generatedCode: [])
                    .ShouldAddProjectItem(string.Empty).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldAddProjectItem_returns_true_if_code_could_be_generated()
        {
            new OneEFWizard(
                    vsUtils: Mock.Of<IVsUtils>(),
                    generatedCode: [new KeyValuePair<string, string>(string.Empty, string.Empty)])
                    .ShouldAddProjectItem(string.Empty).Should().BeTrue();
        }

        [TestMethod]
        public void RunStarted_passes_safeitemname_as_context_name_if_valid_identifier()
        {
            const string contextName = "MyContext";

            Mock<IVsUtils> mockVsUtils = new Mock<IVsUtils>();
            OneEFWizard wizard = new OneEFWizard(vsUtils: mockVsUtils.Object);

            Mock<CodeFirstModelGenerator> mockCodeGenerator = new Mock<CodeFirstModelGenerator>(MockDTE.CreateProject());
            mockCodeGenerator
                .Setup(g => g.Generate(It.IsAny<DbModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { new KeyValuePair<string, string>(string.Empty, string.Empty) });

            ModelBuilderSettings settings = new ModelBuilderSettings { VSApplicationType = VisualStudioProjectSystem.WebApplication };
            Dictionary<string, string> replacenentsDictionary = new Dictionary<string, string>
            {
                { "$safeitemname$", contextName },
                { "$rootnamespace$", "Project" }
            };

            wizard.RunStarted(settings, mockCodeGenerator.Object, replacenentsDictionary);

            mockCodeGenerator.Verify(
                g => g.Generate(
                    It.IsAny<DbModel>(),
                    "Project",
                    /*contextClassName*/ It.Is<string>(v => ReferenceEquals(v, contextName)),
                    /*connectionStringName*/ It.Is<string>(v => ReferenceEquals(v, contextName))),
            Times.Once());
        }

        [TestMethod]
        public void RunStarted_creates_valid_context_name_if_safeitemname_is_not_valid_identifier()
        {
            Mock<IVsUtils> mockVsUtils = new Mock<IVsUtils>();
            OneEFWizard wizard = new OneEFWizard(vsUtils: mockVsUtils.Object);

            Mock<CodeFirstModelGenerator> mockCodeGenerator = new Mock<CodeFirstModelGenerator>(MockDTE.CreateProject());
            mockCodeGenerator
                .Setup(g => g.Generate(It.IsAny<DbModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { new KeyValuePair<string, string>(string.Empty, string.Empty) });

            ModelBuilderSettings settings = new ModelBuilderSettings { VSApplicationType = VisualStudioProjectSystem.WebApplication };
            Dictionary<string, string> replacenentsDictionary = new Dictionary<string, string>
            {
                { "$safeitemname$", "3My.Con text" },
                { "$rootnamespace$", "Project" }
            };

            wizard.RunStarted(settings, mockCodeGenerator.Object, replacenentsDictionary);

            mockCodeGenerator.Verify(
                g => g.Generate(
                    It.IsAny<DbModel>(), 
                    "Project",
                    /*contextClassName*/ "_3MyContext",
                    /*connectionStringName*/ "_3MyContext"),
                Times.Once());
        }
    }
}