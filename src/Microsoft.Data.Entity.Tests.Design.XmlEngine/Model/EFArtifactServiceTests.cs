// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Microsoft.Data.Entity.Tests.Design.XmlEngine.Model
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
