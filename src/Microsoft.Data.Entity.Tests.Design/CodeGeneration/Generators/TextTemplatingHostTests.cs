using System;
using System.CodeDom.Compiler;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Generators;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class TextTemplatingHostTests
    {
        [TestMethod]
        public void StandardAssemblyReferences_returns_references()
        {
            new TextTemplatingHost().StandardAssemblyReferences.Should().BeEquivalentTo(new[] { "System" });
        }

        [TestMethod]
        public void StandardImports_returns_imports()
        {
            new TextTemplatingHost().StandardImports.Should().BeEquivalentTo(new[] { "System" });
        }

        [TestMethod]
        public void GetHostOption_returns_null()
        {
            new TextTemplatingHost().GetHostOption("CacheAssemblies").Should().BeNull();
        }

        [TestMethod]
        public void LogErrors_is_noop_when_no_callback()
        {
            TextTemplatingHost host = new TextTemplatingHost();
            host.LogErrors(Mock.Of<CompilerErrorCollection>());
        }

        [TestMethod]
        public void LogErrors_delegates_when_callback()
        {
            Mock<ITextTemplatingCallback> callback = new Mock<ITextTemplatingCallback>();
            Mock<TextTemplatingHost> host = new Mock<TextTemplatingHost>() { CallBase = true };
            host.SetupGet(h => h.Callback).Returns(callback.Object);
            CompilerErrorCollection errors = new CompilerErrorCollection
                {
                    new CompilerError { IsWarning = true, ErrorText = "Error1", Line = 1, Column = 42 }
                };

            host.Object.LogErrors(errors);

            callback.Verify(c => c.ErrorCallback(true, "Error1", 1, 42));
        }

        [TestMethod]
        public void ProvideTemplatingAppDomain_returns_current_domain()
        {
            new TextTemplatingHost().ProvideTemplatingAppDomain("Content1").Should().BeSameAs(AppDomain.CurrentDomain);
        }

        [TestMethod]
        public void ResolveAssemblyReference_resolves_simple_reference()
        {
            new TextTemplatingHost().ResolveAssemblyReference(GetType().Assembly.GetName().Name)
                .Should().Be(GetType().Assembly.Location);
        }

        [TestMethod]
        public void ResolveAssemblyReference_resolves_qualified_reference()
        {
            new TextTemplatingHost().ResolveAssemblyReference(GetType().Assembly.FullName)
                .Should().Be(GetType().Assembly.Location);
        }

        [TestMethod]
        public void SetFileExtension_is_noop_when_no_callback()
        {
            TextTemplatingHost host = new TextTemplatingHost();
            host.SetFileExtension(".out");
        }

        [TestMethod]
        public void SetFileExtension_delegates_when_callback()
        {
            Mock<ITextTemplatingCallback> callback = new Mock<ITextTemplatingCallback>();
            Mock<TextTemplatingHost> host = new Mock<TextTemplatingHost>() { CallBase = true };
            host.SetupGet(h => h.Callback).Returns(callback.Object);

            host.Object.SetFileExtension(".out");

            callback.Verify(c => c.SetFileExtension(".out"));
        }

        [TestMethod]
        public void SetOutputEncoding_is_noop_when_no_callback()
        {
            TextTemplatingHost host = new TextTemplatingHost();
            host.SetOutputEncoding(Encoding.ASCII, true);
        }

        [TestMethod]
        public void SetOutputEncoding_delegates_when_callback()
        {
            Mock<ITextTemplatingCallback> callback = new Mock<ITextTemplatingCallback>();
            Mock<TextTemplatingHost> host = new Mock<TextTemplatingHost>() { CallBase = true };
            host.SetupGet(h => h.Callback).Returns(callback.Object);

            host.Object.SetOutputEncoding(Encoding.ASCII, true);

            callback.Verify(c => c.SetOutputEncoding(Encoding.ASCII, true));
        }

        [TestMethod]
        public void CreateSession_returns_session()
        {
            new TextTemplatingHost().CreateSession().Should().BeOfType<TextTemplatingSession>();
        }

        // Test stopped working with 15.6 Preview 7 - plan to re-enable with https://github.com/aspnet/EntityFramework6/issues/541
        // [TestMethod]
        public void ProcessTemplate_returns_result()
        {
            new TextTemplatingHost().ProcessTemplate("Dummy.tt", "<#= \"Result\" #>").Should().Be("Result");
        }
    }
}
