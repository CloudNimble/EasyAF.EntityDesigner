// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Model.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;

namespace Microsoft.VisualStudio.Data.Entity.Tests.XmlDesigner.VisualStudio.Package
{
    [TestClass]
    public class EditingContextManagerTests
    {
        [TestMethod]
        public void Verify_CloseArtifact_removes_artifact_from_model_manager()
        {
            Uri artifactUri = new Uri("c:\\artifact.edmx");
            Mock<ModelManager> mockModelManager = new Mock<ModelManager>(null, null) { CallBase = true };
            Mock<EFArtifact> mockArtifact = new Mock<EFArtifact>(mockModelManager.Object, artifactUri, new Mock<XmlModelProvider>().Object);
            mockArtifact.Setup(a => a.Uri).Returns(artifactUri);

            mockModelManager
                .Setup(m => m.GetNewOrExistingArtifact(artifactUri, It.IsAny<XmlModelProvider>()))
                .Returns(mockArtifact.Object);
            mockModelManager
                .Setup(m => m.GetArtifact(artifactUri))
                .Returns(mockArtifact.Object);

            Mock<IXmlDesignerPackage> mockPackage = new Mock<IXmlDesignerPackage>();
            mockPackage.Setup(p => p.ModelManager).Returns(mockModelManager.Object);

            Mock<EditingContextManager> mockEditingContextMgr = new Mock<EditingContextManager>(mockPackage.Object) { CallBase = true };
            mockEditingContextMgr
                .Protected()
                .Setup<EFArtifact>("GetNewOrExistingArtifact", artifactUri)
                .Returns(mockArtifact.Object);

            var editingContext = mockEditingContextMgr.Object.GetNewOrExistingContext(artifactUri);

            editingContext.GetEFArtifactService().Should().NotBeNull();
            mockEditingContextMgr.Object.CloseArtifact(artifactUri);
            mockModelManager.Verify(m => m.ClearArtifact(artifactUri), Times.Once());
            editingContext.GetEFArtifactService().Should().BeNull();
        }
    }
}
