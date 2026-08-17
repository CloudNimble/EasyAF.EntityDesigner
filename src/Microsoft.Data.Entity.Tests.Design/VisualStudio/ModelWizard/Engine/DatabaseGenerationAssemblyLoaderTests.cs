// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.IO;
using FluentAssertions;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Engine
{
    [TestClass]
    public class DatabaseGenerationAssemblyLoaderTests
    {
        [TestMethod]
        public void AssemblyLoader_passed_null_project_can_find_paths_for_standard_DLLs_case_insensitive()
        {
            const string vsInstallPath = @"C:\My\Test\VS\InstallPath";
            DatabaseGenerationAssemblyLoader assemblyLoader = new DatabaseGenerationAssemblyLoader(null, vsInstallPath);
            assemblyLoader.GetAssemblyPath("EntityFramework")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.dll"));
            assemblyLoader.GetAssemblyPath("entityframework")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.dll"));
            assemblyLoader.GetAssemblyPath("ENTITYFRAMEWORK")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.dll"));
            assemblyLoader.GetAssemblyPath("EntityFramework.SqlServer")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServer.dll"));
            assemblyLoader.GetAssemblyPath("entityframework.sqlserver")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServer.dll"));
            assemblyLoader.GetAssemblyPath("ENTITYFRAMEWORK.SQLSERVER")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServer.dll"));
            assemblyLoader.GetAssemblyPath("EntityFramework.SqlServerCompact")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServerCompact.dll"));
            assemblyLoader.GetAssemblyPath("entityframework.sqlservercompact")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServerCompact.dll"));
            assemblyLoader.GetAssemblyPath("ENTITYFRAMEWORK.SQLSERVERCOMPACT")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServerCompact.dll"));
        }

        [TestMethod]
        public void AssemblyLoader_passed_non_WebsiteProject_can_find_correct_paths_to_DLLs()
        {
            const string vsInstallPath = @"C:\My\Test\VS\InstallPath";
            const string projectPath = @"C:\My\Test\ProjectPath";
            var project =
                MockDTE.CreateProject(
                    new[]
                        {
                            MockDTE.CreateReference3(
                                "EntityFramework", "6.0.0.0", "EntityFramework",
                                Path.Combine(projectPath, "EntityFramework.dll")),
                            MockDTE.CreateReference3(
                                "EntityFramework.SqlServer", "6.0.0.0", "EntityFramework.SqlServer",
                                Path.Combine(projectPath, "EntityFramework.SqlServer.dll")),
                            MockDTE.CreateReference3(
                                "EntityFramework.SqlServerCompact", "6.0.0.0",
                                "EntityFramework.SqlServerCompact",
                                Path.Combine(projectPath, "EntityFramework.SqlServerCompact.dll")),
                            MockDTE.CreateReference3(
                                "My.Project.Reference", "6.0.0.0", "My.Project.Reference",
                                Path.Combine(projectPath, "My.Project.Reference.dll"), true)
                        });
            DatabaseGenerationAssemblyLoader assemblyLoader = new DatabaseGenerationAssemblyLoader(project, vsInstallPath);

            // assert that the DLLs installed under VS are resolved there
            assemblyLoader.GetAssemblyPath("EntityFramework")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.dll"));
            assemblyLoader.GetAssemblyPath("EntityFramework.SqlServer")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServer.dll"));
            assemblyLoader.GetAssemblyPath("EntityFramework.SqlServerCompact")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServerCompact.dll"));

            // assert that other project references are resolved to wherever their reference points to
            assemblyLoader.GetAssemblyPath("My.Project.Reference")
                .Should().Be(Path.Combine(projectPath, "My.Project.Reference.dll"));
        }

        [TestMethod]
        public void AssemblyLoader_passed_WebsiteProject_can_find_correct_paths_to_DLLs()
        {
            const string vsInstallPath = @"C:\My\Test\VS\InstallPath";
            const string projectPath = @"C:\My\Test\WebsitePath";
            var project =
                MockDTE.CreateWebSite(
                    new[]
                        {
                            MockDTE.CreateAssemblyReference(
                                "EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                                Path.Combine(projectPath, "EntityFramework.dll")),
                            MockDTE.CreateAssemblyReference(
                                "EntityFramework.SqlServer, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                                Path.Combine(projectPath, "EntityFramework.SqlServer.dll")),
                            MockDTE.CreateAssemblyReference(
                                "EntityFramework.SqlServerCompact, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                                Path.Combine(projectPath, "EntityFramework.SqlServerCompact.dll")),
                            MockDTE.CreateAssemblyReference(
                                "My.WebsiteProject.Reference, Version=4.1.0.0, Culture=neutral, PublicKeyToken=bbbbbbbbbbbbbbbb",
                                Path.Combine(projectPath, "My.WebsiteProject.Reference.dll"))
                        });
            DatabaseGenerationAssemblyLoader assemblyLoader = new DatabaseGenerationAssemblyLoader(project, vsInstallPath);

            // assert that the DLLs installed under VS are resolved there
            assemblyLoader.GetAssemblyPath("EntityFramework")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.dll"));
            assemblyLoader.GetAssemblyPath("EntityFramework.SqlServer")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServer.dll"));
            assemblyLoader.GetAssemblyPath("EntityFramework.SqlServerCompact")
                .Should().Be(Path.Combine(vsInstallPath, "EntityFramework.SqlServerCompact.dll"));

            // assert that other project references are resolved to wherever their reference points to
            assemblyLoader.GetAssemblyPath("My.WebsiteProject.Reference")
                .Should().Be(Path.Combine(projectPath, "My.WebsiteProject.Reference.dll"));
        }
    }
}
