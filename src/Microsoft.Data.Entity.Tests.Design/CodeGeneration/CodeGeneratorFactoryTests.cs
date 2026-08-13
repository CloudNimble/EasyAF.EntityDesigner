using System.Collections;
using EnvDTE;
using FluentAssertions;
using Microsoft.Data.Entity.Design.CodeGeneration;
using Microsoft.Data.Entity.Design.Common;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using VSLangProj;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Generators;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class CodeGeneratorFactoryTests
    {
        [TestMethod]
        public void GetContextGenerator_returns_correct_context_generator_for_empty_model()
        {
            CodeGeneratorFactory generatorFactory = new CodeGeneratorFactory(Mock.Of<Project>());

            generatorFactory.GetContextGenerator(LangEnum.CSharp, isEmptyModel: true)
                .Should().BeOfType<CSharpCodeFirstEmptyModelGenerator>();
            generatorFactory.GetContextGenerator(LangEnum.VisualBasic, isEmptyModel: true)
                .Should().BeOfType<VBCodeFirstEmptyModelGenerator>();
        }

        [TestMethod]
        public void GetContextGenerator_returns_correct_non_customized_context_generator_if_model_not_emtpy()
        {
            CodeGeneratorFactory generatorFactory = new CodeGeneratorFactory(Mock.Of<Project>());

            generatorFactory.GetContextGenerator(LangEnum.CSharp, isEmptyModel: false)
                .Should().BeOfType<DefaultCSharpContextGenerator>();
            generatorFactory.GetContextGenerator(LangEnum.VisualBasic, isEmptyModel: false)
                .Should().BeOfType<DefaultVBContextGenerator>();
        }

        [TestMethod]
        public void GetContextGenerator_returns_correct_customized__context_generator_if_model_not_emtpy_CS()
        {
            var mockDte = SetupMockProjectWithCustomizedTemplate(@"CodeTemplates\EFModelFromDatabase\Context.cs.t4");

            CodeGeneratorFactory generatorFactory = new CodeGeneratorFactory(mockDte.Project);

            generatorFactory.GetContextGenerator(LangEnum.CSharp, isEmptyModel: false)
                .Should().BeOfType<CustomGenerator>();
        }

        [TestMethod]
        public void GetContextGenerator_returns_correct_customized_context_generator_if_model_not_emtpy_VB()
        {
            var mockDte = SetupMockProjectWithCustomizedTemplate(@"CodeTemplates\EFModelFromDatabase\Context.vb.t4");

            CodeGeneratorFactory generatorFactory = new CodeGeneratorFactory(mockDte.Project);

            generatorFactory.GetContextGenerator(LangEnum.VisualBasic, isEmptyModel: false)
                .Should().BeOfType<CustomGenerator>();
        }

        [TestMethod]
        public void GetEntityTypeGenerator_returns_correct_non_customized_entity_type_generator()
        {
            CodeGeneratorFactory generatorFactory = new CodeGeneratorFactory(Mock.Of<Project>());

            generatorFactory.GetEntityTypeGenerator(LangEnum.CSharp)
                .Should().BeOfType<DefaultCSharpEntityTypeGenerator>();
            generatorFactory.GetEntityTypeGenerator(LangEnum.VisualBasic)
                .Should().BeOfType<DefaultVBEntityTypeGenerator>();
        }

        [TestMethod]
        public void GetEntityTypeGenerator_returns_correct_customized_entity_type_generator_CS()
        {
            var mockDte = SetupMockProjectWithCustomizedTemplate(@"CodeTemplates\EFModelFromDatabase\Entity.cs.t4");

            CodeGeneratorFactory generatorFactory = new CodeGeneratorFactory(mockDte.Project);

            generatorFactory.GetEntityTypeGenerator(LangEnum.CSharp)
                .Should().BeOfType<CustomGenerator>();
        }

        [TestMethod]
        public void GetEntityTypeGenerator_returns_correct_customized_entity_type_generator_VB()
        {
            var mockDte = SetupMockProjectWithCustomizedTemplate(@"CodeTemplates\EFModelFromDatabase\Entity.vb.t4");

            CodeGeneratorFactory generatorFactory = new CodeGeneratorFactory(mockDte.Project);

            generatorFactory.GetEntityTypeGenerator(LangEnum.CSharp)
                .Should().BeOfType<CustomGenerator>();
        }

        private static MockDTE SetupMockProjectWithCustomizedTemplate(string templatePath)
        {
            Mock<ProjectItem> mockChildProjectItem = null;

            foreach (var step in Enumerable.Reverse(templatePath.Split('\\')))
            {
                var mockProjectItem = MockDTE.CreateProjectItem(step);

                Mock<Property> mockProperty = new Mock<Property>();
                mockProperty.Setup(p => p.Name).Returns("FullPath");
                mockProperty.Setup(p => p.Value).Returns(step);

                Mock<Properties> mockProperties = new Mock<Properties>();
                mockProperties.As<IEnumerable>()
                    .Setup(p => p.GetEnumerator())
                    .Returns(() => new[] { mockProperty.Object }.GetEnumerator());

                mockProjectItem.
                    Setup(i => i.Properties)
                    .Returns(mockProperties.Object);

                mockProjectItem
                    .Setup(p => p.ProjectItems)
                    .Returns(CreateProjectItems(mockChildProjectItem?.Object));

                mockChildProjectItem = mockProjectItem;
            }

            MockDTE mockDte = new MockDTE(".NETFramework, Version=v4.5", references: new Reference[0]);

            mockChildProjectItem
                .SetupGet(i => i.ContainingProject)
                .Returns(mockDte.Project);

            Mock.Get(mockDte.Project)
                .Setup(p => p.ProjectItems)
                .Returns(CreateProjectItems(mockChildProjectItem.Object));

            return mockDte;
        }

        private static ProjectItems CreateProjectItems(ProjectItem childProjectItem)
        {
            Mock<ProjectItems> mockProjectItems = new Mock<ProjectItems>();

            if (childProjectItem != null)
            {
                mockProjectItems
                    .Setup(p => p.Item(It.IsAny<object>()))
                    .Returns(childProjectItem);
            }

            return mockProjectItems.Object;
        }

    }
}
