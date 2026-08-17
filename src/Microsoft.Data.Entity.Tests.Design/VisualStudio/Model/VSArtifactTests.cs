// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Xml.Linq;
using EnvDTE;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using VSLangProj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Model;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.Model
{
    [TestClass]
    public class VSArtifactTests
    {
        [TestMethod]
        public void GetModelGenErrors_returns_errors_stored_in_ModelGenCache()
        {
            ModelGenErrorCache modelGenCache = new ModelGenErrorCache();
            List<EdmSchemaError> errors = new List<EdmSchemaError>(new[] { new EdmSchemaError("test", 42, EdmSchemaErrorSeverity.Error) });
            modelGenCache.AddErrors(@"C:\temp.edmx", errors);

            Mock<IEdmPackage> mockPackage = new Mock<IEdmPackage>();
            mockPackage.Setup(p => p.ModelGenErrorCache).Returns(modelGenCache);
            PackageManager.Package = mockPackage.Object;

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            using (VSArtifact vsArtifact = new VSArtifact(modelManager, new Uri(@"C:\temp.edmx"), modelProvider))
            {
                vsArtifact.GetModelGenErrors().Should().BeSameAs(errors);
            }
        }

        [TestMethod]
        public void
            DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_true_if_schema_is_the_latest_version_supported_by_referenced_runtime()
        {
            MockDTE mockDte = new MockDTE(
                ".NETFramework, Version=v4.7.2",
                references: new[] { MockDTE.CreateReference("EntityFramework", "6.0.0.0") });

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<VSArtifact> mockVsArtifact =
                new Mock<VSArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };
            mockVsArtifact.Protected().Setup<IServiceProvider>("ServiceProvider").Returns(mockDte.ServiceProvider);
            mockVsArtifact.Protected().Setup<Project>("GetProject").Returns(mockDte.Project);
            mockVsArtifact.Setup(a => a.XDocument).Returns(
                new XDocument(
                    new XElement(
                        XName.Get(
                            "Edmx",
                            SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3)))));

            var artifact = mockVsArtifact.Object;

            artifact.DetermineIfArtifactIsVersionSafe();
            artifact.IsVersionSafe.Should().BeTrue();
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_true_for_Misc_project()
        {
            MockDTE mockDte = new MockDTE(
                ".NETFramework, Version=v4.7.2",
                Constants.vsMiscFilesProjectUniqueName);

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<VSArtifact> mockVsArtifact =
                new Mock<VSArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };
            mockVsArtifact.Protected().Setup<IServiceProvider>("ServiceProvider").Returns(mockDte.ServiceProvider);
            mockVsArtifact.Protected().Setup<Project>("GetProject").Returns(mockDte.Project);
            mockVsArtifact.Setup(a => a.XDocument).Returns(
                new XDocument(
                    new XElement(
                        XName.Get(
                            "Edmx",
                            SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3)))));

            var artifact = mockVsArtifact.Object;
            artifact.DetermineIfArtifactIsVersionSafe();
            artifact.IsVersionSafe.Should().BeTrue();
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_false_if_project_does_not_support_EF()
        {
            MockDTE mockDte = new MockDTE(
                ".NETFramework, Version=v2.0",
                references: new Reference[0]);

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<VSArtifact> mockVsArtifact =
                new Mock<VSArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };
            mockVsArtifact.Protected().Setup<IServiceProvider>("ServiceProvider").Returns(mockDte.ServiceProvider);
            mockVsArtifact.Protected().Setup<Project>("GetProject").Returns(mockDte.Project);
            mockVsArtifact.Setup(a => a.XDocument).Returns(
                new XDocument(
                    new XElement(
                        XName.Get(
                            "Edmx",
                            SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3)))));

            var artifact = mockVsArtifact.Object;

            artifact.DetermineIfArtifactIsVersionSafe();
            artifact.IsVersionSafe.Should().BeFalse();
        }

        [TestMethod]
        public void
            DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_true_when_schema_is_latest_version_supported_by_referenced_runtime
            ()
        {
            // Version3 is the only supported schema version now, and EF6 supports Version3
            MockDTE mockDte = new MockDTE(
                ".NETFramework, Version=v4.7.2",
                references: new[] { MockDTE.CreateReference("EntityFramework", "6.0.0.0") });

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<VSArtifact> mockVsArtifact =
                new Mock<VSArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };
            mockVsArtifact.Protected().Setup<IServiceProvider>("ServiceProvider").Returns(mockDte.ServiceProvider);
            mockVsArtifact.Protected().Setup<Project>("GetProject").Returns(mockDte.Project);
            mockVsArtifact.Setup(a => a.XDocument).Returns(
                new XDocument(
                    new XElement(
                        XName.Get(
                            "Edmx",
                            SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3)))));

            var artifact = mockVsArtifact.Object;

            artifact.DetermineIfArtifactIsVersionSafe();
            artifact.IsVersionSafe.Should().BeTrue();
        }

        [TestMethod]
        public void DetermineIfArtifactIsVersionSafe_sets_IsVersionSafe_to_false_if_versions_dont_match()
        {
            MockDTE mockDte = new MockDTE(
                ".NETFramework, Version=v4.7.2",
                Constants.vsMiscFilesProjectUniqueName);

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<VSArtifact> mockVsArtifact =
                new Mock<VSArtifact>(modelManager, new Uri("urn:dummy"), modelProvider) { CallBase = true };
            mockVsArtifact.Protected().Setup<IServiceProvider>("ServiceProvider").Returns(mockDte.ServiceProvider);
            mockVsArtifact.Protected().Setup<Project>("GetProject").Returns(mockDte.Project);
            mockVsArtifact.Setup(a => a.XDocument).Returns(
                new XDocument(
                    new XElement(
                        XName.Get(
                            "Edmx",
                            SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3)))));

            // Use a fictional namespace to simulate a mismatch
            Mock<ConceptualEntityModel> mockConceptualModel =
                new Mock<ConceptualEntityModel>(
                    mockVsArtifact.Object,
                    new XElement(XName.Get("Schema", "http://invalid.namespace.for.test")));

            mockConceptualModel
                .Setup(m => m.XNamespace)
                .Returns("http://invalid.namespace.for.test");

            mockVsArtifact
                .Setup(m => m.ConceptualModel)
                .Returns(mockConceptualModel.Object);

            var artifact = mockVsArtifact.Object;
            artifact.DetermineIfArtifactIsVersionSafe();
            artifact.IsVersionSafe.Should().BeFalse();
        }
    }
}
