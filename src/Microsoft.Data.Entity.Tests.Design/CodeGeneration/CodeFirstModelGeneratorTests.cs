// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.Data.Entity.Tests.Design.TestHelpers;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Resources = Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Properties.Resources;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class CodeFirstModelGeneratorTests
    {
        private static DbModel _model;

        private static DbModel Model
        {
            get
            {
                if (_model == null)
                {
                    DbModelBuilder modelBuilder = new DbModelBuilder();
                    modelBuilder.Entity<Entity>();
                    _model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
                }

                return _model;
            }
        }

        [TestMethod]
        public void Generate_returns_code_when_cs()
        {
            CodeFirstModelGenerator generator = new CodeFirstModelGenerator(MockDTE.CreateProject());

            var files = generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString").ToArray();

            files.Length.Should().Be(2);
            files.Select(p => p.Key).Should().BeEquivalentTo(new[] { "MyContext.cs", "Entity.cs" });
        }

        [TestMethod]
        public void Generate_returns_code_when_cs_and_empty_model()
        {
            CodeFirstModelGenerator generator = new CodeFirstModelGenerator(MockDTE.CreateProject());

            var files = generator.Generate(null, "WebApplication1.Models", "MyContext", "MyContextConnString").ToArray();

            files.Length.Should().Be(1);
            files[0].Key.Should().Be("MyContext.cs");
            files[0].Value.Should().Contain(Resources.CodeFirstCodeFile_DbSetComment_CS);
        }

        [TestMethod]
        public void Generate_returns_code_when_vb()
        {
            CodeFirstModelGenerator generator = new CodeFirstModelGenerator(MockDTE.CreateProject(kind: MockDTE.VBProjectKind));

            var files = generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString").ToArray();

            files.Length.Should().Be(2);
            files.Select(p => p.Key).Should().BeEquivalentTo(new[] { "MyContext.vb", "Entity.vb" });
        }

        [TestMethod]
        public void Generate_returns_code_when_vb_and_empty_model()
        {
            CodeFirstModelGenerator generator = new CodeFirstModelGenerator(MockDTE.CreateProject(kind: MockDTE.VBProjectKind));

            var files = generator.Generate(null, "WebApplication1.Models", "MyContext", "MyContextConnString").ToArray();

            files.Length.Should().Be(1);
            files[0].Key.Should().Be("MyContext.vb");
            files[0].Value.Should().Contain(Resources.CodeFirstCodeFile_DbSetComment_VB);
        }

        // Test stopped working with 15.6 Preview 7 - plan to re-enable with https://github.com/aspnet/EntityFramework6/issues/541
        // [TestMethod]
        public void Generate_returns_code_when_cs_and_customized()
        {
            var project = MockDTE.CreateProject();

            using (AddCustomizedTemplates(project))
            {
                CodeFirstModelGenerator generator = new CodeFirstModelGenerator(project);

                var files = generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString").ToArray();

                files.Length.Should().Be(2);
                files.Select(p => p.Key).Should().BeEquivalentTo(new[] { "MyContext.cs", "Entity.cs" });
                files.All(p => p.Value == "Customized").Should().BeTrue();
            }
        }

        // Test stopped working with 15.6 Preview 7 - plan to re-enable with https://github.com/aspnet/EntityFramework6/issues/541
        // [TestMethod]
        public void Generate_returns_code_when_vb_and_customized()
        {
            var project = MockDTE.CreateProject(kind: MockDTE.VBProjectKind);

            using (AddCustomizedTemplates(project))
            {
                CodeFirstModelGenerator generator = new CodeFirstModelGenerator(project);

                var files = generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString").ToArray();

                files.Length.Should().Be(2);
                files.Select(p => p.Key).Should().BeEquivalentTo(new[] { "MyContext.vb", "Entity.vb" });
                files.All(p => p.Value == "Customized").Should().BeTrue();
            }
        }

        // Test stopped working with 15.6 Preview 7 - plan to re-enable with https://github.com/aspnet/EntityFramework6/issues/541
        // [TestMethod]
        public void Generate_throws_when_error_in_context_template()
        {
            var project = MockDTE.CreateProject();
            var token = Guid.NewGuid().ToString();

            using (AddCustomizedTemplates(project))
            {
                var templatePath = Path.GetTempFileName();
                try
                {
                    File.WriteAllText(templatePath, "<# throw new Exception(\"" + token + "\"); #>");

                    var fullPathProperty = project.ProjectItems.Item("CodeTemplates").ProjectItems
                        .Item("EFModelFromDatabase").ProjectItems.Item("Context.cs.t4").Properties.Cast<Property>()
                        .First(i => i.Name == "FullPath");
                    Mock.Get(fullPathProperty).SetupGet(p => p.Value).Returns(templatePath);

                    CodeFirstModelGenerator generator = new CodeFirstModelGenerator(project);

                    Action act = () => generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString")
                        .ToArray();

                    var ex = act.Should().Throw<CodeFirstModelGenerationException>().Which;

                    ex.Message.Should().Be(string.Format(Resources.ErrorGeneratingCodeFirstModel, "MyContext.cs"));
                    ex.InnerException.Message.Should().Contain(token);
                }
                finally
                {
                    File.Delete(templatePath);
                }
            }
        }

        // Test stopped working with 15.6 Preview 7 - plan to re-enable with https://github.com/aspnet/EntityFramework6/issues/541
        // [TestMethod]
        public void Generate_throws_when_error_in_entity_type_template()
        {
            var project = MockDTE.CreateProject();
            var token = Guid.NewGuid().ToString();

            using (AddCustomizedTemplates(project))
            {
                var templatePath = Path.GetTempFileName();
                try
                {
                    File.WriteAllText(templatePath, "<# throw new Exception(\"" + token + "\"); #>");

                    var fullPathProperty = project.ProjectItems.Item("CodeTemplates").ProjectItems
                        .Item("EFModelFromDatabase").ProjectItems.Item("EntityType.cs.t4").Properties.Cast<Property>()
                        .First(i => i.Name == "FullPath");
                    Mock.Get(fullPathProperty).SetupGet(p => p.Value).Returns(templatePath);

                    CodeFirstModelGenerator generator = new CodeFirstModelGenerator(project);

                    Action act = () => generator.Generate(Model, "WebApplication1.Models", "MyContext", "MyContextConnString")
                        .ToArray();

                    var ex = act.Should().Throw<CodeFirstModelGenerationException>().Which;

                    ex.Message.Should().Be(string.Format(Resources.ErrorGeneratingCodeFirstModel, "Entity.cs"));
                    ex.InnerException.Message.Should().Contain(token);
                }
                finally
                {
                    File.Delete(templatePath);
                }
            }
        }

        private static IDisposable AddCustomizedTemplates(Project project)
        {
            var templatePath = Path.GetTempFileName();
            File.WriteAllText(templatePath, "Customized");

            Mock<ProjectItem> contextCSharpItem = new Mock<ProjectItem>();
            Mock<ProjectItem> contextVBItem = new Mock<ProjectItem>();
            Mock<ProjectItem> entityCSharpItem = new Mock<ProjectItem>();
            Mock<ProjectItem> entityVBItem = new Mock<ProjectItem>();

            foreach (var projectItem in new[] { contextCSharpItem, contextVBItem, entityCSharpItem, entityVBItem })
            {
                Mock<Property> property = new Mock<Property>();
                property.SetupGet(p => p.Name).Returns("FullPath");
                property.SetupGet(p => p.Value).Returns(templatePath);

                var propertyArray = new[] { property.Object };

                Mock<Properties> properties = new Mock<Properties>();
                properties.As<IEnumerable>().Setup(p => p.GetEnumerator()).Returns(() => propertyArray.GetEnumerator());

                projectItem.SetupGet(i => i.Properties).Returns(properties.Object);
            }

            Mock<ProjectItems> actionProjectItems = new Mock<ProjectItems>();
            actionProjectItems.Setup(i => i.Item("Context.cs.t4")).Returns(contextCSharpItem.Object);
            actionProjectItems.Setup(i => i.Item("Context.vb.t4")).Returns(contextVBItem.Object);
            actionProjectItems.Setup(i => i.Item("EntityType.cs.t4")).Returns(entityCSharpItem.Object);
            actionProjectItems.Setup(i => i.Item("EntityType.vb.t4")).Returns(entityVBItem.Object);

            Mock<ProjectItem> actionItem = new Mock<ProjectItem>();
            actionItem.SetupGet(i => i.ProjectItems).Returns(actionProjectItems.Object);

            Mock<ProjectItems> templatesProjectItems = new Mock<ProjectItems>();
            templatesProjectItems.Setup(i => i.Item("EFModelFromDatabase")).Returns(actionItem.Object);

            Mock<ProjectItem> templatesItem = new Mock<ProjectItem>();
            templatesItem.SetupGet(i => i.ProjectItems).Returns(templatesProjectItems.Object);

            Mock<ProjectItems> projectItems = new Mock<ProjectItems>();
            projectItems.Setup(i => i.Item("CodeTemplates")).Returns(templatesItem.Object);

            Mock.Get(project).SetupGet(p => p.ProjectItems).Returns(projectItems.Object);

            return new CleanupAction(() => File.Delete(templatePath));
        }

        private class Entity
        {
            public int Id { get; set; }
        }
    }
}
