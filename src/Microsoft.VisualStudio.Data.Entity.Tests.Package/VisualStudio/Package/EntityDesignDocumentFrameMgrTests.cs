// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio;
using Moq;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.Extensibility;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Model;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Package;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.Package
{
    [TestClass]
    public class EntityDesignDocumentFrameMgrTests
    {
        [TestMethod]
        public void OnBeforeLastDesignerDocumentUnlock_does_not_try_to_unload_artifact_it_does_not_own()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<VSArtifact> mockVsArtifact =
                new Mock<VSArtifact>(modelManager, new Uri("c:\\artifact.edmx"), modelProvider) { CallBase = true };

            Mock<LayerManager> mockLayerManager = new Mock<LayerManager>(mockVsArtifact.Object);

            mockVsArtifact.Setup(m => m.LayerManager).Returns(mockLayerManager.Object);

            using (EntityDesignDocumentFrameMgrTestDouble frameMgrMock = new EntityDesignDocumentFrameMgrTestDouble { Artifact = mockVsArtifact.Object })
            {
                frameMgrMock.OnBeforeLastDocumentUnlockInvoker(new Uri("urn:dummy"));
            }

            mockLayerManager.Verify(m => m.Unload(), Times.Never());
        }

        [TestMethod]
        public void OnBeforeLastDesignerDocumentUnlock_unloads_artifact_it_owns()
        {
            Uri artifactUri = new Uri("c:\\artifact.edmx");

            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<VSArtifact> mockVsArtifact =
                new Mock<VSArtifact>(modelManager, artifactUri, modelProvider) { CallBase = true };

            Mock<LayerManager> mockLayerManager = new Mock<LayerManager>(mockVsArtifact.Object);

            mockVsArtifact.Setup(m => m.LayerManager).Returns(mockLayerManager.Object);

            using (EntityDesignDocumentFrameMgrTestDouble frameMgrMock = new EntityDesignDocumentFrameMgrTestDouble { Artifact = mockVsArtifact.Object })
            {
                frameMgrMock.OnBeforeLastDocumentUnlockInvoker(artifactUri);
            }

            mockLayerManager.Verify(m => m.Unload(), Times.Once());
        }

        private class EntityDesignDocumentFrameMgrTestDouble : EntityDesignDocumentFrameMgr
        {
            public EFArtifact Artifact;

            public EntityDesignDocumentFrameMgrTestDouble()
                : base(new Mock<IXmlDesignerPackage>().Object)
            {
                
            }

            internal override EFArtifact CurrentArtifact { get { return Artifact; } }

            public void OnBeforeLastDocumentUnlockInvoker(Uri uri)
            {
                OnBeforeLastDesignerDocumentUnlock(uri);
            }
        }
    }
}
