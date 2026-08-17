// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade
{
    [TestClass]
    public class SchemaManagerTests
    {
        // Only Version3 namespaces are supported
        private const string CsdlNsV3 = "http://schemas.microsoft.com/ado/2009/11/edm";
        private const string SsdlNsV3 = "http://schemas.microsoft.com/ado/2009/11/edm/ssdl";
        private const string MslNsV3 = "http://schemas.microsoft.com/ado/2009/11/mapping/cs";
        private const string EdmxNsV3 = "http://schemas.microsoft.com/ado/2009/11/edmx";

        private const string EntityStoreSchemaGeneratorNs = "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator";
        private const string CodeGenerationNs = "http://schemas.microsoft.com/ado/2006/04/codegeneration";
        private const string ProviderManifestNs = "http://schemas.microsoft.com/ado/2006/04/edm/providermanifest";
        private const string AnnotationNs = "http://schemas.microsoft.com/ado/2009/02/edm/annotation";

        [TestMethod]
        public void SchemaManager_GetCSDLNamespaceName_returns_correct_Csdl_namespace_for_Version3()
        {
            SchemaManager.GetCSDLNamespaceName(new Version(3, 0, 0, 0)).Should().Be(CsdlNsV3);
        }

        [TestMethod]
        public void SchemaManager_GetCSDLNamespaceName_returns_all_known_Csdl_namespaces()
        {
            var csdlNamespaces = SchemaManager.GetCSDLNamespaceNames();

            // Only Version3 namespace is supported
            new[] { CsdlNsV3 }.SequenceEqual(csdlNamespaces).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaManager_GetSSDLNamespaceName_returns_correct_Ssdl_namespace_for_Version3()
        {
            SchemaManager.GetSSDLNamespaceName(new Version(3, 0, 0, 0)).Should().Be(SsdlNsV3);
        }

        [TestMethod]
        public void SchemaManager_GetSSDLNamespaceName_returns_all_known_Ssdl_namespaces()
        {
            var ssdlNamespaces = SchemaManager.GetSSDLNamespaceNames();

            // Only Version3 namespace is supported
            new[] { SsdlNsV3 }.SequenceEqual(ssdlNamespaces).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaManager_GetMSLNamespaceName_returns_correct_Msl_namespace_for_Version3()
        {
            SchemaManager.GetMSLNamespaceName(new Version(3, 0, 0, 0)).Should().Be(MslNsV3);
        }

        [TestMethod]
        public void SchemaManager_GetMSLNamespaceName_returns_all_known_Msl_namespaces()
        {
            var mslNamespaces = SchemaManager.GetMSLNamespaceNames();

            // Only Version3 namespace is supported
            new[] { MslNsV3 }.SequenceEqual(mslNamespaces).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaManager_GetEDMXNamespaceName_returns_correct_Edmx_namespace_for_Version3()
        {
            SchemaManager.GetEDMXNamespaceName(new Version(3, 0, 0, 0)).Should().Be(EdmxNsV3);
        }

        [TestMethod]
        public void SchemaManager_GetEDMXNamespaceName_returns_all_known_Edmx_namespaces()
        {
            var edmxNamespaces = SchemaManager.GetEDMXNamespaceNames();

            // Only Version3 namespace is supported
            new[] { EdmxNsV3 }.SequenceEqual(edmxNamespaces).Should().BeTrue();
        }

        [TestMethod]
        public void
            SchemaManager_GetEntityStoreSchemaGeneratorNamespaceName_returns_correct_EntityStoreSchemaGenerator_namespace()
        {
            SchemaManager.GetEntityStoreSchemaGeneratorNamespaceName().Should().Be(EntityStoreSchemaGeneratorNs);
        }

        [TestMethod]
        public void SchemaManager_GetCodeGenerationNamespaceName_returns_correct_CodeGeneration_namespace()
        {
            SchemaManager.GetCodeGenerationNamespaceName().Should().Be(CodeGenerationNs);
        }

        [TestMethod]
        public void SchemaManager_GetProviderManifestNamespaceName_returns_correct_ProviderManifest_namespace()
        {
            SchemaManager.GetProviderManifestNamespaceName().Should().Be(ProviderManifestNs);
        }

        [TestMethod]
        public void SchemaManager_GetAnnotationNamespaceName_returns_correct_Annotation_namespace()
        {
            SchemaManager.GetAnnotationNamespaceName().Should().Be(AnnotationNs);
        }

        [TestMethod]
        public void SchemaManager_GetAllNamespacesForVersion_returns_correct_namespaces_for_Version3()
        {
            var namespacesV3 = new[] { EdmxNsV3, CsdlNsV3, SsdlNsV3, ProviderManifestNs, CodeGenerationNs, MslNsV3, AnnotationNs };

            namespacesV3.SequenceEqual(SchemaManager.GetAllNamespacesForVersion(new Version(3, 0, 0, 0))).Should().BeTrue();
        }

        [TestMethod]
        public void SchemaManager_GetSchemaVersion_returns_correct_version_for_namespace()
        {
            Version v3 = new Version(3, 0, 0, 0);

            SchemaManager.GetSchemaVersion(CsdlNsV3).Should().Be(v3);
            SchemaManager.GetSchemaVersion(SsdlNsV3).Should().Be(v3);
            SchemaManager.GetSchemaVersion(MslNsV3).Should().Be(v3);
            SchemaManager.GetSchemaVersion(EdmxNsV3).Should().Be(v3);
        }

        [TestMethod]
        public void SchemaManager_GetSchemaVersion_returns_Version3_for_unknown_namespace()
        {
            // Unknown namespaces default to Version3
            Version v3 = new Version(3, 0, 0, 0);
            SchemaManager.GetSchemaVersion("http://tempuri.org").Should().Be(v3);
            SchemaManager.GetSchemaVersion(null).Should().Be(v3);
            SchemaManager.GetSchemaVersion("abc").Should().Be(v3);
        }

        [TestMethod]
        public void SchemaManager_GetEdmxNamespaceManager_namespace_manager_with_correct_bindings()
        {
            // Only test Version3
            Version version = new Version(3, 0, 0, 0);

            var nsMgr = SchemaManager.GetEdmxNamespaceManager(new NameTable(), version);

            nsMgr.LookupNamespace("edmx").Should().Be(SchemaManager.GetEDMXNamespaceName(version));
            nsMgr.LookupNamespace("csdl").Should().Be(SchemaManager.GetCSDLNamespaceName(version));
            nsMgr.LookupNamespace("essg").Should().Be(SchemaManager.GetEntityStoreSchemaGeneratorNamespaceName());
            nsMgr.LookupNamespace("ssdl").Should().Be(SchemaManager.GetSSDLNamespaceName(version));
            nsMgr.LookupNamespace("msl").Should().Be(SchemaManager.GetMSLNamespaceName(version));
        }
    }
}
