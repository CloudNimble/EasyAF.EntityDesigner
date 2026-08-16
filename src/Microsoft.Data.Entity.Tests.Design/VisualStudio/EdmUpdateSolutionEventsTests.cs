// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Data.Entity.Design.Ide;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio
{
    [TestClass]
    public class EdmUpdateSolutionEventsTests
    {
        [TestMethod]
        public void ShouldValidateArtifactDuringBuild_returns_true_if_no_designer_info()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            var entityDesignArtifact =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider).Object;

            EdmUpdateSolutionEvents.ShouldValidateArtifactDuringBuild(entityDesignArtifact).Should().BeTrue();
        }

        [TestMethod]
        public void ShouldValidateArtifactDuringBuild_returns_true_if_designer_info_does_not_contain_options()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("root")))
            {
                entityDesignArtifactMock
                    .Setup(a => a.DesignerInfo)
                    .Returns(designerInfoRoot);

                EdmUpdateSolutionEvents.ShouldValidateArtifactDuringBuild(entityDesignArtifactMock.Object).Should().BeTrue();
            }
        }

        [TestMethod]
        public void ShouldValidateArtifactDuringBuild_returns_true_if_designer_info_option_does_not_contain_ValidateOnBuild_property()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                designerInfoRoot
                    .AddDesignerInfo(
                        "Options",
                        new Mock<OptionsDesignerInfo>(designerInfoRoot, new XElement("_")).Object);

                entityDesignArtifactMock
                    .Setup(a => a.DesignerInfo)
                    .Returns(designerInfoRoot);

                EdmUpdateSolutionEvents.ShouldValidateArtifactDuringBuild(entityDesignArtifactMock.Object).Should().BeTrue();
            }
        }

        [TestMethod]
        public void ShouldValidateArtifactDuringBuild_returns_true_if_designer_info_option_does_not_contain_ValidateOnBuild_value()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                using (DesignerProperty designerProperty = new DesignerProperty(null, new XElement("_")))
                {
                    Mock<OptionsDesignerInfo> optionsDesignerInfoMock = new Mock<OptionsDesignerInfo>(designerInfoRoot, new XElement("_"));
                    optionsDesignerInfoMock
                        .Setup(o => o.ValidateOnBuild)
                        .Returns(designerProperty);

                    designerInfoRoot
                        .AddDesignerInfo(
                            "Options",
                            optionsDesignerInfoMock.Object);

                    entityDesignArtifactMock
                        .Setup(a => a.DesignerInfo)
                        .Returns(designerInfoRoot);

                    EdmUpdateSolutionEvents.ShouldValidateArtifactDuringBuild(entityDesignArtifactMock.Object).Should().BeTrue();
                }
            }
        }

        [TestMethod]
        public void ShouldValidateArtifactDuringBuild_returns_false_if_ValidateOnBuild_set_to_false()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                using (
                    DesignerProperty designerProperty =
                        new DesignerProperty(
                            null,
                            new XElement("_", new XAttribute("Value", "false"))))
                {
                    Mock<OptionsDesignerInfo> optionsDesignerInfoMock = new Mock<OptionsDesignerInfo>(designerInfoRoot, new XElement("_"));
                    optionsDesignerInfoMock
                        .Setup(o => o.ValidateOnBuild)
                        .Returns(designerProperty);

                    designerInfoRoot
                        .AddDesignerInfo(
                            "Options",
                            optionsDesignerInfoMock.Object);

                    entityDesignArtifactMock
                        .Setup(a => a.DesignerInfo)
                        .Returns(designerInfoRoot);

                    EdmUpdateSolutionEvents.ShouldValidateArtifactDuringBuild(entityDesignArtifactMock.Object).Should().BeFalse();
                }
            }
        }

        [TestMethod]
        public void ShouldValidateArtifactDuringBuild_returns_true_if_ValidateOnBuild_set_to_true()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                using (
                    DesignerProperty designerProperty =
                        new DesignerProperty(
                            null,
                            new XElement("_", new XAttribute("Value", "true"))))
                {
                    Mock<OptionsDesignerInfo> optionsDesignerInfoMock = new Mock<OptionsDesignerInfo>(designerInfoRoot, new XElement("_"));
                    optionsDesignerInfoMock
                        .Setup(o => o.ValidateOnBuild)
                        .Returns(designerProperty);

                    designerInfoRoot
                        .AddDesignerInfo(
                            "Options",
                            optionsDesignerInfoMock.Object);

                    entityDesignArtifactMock
                        .Setup(a => a.DesignerInfo)
                        .Returns(designerInfoRoot);

                    EdmUpdateSolutionEvents.ShouldValidateArtifactDuringBuild(entityDesignArtifactMock.Object).Should().BeTrue();
                }
            }
        }

        [TestMethod]
        public void ShouldValidateArtifactDuringBuild_returns_true_if_ValidateOnBuild_not_a_valid_bool()
        {
            var modelManager = new Mock<ModelManager>(null, null).Object;
            var modelProvider = new Mock<XmlModelProvider>().Object;
            Mock<EntityDesignArtifact> entityDesignArtifactMock =
                new Mock<EntityDesignArtifact>(modelManager, new Uri("urn:dummy"), modelProvider);

            using (EFDesignerInfoRoot designerInfoRoot = new EFDesignerInfoRoot(entityDesignArtifactMock.Object, new XElement("_")))
            {
                using (
                    DesignerProperty designerProperty =
                        new DesignerProperty(
                            null,
                            new XElement("_", new XAttribute("Value", "abc"))))
                {
                    Mock<OptionsDesignerInfo> optionsDesignerInfoMock = new Mock<OptionsDesignerInfo>(designerInfoRoot, new XElement("_"));
                    optionsDesignerInfoMock
                        .Setup(o => o.ValidateOnBuild)
                        .Returns(designerProperty);

                    designerInfoRoot
                        .AddDesignerInfo(
                            "Options",
                            optionsDesignerInfoMock.Object);

                    entityDesignArtifactMock
                        .Setup(a => a.DesignerInfo)
                        .Returns(designerInfoRoot);

                    EdmUpdateSolutionEvents.ShouldValidateArtifactDuringBuild(entityDesignArtifactMock.Object).Should().BeTrue();
                }
            }
        }
    }
}
