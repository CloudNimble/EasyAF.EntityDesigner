// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Data.Entity.Package;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using VSLangProj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Resources = Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Resources;
using Microsoft.VisualStudio.Data.Entity.Extensibility;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility;

namespace Microsoft.VisualStudio.Data.Entity.Tests.Package
{
    [TestClass]
    public class MicrosoftDataEntityDesignDocDataTests
    {
        [TestMethod]
        public void DispatchSaveToExtensions_returns_passed_string_if_passed_string_is_not_valid_xml()
        {
            Mock<IServiceProvider> mockServicePrvider = new Mock<IServiceProvider>();

            const string fileContents = "invalid edmx";

            MicrosoftDataEntityDesignDocData docData = new MicrosoftDataEntityDesignDocData(mockServicePrvider.Object, Guid.NewGuid());
            docData.OnRegisterDocData(42, new Mock<IVsHierarchy>().Object, 3);
            docData.DispatchSaveToExtensions(
                    mockServicePrvider.Object, new Mock<ProjectItem>().Object, fileContents,
                    new Lazy<IModelConversionExtension, IEntityDesignerConversionData>[1],
                    new Lazy<IModelTransformExtension>[0]).Should().BeSameAs(fileContents);
        }

        [TestMethod]
        public void DispatchSaveToExtensions_invokes_serializers_if_present()
        {
            XDocument inputDocument = XDocument.Parse("<model />");
            XDocument updatedDocument = XDocument.Parse("<model x=\"1\" />");

            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);

            Mock<IModelTransformExtension> mockSerializerExtension = new Mock<IModelTransformExtension>();
            mockSerializerExtension
                .Setup(e => e.OnBeforeModelSaved(It.IsAny<ModelTransformExtensionContext>()))
                .Callback<ModelTransformExtensionContext>(
                    context =>
                        {
                            context.EntityFrameworkVersion.Should().Be(new Version(3, 0, 0, 0));
                            XNode.DeepEquals(inputDocument, context.OriginalDocument).Should().BeTrue();
                            context.CurrentDocument = updatedDocument;
                        });

            MicrosoftDataEntityDesignDocData docData = new MicrosoftDataEntityDesignDocData(mockDte.ServiceProvider, Guid.NewGuid());
            docData.RenameDocData(0, mockDte.Hierarchy, 3, "model.edmx");

            var result = docData.DispatchSaveToExtensions(
                mockDte.ServiceProvider, mockProjectItem.Object, inputDocument.ToString(),
                new Lazy<IModelConversionExtension, IEntityDesignerConversionData>[1],
                new[] { new Lazy<IModelTransformExtension>(() => mockSerializerExtension.Object) });

            XNode.DeepEquals(updatedDocument, XDocument.Parse(result)).Should().BeTrue();

            mockSerializerExtension.Verify(
                e => e.OnAfterModelLoaded(It.IsAny<ModelTransformExtensionContext>()), Times.Never());
            mockSerializerExtension.Verify(
                e => e.OnBeforeModelSaved(It.IsAny<ModelTransformExtensionContext>()), Times.Once());
        }

#if (VS11 || VS12) // TODO: uncomment this when figure out why VS14 runtime does not allow callback at line 84
        [TestMethod]
        public void DispatchSaveToExtensions_invokes_converter_for_non_edmx_files_if_present()
        {
            var mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            var mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);

            var mockConversionExtension = new Mock<IModelConversionExtension>();
            mockConversionExtension
                .Setup(c => c.OnBeforeFileSaved(It.IsAny<ModelConversionExtensionContext>()))
                .Callback<ModelConversionExtensionContext>(
                    context =>
                        {
                            context.EntityFrameworkVersion.Should().Be(new Version(3, 0, 0, 0));
                            context.OriginalDocument = "my model";
                        });

            var mockConversionData = new Mock<IEntityDesignerConversionData>();
            mockConversionData.Setup(d => d.FileExtension).Returns("xmde");

            var docData = new MicrosoftDataEntityDesignDocData(mockDte.ServiceProvider, Guid.NewGuid());
            docData.RenameDocData(0, mockDte.Hierarchy, 3, "model.xmde");

            docData.DispatchSaveToExtensions(
                    mockDte.ServiceProvider, mockProjectItem.Object, "<model />",
                    new[]
                        {
                            new Lazy<IModelConversionExtension, IEntityDesignerConversionData>(
                                () => mockConversionExtension.Object, mockConversionData.Object),
                        },
                    new Lazy<IModelTransformExtension>[0]).Should().BeSameAs("my model");

            mockConversionExtension.Verify(
                e => e.OnAfterFileLoaded(It.IsAny<ModelConversionExtensionContext>()), Times.Never());
            mockConversionExtension.Verify(
                e => e.OnBeforeFileSaved(It.IsAny<ModelConversionExtensionContext>()), Times.Once());
        }
#endif

        [TestMethod]
        public void DispatchSaveToExtensions_throws_for_non_edmx_if_converter_is_missing()
        {
            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);
            Mock<ProjectItem> mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.SetupGet(i => i.ContainingProject).Returns(mockDte.Project);

            MicrosoftDataEntityDesignDocData docData = new MicrosoftDataEntityDesignDocData(mockDte.ServiceProvider, Guid.NewGuid());
            docData.RenameDocData(0, mockDte.Hierarchy, 3, "model.xmde");

            Mock<IModelTransformExtension> mockSerializerExtension = new Mock<IModelTransformExtension>();

            Action act = () => docData.DispatchSaveToExtensions(
                        mockDte.ServiceProvider, mockProjectItem.Object, "<model />",
                        new Lazy<IModelConversionExtension, IEntityDesignerConversionData>[0],
                        new[] { new Lazy<IModelTransformExtension>(() => mockSerializerExtension.Object) });
            act.Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.Extensibility_NoConverterForExtension);
        }
    }
}
