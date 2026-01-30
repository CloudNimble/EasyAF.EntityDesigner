// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.EntityDesigner.View;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Model.Visitor;

namespace Microsoft.Data.Entity.Tests.Design.EntityDesigner.View
{
    /// <summary>
    ///     Tests for EntityDesignerDiagram.
    ///     Note: Only UPGRADE to Version3 (EF6) is now supported. Downgrade tests have been removed.
    /// </summary>
    [TestClass]
    public class EntityDesignerDiagramTests
    {
        [TestMethod]
        public void ReversionModel_upgrades_model_namespaces_to_Version3()
        {
            // Test that ReversionModel works with Version3 (only supported version)
            var model = CreateModel(EntityFrameworkVersion.Version3);

            Uri tempUri = new Uri("http://tempuri");

            Mock<ModelManager> mockModelManager =
                new Mock<ModelManager>(new Mock<IEFArtifactFactory>().Object, new Mock<IEFArtifactSetFactory>().Object);

            using (var modelManager = mockModelManager.Object)
            {
                Mock<XmlModelProvider> mockModelProvider = new Mock<XmlModelProvider>();
                mockModelProvider
                    .Setup(p => p.BeginTransaction(It.IsAny<string>(), It.IsAny<object>()))
                    .Returns(new Mock<XmlTransaction>().Object);

                Mock<DocumentFrameMgr> mockFrameManager = new Mock<DocumentFrameMgr>(new Mock<IXmlDesignerPackage>().Object);
                Mock<IEdmPackage> mockPackage = new Mock<IEdmPackage>();
                mockPackage.Setup(p => p.DocumentFrameMgr).Returns(mockFrameManager.Object);

                Mock<EditingContext> mockEditingContext = new Mock<EditingContext>();
                Mock<EntityDesignArtifact> mockArtifact = new Mock<EntityDesignArtifact>(modelManager, tempUri, mockModelProvider.Object);
                mockArtifact.Setup(a => a.CanEditArtifact()).Returns(true);
                mockArtifact.Setup(a => a.XDocument).Returns(model);

#if DEBUG
                mockArtifact
                    .Setup(
                        a => a.GetVerifyModelIntegrityVisitor(
                            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(new Mock<VerifyModelIntegrityVisitor>().Object);
#endif

                mockArtifact.Object.IsDirty.Should().BeFalse();

                // Upgrade from V2 to V3
                EntityDesignerDiagram.ReversionModel(
                    mockPackage.Object, mockEditingContext.Object, mockArtifact.Object, EntityFrameworkVersion.Version3);

                mockArtifact.Object.IsDirty.Should().BeTrue();
                XNode.DeepEquals(CreateModel(EntityFrameworkVersion.Version3), model).Should().BeTrue();
                mockFrameManager.Verify(m => m.SetCurrentContext(mockEditingContext.Object), Times.Once());
            }
        }

        private static XDocument CreateModel(Version targetSchemaVersion)
        {
            const string edmxTemplate =
                @"<!-- edmx -->
<edmx:Edmx Version=""{0}"" xmlns:edmx=""{1}"">
  <edmx:Runtime>
    <edmx:StorageModels>
      <Schema xmlns=""{2}"" />
    </edmx:StorageModels>
    <edmx:ConceptualModels>
      <Schema xmlns=""{3}"" />
    </edmx:ConceptualModels>
    <!-- C-S mapping content -->
    <edmx:Mappings>
      <Mapping xmlns=""{4}"" />
    </edmx:Mappings>
  </edmx:Runtime>
</edmx:Edmx>";

            return
                XDocument.Parse(
                    string.Format(
                        edmxTemplate,
                        targetSchemaVersion.ToString(2),
                        SchemaManager.GetEDMXNamespaceName(targetSchemaVersion),
                        SchemaManager.GetCSDLNamespaceName(targetSchemaVersion),
                        SchemaManager.GetSSDLNamespaceName(targetSchemaVersion),
                        SchemaManager.GetMSLNamespaceName(targetSchemaVersion)));
        }
    }
}
