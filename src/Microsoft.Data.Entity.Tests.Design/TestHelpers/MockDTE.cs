// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VSLangProj;
using VSLangProj80;
using VsWebSite;
using Constants = EnvDTE.Constants;

namespace Microsoft.Data.Entity.Tests.Design.TestHelpers
{
    internal class MockDTE
    {
        public const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        public const string VBProjectKind = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        public const string WebSiteProjectKind = "{E24C65DC-7377-472b-9ABA-BC803B73C61A}";

        private const int S_OK = 0;
        private const uint VSITEMID_ROOT = unchecked((uint)-2);
        private const int VSHPROPID_TargetFrameworkMoniker = -2102;
        private const int VSHPROPID_ExtObject = -2027;

        public readonly object TargetFrameworkMoniker;
        public readonly IVsHierarchy Hierarchy;
        public readonly IVsSolution Solution;
        public readonly Project Project;
        public readonly IServiceProvider ServiceProvider;

        private uint _projectItemId;

        public static Project CreateProject(
            IEnumerable<Reference> assemblyReferences = null,
            string kind = CSharpProjectKind,
            IDictionary<string, object> properties = null,
            IDictionary<string, object> configurationProperties = null)
        {
            return CreateProject(
                null,
                CreateVsProject2(assemblyReferences),
                kind,
                properties,
                configurationProperties);
        }

        public static Project CreateMiscFilesProject()
        {
            return CreateProject(Constants.vsMiscFilesProjectUniqueName, null, null, null, null);
        }

        /// <summary>
        /// Creates a mock SDK-style project (e.g., .NET Core, .NET 5+).
        /// SDK-style projects don't expose VSProject2 or VSWebSite, they use the Common Project System (CPS).
        /// </summary>
        public static Project CreateSdkStyleProject(
            string kind = CSharpProjectKind,
            IDictionary<string, object> properties = null,
            IDictionary<string, object> configurationProperties = null)
        {
            // SDK-style projects have null for project.Object (no VSProject2 or VSWebSite)
            return CreateProject(
                null,
                null, // vsProject is null - this is what makes it an SDK-style project
                kind,
                properties,
                configurationProperties);
        }

        public static Project CreateWebSite(
            IEnumerable<AssemblyReference> assemblyReferences = null,
            IDictionary<string, object> properties = null,
            IDictionary<string, object> configurationProperties = null)
        {
            return CreateProject(
                null,
                CreateVsWebsite(assemblyReferences),
                WebSiteProjectKind,
                properties,
                configurationProperties);
        }

        public static Reference CreateReference(string name, string version)
        {
            Mock<Reference> reference = new Mock<Reference>();
            reference.SetupGet(r => r.Name).Returns(name);
            reference.SetupGet(r => r.Version).Returns(version);

            return reference.Object;
        }

        public static Reference3 CreateReference3(
            string name, string version, string identity = null, string path = null, bool isResolved = false)
        {
            Mock<Reference3> reference = new Mock<Reference3>();
            reference.SetupGet(r => r.Name).Returns(name);
            reference.SetupGet(r => r.Version).Returns(version);
            if (identity != null)
            {
                reference.SetupGet(r => r.Identity).Returns(identity);
            }
            if (path != null)
            {
                reference.SetupGet(r => r.Path).Returns(path);
            }
            reference.SetupGet(r => r.Resolved).Returns(isResolved);

            return reference.Object;
        }

        public static AssemblyReference CreateAssemblyReference(string strongName, string path = null)
        {
            Mock<AssemblyReference> assemblyReference = new Mock<AssemblyReference>();
            assemblyReference.SetupGet(r => r.StrongName).Returns(strongName);
            if (path != null)
            {
                assemblyReference.SetupGet(r => r.FullPath).Returns(path);
            }

            return assemblyReference.Object;
        }

        public static Mock<ProjectItem> CreateProjectItem(string path)
        {
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();

            Mock<Properties> mockProperties = new Mock<Properties>() { CallBase = true};

            mockProjectItem.Setup(p => p.Properties).Returns(mockProperties.Object);

            mockProjectItem
                .Setup(p => p.get_FileNames(1))
                .Returns(path);

            return mockProjectItem;
        }

        public MockDTE(
            object targetFrameworkMoniker, string projectUniqueName = null, IEnumerable<Reference> references = null)
            : this(targetFrameworkMoniker, CreateProject(projectUniqueName, CreateVsProject2(references), null, null, null))
        {
        }

        public MockDTE(object targetFrameworkMoniker, Project project)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;

            Project = project;
            Hierarchy = CreateVsHierarchy(targetFrameworkMoniker, Project);
            Solution = CreateVsSolution(Hierarchy);

            Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(m => m.GetService(typeof(IVsSolution)))
                .Returns(Solution);

            mockServiceProvider
                .Setup(m => m.GetService(typeof(SVsSolution)))
                .Returns(Solution);

            ServiceProvider = mockServiceProvider.Object;
        }

        public uint AddProjectItem(Mock<ProjectItem> mockProjectItem)
        {
            ++_projectItemId;

            object projectItem = mockProjectItem.Object;

            Mock<IVsHierarchy> mockHierarchy = Mock.Get(Hierarchy);
            mockHierarchy.
                Setup(
                    m => m.GetProperty(
                        _projectItemId,
                        VSHPROPID_ExtObject,
                        out projectItem)).
                Returns(S_OK);

            mockProjectItem.Setup(i => i.ContainingProject).Returns(Project);

            return _projectItemId;
        }

