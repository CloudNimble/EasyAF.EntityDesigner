// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using FluentAssertions;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Model;
using Microsoft.VisualStudio.Data.Entity.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using VSLangProj;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.Model
{
    [TestClass]
    public class StandaloneXmlModelProviderTests
    {
        [TestMethod]
        public void TryGetBufferViaExtensions_returns_false_when_converter_is_present_but_transformer_is_absent_for_non_edmx_file()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);
            mockProjectItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns("non-edmx-file.xmde");

            Mock<IEntityDesignerConversionData> mockConversionData = new Mock<IEntityDesignerConversionData>();
            mockConversionData.SetupGet(d => d.FileExtension).Returns("xmde");

            Mock<IModelConversionExtension> mockConversionExtension = new Mock<IModelConversionExtension>();
            mockConversionExtension
                .Setup(e => e.OnAfterFileLoaded(It.IsAny<ModelConversionExtensionContext>()))
                .Callback<ModelConversionExtensionContext>(
                    ctx =>
                        {
                            ctx.EntityFrameworkVersion.Should().Be(new Version(3, 0, 0, 0));
                            ctx.FileInfo.Name.Should().Be("non-edmx-file.xmde");
                            ctx.ProjectItem.Should().BeSameAs(mockProjectItem.Object);
                            ctx.Project.Should().BeSameAs(mockDte.Project);
                        });

            Lazy<IModelConversionExtension, IEntityDesignerConversionData> converter =
                new Lazy<IModelConversionExtension, IEntityDesignerConversionData>(
                    () => mockConversionExtension.Object, mockConversionData.Object);

            StandaloneXmlModelProvider.TryGetBufferViaExtensions(
                mockDte.ServiceProvider, mockProjectItem.Object, string.Empty, new[] { converter },
                new Lazy<IModelTransformExtension>[0], out string outputDocument, out List<ExtensionError> errors).Should().BeFalse();

            outputDocument.Should().BeEmpty();
            errors.Should().BeEmpty();

            mockConversionExtension.Verify(e => e.OnAfterFileLoaded(It.IsAny<ModelConversionExtensionContext>()), Times.Once());
            mockConversionExtension.Verify(e => e.OnBeforeFileSaved(It.IsAny<ModelConversionExtensionContext>()), Times.Never());
            mockConversionData.Verify(e => e.FileExtension, Times.Once());
        }

        [TestMethod]
        // note that this may not be the desired behavior see: https://entityframework.codeplex.com/workitem/1371
        public void TryGetBufferViaExtensions_throws_when_converter_is_absent_for_non_edmx_file()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);
            mockProjectItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns("non-edmx-file.xmde");

            // need to pass a transformer since there must be at least onve converter or transformer
            // and the test tests a case where converter does not exist
            Mock<IModelTransformExtension> mockTransformExtension = new Mock<IModelTransformExtension>();
            Lazy<IModelTransformExtension> transformer = new Lazy<IModelTransformExtension>(() => mockTransformExtension.Object);


            Action action = () => StandaloneXmlModelProvider.TryGetBufferViaExtensions(
                mockDte.ServiceProvider, mockProjectItem.Object, "<root/>",
                new Lazy<IModelConversionExtension, IEntityDesignerConversionData>[0],
                new[] { transformer }, out string outputDocument, out List<ExtensionError> errors);

            action.Should().Throw<InvalidOperationException>()
                .WithMessage(EdmxDesignerResources.Extensibility_NoConverterForExtension);
        }

        [TestMethod]
        public void TryGetBufferViaExtensions_returns_false_when_transformer_does_not_modify_original_document()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);
            mockProjectItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns("model.edmx");

            Mock<IModelTransformExtension> mockTransformExtension = new Mock<IModelTransformExtension>();
            mockTransformExtension
                .Setup(e => e.OnAfterModelLoaded(It.IsAny<ModelTransformExtensionContext>()))
                .Callback<ModelTransformExtensionContext>(
                    ctx =>
                        {
                            ctx.EntityFrameworkVersion.Should().Be(new Version(3, 0, 0, 0));
                            XNode.DeepEquals(ctx.OriginalDocument, XDocument.Parse("<root />")).Should().BeTrue();
                            ctx.ProjectItem.Should().BeSameAs(mockProjectItem.Object);
                            ctx.Project.Should().BeSameAs(mockDte.Project);
                        });

            Lazy<IModelTransformExtension> transformer = new Lazy<IModelTransformExtension>(() => mockTransformExtension.Object);

            StandaloneXmlModelProvider.TryGetBufferViaExtensions(
                mockDte.ServiceProvider, mockProjectItem.Object, "<root/>",
                new Lazy<IModelConversionExtension, IEntityDesignerConversionData>[0],
                new[] { transformer }, out string outputDocument, out List<ExtensionError> errors).Should().BeFalse();

            outputDocument.Should().BeEmpty();
            errors.Should().BeEmpty();

            mockTransformExtension.Verify(
                e => e.OnAfterModelLoaded(It.IsAny<ModelTransformExtensionContext>()), Times.Once());
            mockTransformExtension.Verify(
                e => e.OnBeforeModelSaved(It.IsAny<ModelTransformExtensionContext>()), Times.Never());
        }

        [TestMethod]
        public void TryGetBufferViaExtensions_returns_true_when_transformer_is_present_but_converter_is_absent_for_edmx_file()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);
            mockProjectItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns("model.edmx");

            Mock<IModelTransformExtension> mockTransformExtension = new Mock<IModelTransformExtension>();
            mockTransformExtension
                .Setup(e => e.OnAfterModelLoaded(It.IsAny<ModelTransformExtensionContext>()))
                .Callback<ModelTransformExtensionContext>(
                    ctx =>
                        {
                            ctx.EntityFrameworkVersion.Should().Be(new Version(3, 0, 0, 0));
                            XNode.DeepEquals(ctx.OriginalDocument, XDocument.Parse("<root />")).Should().BeTrue();
                            ctx.ProjectItem.Should().BeSameAs(mockProjectItem.Object);
                            ctx.Project.Should().BeSameAs(mockDte.Project);

                            XDocument modifiedDocument = new XDocument(ctx.OriginalDocument);
                            modifiedDocument.Root.Add(new XAttribute("test", "value"));
                            ctx.CurrentDocument = modifiedDocument;
                        });

            Lazy<IModelTransformExtension> transformer = new Lazy<IModelTransformExtension>(() => mockTransformExtension.Object);

            StandaloneXmlModelProvider.TryGetBufferViaExtensions(
                mockDte.ServiceProvider, mockProjectItem.Object, "<root/>",
                new Lazy<IModelConversionExtension, IEntityDesignerConversionData>[0],
                new[] { transformer }, out string outputDocument, out List<ExtensionError> errors).Should().BeTrue();

            XNode.DeepEquals(XDocument.Parse("<root test=\"value\" />"), XDocument.Parse(outputDocument)).Should().BeTrue();
            errors.Should().BeEmpty();

            mockTransformExtension.Verify(
                e => e.OnAfterModelLoaded(It.IsAny<ModelTransformExtensionContext>()), Times.Once());
            mockTransformExtension.Verify(
                e => e.OnBeforeModelSaved(It.IsAny<ModelTransformExtensionContext>()), Times.Never());
        }

        // [TestMethod] https://entityframework.codeplex.com/workitem/1371
        public void TryGetBufferViaExtensions_passes_content_from_converter_to_transformer_and_returns_true()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);
            mockProjectItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns("non-edmx-file.xmde");

            // converter setup
            Mock<IEntityDesignerConversionData> mockConversionData = new Mock<IEntityDesignerConversionData>();
            mockConversionData.SetupGet(d => d.FileExtension).Returns("xmde");

            Mock<IModelConversionExtension> mockConversionExtension = new Mock<IModelConversionExtension>();
            mockConversionExtension
                .Setup(e => e.OnAfterFileLoaded(It.IsAny<ModelConversionExtensionContext>()))
                .Callback<ModelConversionExtensionContext>(
                    ctx =>
                        {
                            ctx.EntityFrameworkVersion.Should().Be(new Version(3, 0, 0, 0));
                            ctx.FileInfo.Name.Should().Be("non-edmx-file.xmde");
                            ctx.ProjectItem.Should().BeSameAs(mockProjectItem.Object);
                            ctx.Project.Should().BeSameAs(mockDte.Project);

                            // https://entityframework.codeplex.com/workitem/1371
                            // ctx.CurrentDocument = "<root />";
                        });

            // transformer setup
            Mock<IModelTransformExtension> mockTransformExtension = new Mock<IModelTransformExtension>();
            mockTransformExtension
                .Setup(e => e.OnAfterModelLoaded(It.IsAny<ModelTransformExtensionContext>()))
                .Callback<ModelTransformExtensionContext>(
                    ctx =>
                        {
                            ctx.EntityFrameworkVersion.Should().Be(new Version(3, 0, 0, 0));
                            XNode.DeepEquals(ctx.OriginalDocument, XDocument.Parse("<root />")).Should().BeTrue();
                            ctx.ProjectItem.Should().BeSameAs(mockProjectItem.Object);
                            ctx.Project.Should().BeSameAs(mockDte.Project);

                            XDocument modifiedDocument = new XDocument(ctx.OriginalDocument);
                            modifiedDocument.Root.Add(new XAttribute("test", "value"));
                            ctx.CurrentDocument = modifiedDocument;
                        });

            Lazy<IModelConversionExtension, IEntityDesignerConversionData> converter =
                new Lazy<IModelConversionExtension, IEntityDesignerConversionData>(
                    () => mockConversionExtension.Object, mockConversionData.Object);
            Lazy<IModelTransformExtension> transformer = new Lazy<IModelTransformExtension>(() => mockTransformExtension.Object);

            StandaloneXmlModelProvider.TryGetBufferViaExtensions(
                mockDte.ServiceProvider, mockProjectItem.Object, string.Empty,
                new[] { converter }, new[] { transformer }, out string outputDocument, out List<ExtensionError> errors).Should().BeTrue();

            XNode.DeepEquals(XDocument.Parse("<root test=\"value\" />"), XDocument.Parse(outputDocument)).Should().BeTrue();
            errors.Should().BeEmpty();

            mockConversionExtension.Verify(
                e => e.OnAfterFileLoaded(It.IsAny<ModelConversionExtensionContext>()), Times.Once());
            mockConversionExtension.Verify(
                e => e.OnBeforeFileSaved(It.IsAny<ModelConversionExtensionContext>()), Times.Never());

            mockTransformExtension.Verify(
                e => e.OnAfterModelLoaded(It.IsAny<ModelTransformExtensionContext>()), Times.Once());
            mockTransformExtension.Verify(
                e => e.OnBeforeModelSaved(It.IsAny<ModelTransformExtensionContext>()), Times.Never());

            mockConversionData.Verify(e => e.FileExtension, Times.Exactly(2));
        }

        [TestMethod]
        public void Build_creates_annotated_XDocument()
        {
            var serviceProvider = new Mock<IServiceProvider>().Object;
            Mock<StandaloneXmlModelProvider> mockModelProvider = new Mock<StandaloneXmlModelProvider>(serviceProvider) { CallBase = true };
            mockModelProvider.Protected()
                .Setup<string>("ReadEdmxContents", ItExpr.IsAny<Uri>())
                .Returns("<root>\n<child />\n</root>");

            var xDoc = mockModelProvider.Object.GetXmlModel(new Uri("z:\\model.edmx")).Document;

            xDoc.Descendants().All(
                e =>
                    {
                        var textRange = e.GetTextRange();
                        return textRange != null && textRange.OpenStartLine > 0 && textRange.OpenStartColumn > 0;
                    }).Should().BeTrue();
        }
    }
}
