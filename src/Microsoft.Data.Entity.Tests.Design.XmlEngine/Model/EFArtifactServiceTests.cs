// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Design.Model
{
    [TestClass]
    public class EFArtifactServiceTests
    {
        [TestMethod]
        public void EFArtifactService_returns_artifact_passed_in_ctor()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var artifact = new Mock<EFArtifact>(modelManager, new Uri("urn:dummy"), modelProvider).Object;

            new EFArtifactService(artifact).Artifact.Should().BeSameAs(artifact);
        }
    }
}