        public void SetProjectProperties(IDictionary<string, object> properties)
        {
            SetProjectProperties(Mock.Get(Project), properties);
        }

        private static IVsHierarchy CreateVsHierarchy(object targetFrameworkMoniker, object project)
        {
            Mock<IVsHierarchy> mockHierarchy = new Mock<IVsHierarchy>();
            mockHierarchy
                .Setup(m => m.GetProperty(VSITEMID_ROOT, VSHPROPID_TargetFrameworkMoniker, out targetFrameworkMoniker))
                .Returns(S_OK);

            mockHierarchy.
                Setup(
                    m => m.GetProperty(
                        It.Is<uint>(itemId => itemId == VSITEMID_ROOT),
                        It.Is<int>(propertyId => propertyId == VSHPROPID_ExtObject),
                        out project)).
                Returns(S_OK);

            return mockHierarchy.Object;
        }

        private static IVsSolution CreateVsSolution(IVsHierarchy hierarchy)
        {
            Mock<IVsSolution> mockSolution = new Mock<IVsSolution>();
            mockSolution
                .Setup(m => m.GetProjectOfUniqueName(It.IsAny<string>(), out hierarchy))
                .Returns(S_OK);

            return mockSolution.Object;
        }

        private static Project CreateProject(
            string projectUniqueName,
            object vsProject,
            string kind,
            IDictionary<string, object> properties,
            IDictionary<string, object> configurationProperties)
        {
            Mock<Project> mockProject = new Mock<Project>();
            mockProject.Setup(m => m.UniqueName).Returns(projectUniqueName);
            mockProject.SetupGet(p => p.Object).Returns(vsProject);
            mockProject.SetupGet(p => p.Kind).Returns(kind);

            SetProjectProperties(mockProject, properties);

            if (configurationProperties != null)
            {
                List<Property> configurationPropertiesList = configurationProperties.Select(p => CreateProperty(p.Key, p.Value))
                    .ToList();

                Mock<Properties> activeConfigurationProperties = new Mock<Properties>();
                activeConfigurationProperties.As<IEnumerable>().Setup(p => p.GetEnumerator())
                    .Returns(() => configurationPropertiesList.GetEnumerator());

                Mock<Configuration> configuration = new Mock<Configuration>();
                configuration.SetupGet(c => c.Properties).Returns(activeConfigurationProperties.Object);

                Mock<ConfigurationManager> configurationManager = new Mock<ConfigurationManager>();
                configurationManager.SetupGet(m => m.ActiveConfiguration).Returns(configuration.Object);

                mockProject.SetupGet(p => p.ConfigurationManager).Returns(configurationManager.Object);
            }

            return mockProject.Object;
        }


        private static VSProject2 CreateVsProject2(IEnumerable<Reference> references)
        {
            if (references == null)
            {
                return null;
            }

            Mock<References> mockReferences = new Mock<References>();
            mockReferences.Setup(r => r.GetEnumerator())
                .Returns(() => references.GetEnumerator());
            mockReferences.As<IEnumerable>().Setup(r => r.GetEnumerator())
                .Returns(() => references.GetEnumerator());

            Mock<VSProject2> mockVsProject2 = new Mock<VSProject2>();
            mockVsProject2.SetupGet(p => p.References).Returns(mockReferences.Object);

            return mockVsProject2.Object;
        }

        private static VSWebSite CreateVsWebsite(IEnumerable<AssemblyReference> references)
        {
            if (references == null)
            {
                return null;
            }

            Mock<VSWebSite> vsWebSite = new Mock<VSWebSite>();
            Mock<AssemblyReferences> vsAssemblyReferences = new Mock<AssemblyReferences>();

            // Both overloads are needed. foreach binds to the strongly typed AssemblyReferences.GetEnumerator(),
            // not the IEnumerable one, so setting up only the latter leaves the former returning null and the
            // loop throws. Each is a lambda so every enumeration gets a fresh enumerator rather than sharing one
            // that is already exhausted. CreateVsProject2 does the same thing, which is why it works.
            vsAssemblyReferences.Setup(r => r.GetEnumerator())
                .Returns(() => references.GetEnumerator());
            vsAssemblyReferences.As<IEnumerable>()
                .Setup(r => r.GetEnumerator())
                .Returns(() => references.GetEnumerator());

            vsWebSite.SetupGet(p => p.References).Returns(vsAssemblyReferences.Object);

            return vsWebSite.Object;
        }

        private static void SetProjectProperties(Mock<Project> mockProject, IDictionary<string, object> properties)
        {
            if (properties != null)
            {
                List<Property> propertyList = properties.Select(p => CreateProperty(p.Key, p.Value)).ToList();

                Mock<Properties> projectProperties = new Mock<Properties>();
                projectProperties.As<IEnumerable>().Setup(p => p.GetEnumerator())
                    .Returns(() => propertyList.GetEnumerator());
                projectProperties.Setup(p => p.Item(It.IsAny<string>()))
                    .Returns<string>(n => propertyList.FirstOrDefault(p => p.Name == n));

                mockProject.SetupGet(p => p.Properties).Returns(projectProperties.Object);
            }
        }

        private static Property CreateProperty(string name, object value)
        {
            Mock<Property> property = new Mock<Property>();
            property.SetupGet(p => p.Name).Returns(name);
            property.SetupGet(p => p.Value).Returns(value);

            return property.Object;
        }
    }
}
