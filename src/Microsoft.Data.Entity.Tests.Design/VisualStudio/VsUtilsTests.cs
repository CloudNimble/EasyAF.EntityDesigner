// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using Microsoft.Data.Entity.Design.VersioningFacade;
    using Microsoft.VisualStudio.Shell.Design;
    using Microsoft.VisualStudio.Shell.Interop;
    using Moq;
    using Moq.Protected;
    using Microsoft.Data.Entity.Tests.Design.TestHelpers;
    using Microsoft.Data.Entity.Design.VisualStudio;
    using VSLangProj;
    using VSLangProj80;
    using VsWebSite;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using FluentAssertions;
    using DbProviderServices = System.Data.Entity.Core.Common.DbProviderServices;

    [TestClass]
    public class VsUtilsTests
    {
        [TestMethod]
        public void GetProjectReferenceAssemblyNames_returns_references()
        {
            var project = MockDTE.CreateProject(
                new[]
                    {
                        MockDTE.CreateReference("System.Data.Entity", "4.0.0.0"),
                        MockDTE.CreateReference("EntityFramework", "5.0.0.0")
                    });

            var referenceAssemblyNames = VsUtils.GetProjectReferenceAssemblyNames(project).ToArray();

            referenceAssemblyNames.Count().Should().Be(2);
            referenceAssemblyNames.Last().Key.Should().Be("EntityFramework");
            referenceAssemblyNames.Last().Value.Should().Be(new Version(5, 0, 0, 0));
        }

        [TestMethod]
        public void GetProjectReferenceAssemblyNames_returns_references_for_websites()
        {
            var project = MockDTE.CreateWebSite(
                new[]
                    {
                        MockDTE.CreateAssemblyReference(
                            "System.Data.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                        MockDTE.CreateAssemblyReference(
                            "EntityFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                    });

            var referenceAssemblyNames = VsUtils.GetProjectReferenceAssemblyNames(project).ToArray();

            referenceAssemblyNames.Count().Should().Be(2);
            referenceAssemblyNames.Last().Key.Should().Be("EntityFramework");
            referenceAssemblyNames.Last().Value.Should().Be(new Version(5, 0, 0, 0));
        }

        [TestMethod]
        public void GetProjectReferenceAssemblyNames_for_websites_ignores_references_with_badly_formed_strong_names()
        {
            var project = MockDTE.CreateWebSite(
                new[]
                    {
                        // Deliberately emptying the PublicKeyToken causes a situation where
                        // creating the AssemblyName throws. This test tests that the exception
                        // is ignored and the other 2 references are still returned.
                        // (See issue 1467).
                        MockDTE.CreateAssemblyReference(
                            "AspNet.ScriptManager.jQuery.UI.Combined, Version=1.8.20.0, Culture=neutral, PublicKeyToken="),
                        MockDTE.CreateAssemblyReference(
                            "System.Data.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                        MockDTE.CreateAssemblyReference(
                            "EntityFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                    });

            var referenceAssemblyNames = VsUtils.GetProjectReferenceAssemblyNames(project).ToArray();

            referenceAssemblyNames.Count().Should().Be(2);
            referenceAssemblyNames.Where(ran => ran.Key == "AspNet.ScriptManager.jQuery.UI.Combined").Count().Should().Be(0);
            referenceAssemblyNames.Where(ran => ran.Key == "System.Data.Entity").Count().Should().Be(1);
            referenceAssemblyNames.Where(ran => ran.Key == "EntityFramework").Count().Should().Be(1);
        }

        [TestMethod]
        public void GetProjectReferenceAssemblyNames_handles_duplicate_references()
        {
            var project = MockDTE.CreateWebSite(
                new[]
                    {
                        MockDTE.CreateAssemblyReference(
                            "System.Web.WebPages.Deployment, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"),
                        MockDTE.CreateAssemblyReference(
                            "System.Web.WebPages.Deployment, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35")
                    });

            var referenceAssemblyNames = VsUtils.GetProjectReferenceAssemblyNames(project).ToArray();

            referenceAssemblyNames.Count().Should().Be(2);
            referenceAssemblyNames.Last().Key.Should().Be("System.Web.WebPages.Deployment");
            referenceAssemblyNames.Last().Value.Should().Be(new Version(2, 0, 0, 0));
        }

        [TestMethod]
        public void AddProjectReference_adds_reference()
        {
            var vsReferences = new Mock<References>();
            var vsProject = new Mock<VSProject2>();
            vsProject.SetupGet(p => p.References).Returns(vsReferences.Object);

            var project = MockDTE.CreateProject();
            Mock.Get(project).Setup(p => p.Object).Returns(vsProject.Object);

            VsUtils.AddProjectReference(project, "System.Data");

            vsReferences.Verify(r => r.Add("System.Data"));
        }

        [TestMethod]
        public void AddProjectReference_adds_reference_for_websites()
        {
            var vsAssemblyReferences = new Mock<AssemblyReferences>();
            var vsWebSite = new Mock<VSWebSite>();
            vsWebSite.SetupGet(p => p.References).Returns(vsAssemblyReferences.Object);
            var project = MockDTE.CreateWebSite();
            Mock.Get(project).Setup(p => p.Object).Returns(vsWebSite.Object);

            VsUtils.AddProjectReference(project, "System.Data");

            vsAssemblyReferences.Verify(r => r.AddFromGAC("System.Data"));
        }

        [TestMethod]
        public void GetInstalledEntityFrameworkAssemblyVersion_returns_null_when_none()
        {
            var project = MockDTE.CreateProject(Enumerable.Empty<Reference>());

            VsUtils.GetInstalledEntityFrameworkAssemblyVersion(project).Should().BeNull();
        }

        [TestMethod]
        public void GetInstalledEntityFrameworkAssemblyVersion_returns_version_when_only_SDE()
        {
            var project = MockDTE.CreateProject(new[] { MockDTE.CreateReference("System.Data.Entity", "4.0.0.0") });

            VsUtils.GetInstalledEntityFrameworkAssemblyVersion(project).Should().Be(new Version(4, 0, 0, 0));
        }

        [TestMethod]
        public void GetInstalledEntityFrameworkAssemblyVersion_returns_EF_version_when_EF_and_SDE()
        {
            var project = MockDTE.CreateProject(
                new[]
                    {
                        MockDTE.CreateReference("System.Data.Entity", "4.0.0.0"),
                        MockDTE.CreateReference("EntityFramework", "5.0.0.0")
                    });

            VsUtils.GetInstalledEntityFrameworkAssemblyVersion(project).Should().Be(RuntimeVersion.Version5Net45);
        }

        [TestMethod]
        public void GetInstalledEntityFrameworkAssemblyVersion_returns_version_when_only_EF()
        {
            var project = MockDTE.CreateProject(new[] { MockDTE.CreateReference("EntityFramework", "6.0.0.0") });

            VsUtils.GetInstalledEntityFrameworkAssemblyVersion(project).Should().Be(RuntimeVersion.Version6);
        }

        [TestMethod]
        public void IsModernProviderAvailable_returns_true_for_known_providers()
        {
            VsUtils.IsModernProviderAvailable(
                "System.Data.SqlClient",
                Mock.Of<Project>(),
                Mock.Of<IServiceProvider>()).Should().BeTrue();
        }

        [TestMethod]
        public void IsMiscellaneousProject_detects_misc_files_project()
        {
            VsUtils.IsMiscellaneousProject(MockDTE.CreateMiscFilesProject()).Should().BeTrue();
            VsUtils.IsMiscellaneousProject(MockDTE.CreateProject()).Should().BeFalse();
        }

        [TestMethod]
        public void EntityFrameworkSupportedInProject_returns_true_for_applicable_projects()
        {
            var targets =
                new[]
                    {
                        ".NETFramework,Version=v4.0",
                        ".NETFramework,Version=v3.5",
                        ".NETFramework,Version=v4.5",
                    };

            foreach (var target in targets)
            {
                var monikerHelper = new MockDTE(target);

                VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: true).Should().BeTrue();
                VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: false).Should().BeTrue();
            }
        }

        [TestMethod]
        public void EntityFrameworkSupportedInProject_returns_true_for_Misc_project_if_allowed()
        {
            const string vsMiscFilesProjectUniqueName = "<MiscFiles>";

            var monikerHelper = new MockDTE("anytarget", vsMiscFilesProjectUniqueName);

            VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: true).Should().BeTrue();
        }

        [TestMethod]
        public void EntityFrameworkSupportedInProject_returns_false_for_Misc_project_if_not_allowed()
        {
            const string vsMiscFilesProjectUniqueName = "<MiscFiles>";

            var monikerHelper = new MockDTE("anytarget", vsMiscFilesProjectUniqueName);

            VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: false).Should().BeFalse();
        }

        [TestMethod]
        public void EntityFrameworkSupportedInProject_returns_false_for_project_where_EF_cannot_be_used()
        {
            var targets =
                new[]
                    {
                        ".NETFramework,Version=v3.0",
                        ".NETFramework,Version=v2.0",
                        ".XBox,Version=v4.5",
                        string.Empty,
                        null
                    };

            foreach (var target in targets)
            {
                var monikerHelper = new MockDTE(target);

                VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: true).Should().BeFalse();
                VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: false).Should().BeFalse();
            }
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_all_versions_for_misc_project()
        {
            var serviceProvider = new Mock<IServiceProvider>().Object;

            VsUtils.SchemaVersionSupportedInProject(
                MockDTE.CreateMiscFilesProject(), EntityFrameworkVersion.Version1, serviceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                MockDTE.CreateMiscFilesProject(), EntityFrameworkVersion.Version2, serviceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                MockDTE.CreateMiscFilesProject(), EntityFrameworkVersion.Version3, serviceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v1_and_NetFramework_3_5_otherwise_false()
        {
            var mockDte =
                new MockDTE(
                    ".NETFramework, Version=v3.5",
                    references: new[] { MockDTE.CreateReference("System.Data.Entity", "3.5.0.0") });

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v2_and_NetFramework_4_otherwise_false()
        {
            var mockDte =
                new MockDTE(
                    ".NETFramework, Version=v4.0",
                    references: new[] { MockDTE.CreateReference("System.Data.Entity", "4.0.0.0") });

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v1_and_NetFramework_3_5_if_EF_not_referenced_otherwise_false()
        {
            var mockDte =
                new MockDTE(".NETFramework, Version=v3.5", references: new Reference[0]);

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void
            SchemaVersionSupportedInProject_returns_true_for_v2_and_NetFramework_4_if_EF_not_referenced_otherwise_false()
        {
            var mockDte =
                new MockDTE(".NETFramework, Version=v4.0", references: new Reference[0]);

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void
            SchemaVersionSupportedInProject_returns_true_for_v3_and_NetFramework_4_5_if_EF_not_referenced_otherwise_false()
        {
            var mockDte =
                new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v2_and_NetFramework_4_and_EF5_otherwise_false()
        {
            var mockDte =
                new MockDTE(
                    ".NETFramework, Version=v4.0",
                    references:
                        new[]
                            {
                                MockDTE.CreateReference("System.Data.Entity", "4.0.0.0"),
                                MockDTE.CreateReference("EntityFramework", "4.4.0.0")
                            });

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeFalse();
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v2_and_v3_if_EF4_and_EF6_present()
        {
            var mockDte =
                new MockDTE(
                    ".NETFramework, Version=v4.0",
                    references:
                        new[]
                            {
                                MockDTE.CreateReference("System.Data.Entity", "4.0.0.0"),
                                MockDTE.CreateReference("EntityFramework", "6.0.0.0")
                            });

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeTrue();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v3_and_EF5_on_NetFramework_4_5_otherwise_false()
        {
            var mockDte =
                new MockDTE(
                    ".NETFramework, Version=v4.5",
                    references: new[] { MockDTE.CreateReference("System.Data.Entity", "4.0.0.0") });

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeFalse();

            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v3_and_EF6_otherwise_false()
        {
            var targetNetFrameworkVersions =
                new[] { ".NETFramework, Version=v4.0", ".NETFramework, Version=v4.5" };

            foreach (var targetNetFrameworkVersion in targetNetFrameworkVersions)
            {
                var mockDte =
                    new MockDTE(
                        targetNetFrameworkVersion,
                        references: new[] { MockDTE.CreateReference("EntityFramework", "6.0.0.0") });

                VsUtils.SchemaVersionSupportedInProject(
                    mockDte.Project, EntityFrameworkVersion.Version1, mockDte.ServiceProvider).Should().BeFalse();

                VsUtils.SchemaVersionSupportedInProject(
                    mockDte.Project, EntityFrameworkVersion.Version2, mockDte.ServiceProvider).Should().BeFalse();

                VsUtils.SchemaVersionSupportedInProject(
                    mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeTrue();
            }
        }

        [TestMethod]
        public void GetProjectKind_when_csharp()
        {
            var project = MockDTE.CreateProject(kind: MockDTE.CSharpProjectKind);

            VsUtils.GetProjectKind(project).Should().Be(VsUtils.ProjectKind.CSharp);
        }

        [TestMethod]
        public void GetProjectKind_when_vb()
        {
            var project = MockDTE.CreateProject(kind: MockDTE.VBProjectKind);

            VsUtils.GetProjectKind(project).Should().Be(VsUtils.ProjectKind.VB);
        }

        [TestMethod]
        public void GetProjectKind_when_web()
        {
            VsUtils.GetProjectKind(MockDTE.CreateWebSite()).Should().Be(VsUtils.ProjectKind.Web);
        }

        [TestMethod]
        public void GetProjectKind_when_unknown()
        {
            var project = MockDTE.CreateProject(kind: null);

            VsUtils.GetProjectKind(project).Should().Be(VsUtils.ProjectKind.Unknown);
        }

        [TestMethod]
        public void IsWebSiteProject_when_web()
        {
            VsUtils.IsWebSiteProject(MockDTE.CreateWebSite()).Should().BeTrue();
        }

        [TestMethod]
        public void IsWebSiteProject_when_not_web()
        {
            VsUtils.IsWebSiteProject(MockDTE.CreateProject()).Should().BeFalse();
        }

        [TestMethod]
        public void GetProjectRoot_returns_solution_dir_when_misc_project()
        {
            var solution = new Mock<IVsSolution>();
            var solutionDirectory = @"C:\Path\To\Solution\";
            string temp;
            solution.Setup(s => s.GetSolutionInfo(out solutionDirectory, out temp, out temp));
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IVsSolution))).Returns(solution.Object);

            var root = VsUtils.GetProjectRoot(MockDTE.CreateMiscFilesProject(), serviceProvider.Object);

            root.FullName.Should().Be(solutionDirectory);
        }

        [TestMethod]
        public void GetProjectRoot_returns_fullpath_when_csharp_project()
        {
            const string fullPath = @"C:\Path\To\Project\";
            var project = MockDTE.CreateProject(
                properties: new Dictionary<string, object> { { "FullPath", fullPath } });

            var root = VsUtils.GetProjectRoot(project, Mock.Of<IServiceProvider>());

            root.FullName.Should().Be(fullPath);
        }

        [TestMethod]
        public void GetProjectRoot_returns_fullpath_when_website()
        {
            const string fullPath = @"C:\Path\To\WebSite";
            var project = MockDTE.CreateWebSite(
                properties: new Dictionary<string, object> { { "FullPath", fullPath } });

            var root = VsUtils.GetProjectRoot(project, Mock.Of<IServiceProvider>());

            root.FullName.Should().Be(fullPath + Path.DirectorySeparatorChar);
        }

        [TestMethod]
        public void GetProjectTargetDir_returns_dir_when_csharp_project()
        {
            var project = MockDTE.CreateProject(
                properties: new Dictionary<string, object> { { "FullPath", @"C:\Path\To\Project\" } },
                configurationProperties: new Dictionary<string, object> { { "OutputPath", @"bin\Debug\" } });

            var targetDir = VsUtils.GetProjectTargetDir(project, Mock.Of<IServiceProvider>());

            targetDir.Should().Be(@"C:\Path\To\Project\bin\Debug\");
        }

        [TestMethod]
        public void GetProjectTargetDir_returns_dir_when_website()
        {
            var project = MockDTE.CreateWebSite(
                properties: new Dictionary<string, object> { { "FullPath", @"C:\Path\To\WebSite" } });

            var targetDir = VsUtils.GetProjectTargetDir(project, Mock.Of<IServiceProvider>());

            targetDir.Should().Be(@"C:\Path\To\WebSite\Bin\");
        }

        [TestMethod]
        public void GetProjectTargetFileName_returns_name()
        {
            var project = MockDTE.CreateProject(
                properties: new Dictionary<string, object> { { "OutputFileName", "ConsoleApplication1.exe" } });

            VsUtils.GetProjectTargetFileName(project).Should().Be("ConsoleApplication1.exe");
        }

        [TestMethod]
        public void GetProjectTargetFileName_returns_null_when_nonstring()
        {
            var project = MockDTE.CreateProject(
                properties: new Dictionary<string, object> { { "OutputFileName", 42 } });

            VsUtils.GetProjectTargetFileName(project).Should().BeNull();
        }

        [TestMethod]
        public void GetProjectTargetFileName_returns_null_when_no_property()
        {
            var project = MockDTE.CreateProject(properties: new Dictionary<string, object>());

            VsUtils.GetProjectTargetFileName(project).Should().BeNull();
        }

        [TestMethod]
        public void GetTypeFromProject_uses_dynamic_type_service()
        {
            var dte = new MockDTE(".NETFramework,Version=v4.5");

            var typeResolutionService = new Mock<ITypeResolutionService>();
            var dynamicTypeService = new Mock<DynamicTypeService>(MockBehavior.Strict);
            dynamicTypeService.Setup(s => s.GetTypeResolutionService(It.IsAny<IVsHierarchy>(), It.IsAny<uint>()))
                .Returns(typeResolutionService.Object);
            var serviceProvider = Mock.Get(dte.ServiceProvider);
            serviceProvider.Setup(p => p.GetService(typeof(DynamicTypeService))).Returns(dynamicTypeService.Object);

            VsUtils.GetTypeFromProject("Some.Type", dte.Project, dte.ServiceProvider);

            dynamicTypeService.Verify(s => s.GetTypeResolutionService(dte.Hierarchy, It.IsAny<uint>()));
            typeResolutionService.Verify(s => s.GetType("Some.Type"));
        }

        [TestMethod]
        public void EnsureProvider_registers_modern_provider()
        {
            VsUtils.EnsureProvider("System.Data.SqlClient", false, Mock.Of<Project>(), Mock.Of<IServiceProvider>());
            try
            {
                var sqlProviderServicesType = Type.GetType(
                    "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer",
                    throwOnError: true);
                var instanceProperty = sqlProviderServicesType.GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var expectedInstance = instanceProperty.GetValue(null);

                DependencyResolver.GetService<DbProviderServices>("System.Data.SqlClient")
                    .Should().BeSameAs(expectedInstance);
            }
            finally
            {
                DependencyResolver.UnregisterProvider("System.Data.SqlClient");
            }
        }

        [TestMethod]
        public void GetProjectItemByPath_returns_item()
        {
            var projectItem = new Mock<ProjectItem>();
            var projectItems = new Mock<ProjectItems>();
            projectItems.Setup(i => i.Item("Class1.cs")).Returns(projectItem.Object);
            var project = new Mock<Project>();
            project.SetupGet(p => p.ProjectItems).Returns(projectItems.Object);

            var result = VsUtils.GetProjectItemByPath(project.Object, "Class1.cs");

            result.Should().BeSameAs(projectItem.Object);
        }

        [TestMethod]
        public void GetProjectItemByPath_returns_item_when_nested()
        {
            var fileProjectItem = new Mock<ProjectItem>();
            var directoryProjectItems = new Mock<ProjectItems>();
            directoryProjectItems.Setup(i => i.Item("Class1.cs")).Returns(fileProjectItem.Object);
            var directoryProjectItem = new Mock<ProjectItem>();
            directoryProjectItem.SetupGet(i => i.ProjectItems).Returns(directoryProjectItems.Object);
            var projectItems = new Mock<ProjectItems>();
            projectItems.Setup(i => i.Item("Model")).Returns(directoryProjectItem.Object);
            var project = new Mock<Project>();
            project.SetupGet(p => p.ProjectItems).Returns(projectItems.Object);

            var result = VsUtils.GetProjectItemByPath(project.Object, @"Model\Class1.cs");

            result.Should().BeSameAs(fileProjectItem.Object);
        }

        [TestMethod]
        public void GetProjectItemByPath_returns_null_when_error()
        {
            var projectItems = new Mock<ProjectItems>();
            projectItems.Setup(i => i.Item(It.IsAny<object>())).Throws<Exception>();
            var project = new Mock<Project>();
            project.SetupGet(p => p.ProjectItems).Returns(projectItems.Object);

            var result = VsUtils.GetProjectItemByPath(project.Object, "Class1.cs");

            result.Should().BeNull();
        }

        [TestMethod]
        public void GetProviderManifestTokenConnected_returns_provider_manifest_token()
        {
            var providerServicesMock = new Mock<DbProviderServices>();
            providerServicesMock
                .Protected()
                .Setup<string>("GetDbProviderManifestToken", ItExpr.IsAny<DbConnection>())
                .Returns("FakeProviderManifestToken");

            var mockResolver = new Mock<IDbDependencyResolver>();
            mockResolver.Setup(
                r => r.GetService(
                    It.Is<Type>(t => t == typeof(DbProviderServices)),
                    It.IsAny<string>())).Returns(providerServicesMock.Object);

            VsUtils.GetProviderManifestTokenConnected(
                mockResolver.Object,
                "System.Data.SqlClient",
                providerConnectionString: string.Empty).Should().Be("FakeProviderManifestToken");
        }

        // Tests for SDK-style project support

        [TestMethod]
        public void IsSdkStyleProject_returns_true_for_sdk_style_project()
        {
            var project = MockDTE.CreateSdkStyleProject();

            VsUtils.IsSdkStyleProject(project).Should().BeTrue();
        }

        [TestMethod]
        public void IsSdkStyleProject_returns_false_for_traditional_project()
        {
            var project = MockDTE.CreateProject(new[] { MockDTE.CreateReference("EntityFramework", "6.0.0.0") });

            VsUtils.IsSdkStyleProject(project).Should().BeFalse();
        }

        [TestMethod]
        public void IsSdkStyleProject_returns_false_for_website_project()
        {
            var project = MockDTE.CreateWebSite(new[] { MockDTE.CreateAssemblyReference("EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") });

            VsUtils.IsSdkStyleProject(project).Should().BeFalse();
        }

        [TestMethod]
        public void IsSdkStyleProject_returns_false_for_misc_files_project()
        {
            var project = MockDTE.CreateMiscFilesProject();

            VsUtils.IsSdkStyleProject(project).Should().BeFalse();
        }

        [TestMethod]
        public void GetProjectReferenceAssemblyNames_returns_empty_for_sdk_style_project()
        {
            var project = MockDTE.CreateSdkStyleProject();

            var referenceAssemblyNames = VsUtils.GetProjectReferenceAssemblyNames(project).ToArray();

            referenceAssemblyNames.Should().BeEmpty();
        }

        [TestMethod]
        public void EntityFrameworkSupportedInProject_returns_true_for_modern_dotnet_projects()
        {
            var targets = new[]
            {
                ".NET,Version=v8.0",
                ".NET,Version=v9.0",
                ".NET,Version=v10.0",
                ".NETCoreApp,Version=v3.1",
            };

            foreach (var target in targets)
            {
                var monikerHelper = new MockDTE(target, MockDTE.CreateSdkStyleProject());

                VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: true)
                    .Should().BeTrue($"Expected true for {target}");
                VsUtils.EntityFrameworkSupportedInProject(monikerHelper.Project, monikerHelper.ServiceProvider, allowMiscProject: false)
                    .Should().BeTrue($"Expected true for {target}");
            }
        }

        [TestMethod]
        public void SchemaVersionSupportedInProject_returns_true_for_v3_and_modern_dotnet_no_ef_referenced()
        {
            // Modern .NET projects without explicit EF reference should support v3 schema
            var mockDte = new MockDTE(".NET,Version=v8.0", MockDTE.CreateSdkStyleProject());

            // For SDK-style projects, GetProjectReferenceAssemblyNames returns empty,
            // so we fall back to checking schema version against target framework.
            // Modern .NET (null from TargetNetFrameworkVersion) should map to v3 schema.
            VsUtils.SchemaVersionSupportedInProject(
                mockDte.Project, EntityFrameworkVersion.Version3, mockDte.ServiceProvider).Should().BeTrue();
        }
    }
}
