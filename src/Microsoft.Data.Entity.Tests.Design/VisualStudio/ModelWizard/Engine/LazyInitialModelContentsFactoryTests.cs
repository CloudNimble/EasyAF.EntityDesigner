// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Engine
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using Microsoft.Data.Entity.Design.VersioningFacade;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class LazyInitialModelContentsFactoryTests
    {
        [TestMethod]
        public void GetInitialModelContents_returns_contents()
        {
            const string fileContentsTemplate = "Contents";
            var replacementsDictionary = new Dictionary<string, string>();
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            Assert.Equal(
                fileContentsTemplate,
                factory.GetInitialModelContents(EntityFrameworkVersion.Version3));
        }

        [TestMethod]
        public void GetInitialModelContents_replaces_tokens()
        {
            var fileContentsTemplate = "$test$";
            var replacementsDictionary = new Dictionary<string, string> { { "$test$", "Passed" } };
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            Assert.Equal("Passed", factory.GetInitialModelContents(EntityFrameworkVersion.Version3));
        }

        [TestMethod]
        public void GetInitialModelContents_replaces_version_specific_tokens()
        {
            var fileContentsTemplate = "$edmxversion$";
            var replacementsDictionary = new Dictionary<string, string>();
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            Assert.Equal("3.0", factory.GetInitialModelContents(EntityFrameworkVersion.Version3));
        }

        [TestMethod]
        public void GetInitialModelContents_appends_version_specific_tokens_to_replacements()
        {
            var fileContentsTemplate = "Contents";
            var replacementsDictionary = new Dictionary<string, string> { { "$first$", "First" } };
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            factory.GetInitialModelContents(EntityFrameworkVersion.Version3);

            replacementsDictionary.Count.Should().Be(11);
            Assert.Equal("$first$", replacementsDictionary.First().Key);
        }

        [TestMethod]
        public void GetInitialModelContents_is_idempotent()
        {
            const string fileContentsTemplate = "Contents";
            var replacementsDictionary = new Dictionary<string, string>();
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            factory.GetInitialModelContents(EntityFrameworkVersion.Version3);

            replacementsDictionary.Count.Should().Be(10);

            factory.GetInitialModelContents(EntityFrameworkVersion.Version3);

            replacementsDictionary.Count.Should().Be(10);
        }

        private IInitialModelContentsFactory CreateFactory(
            string fileContentsTemplate,
            IDictionary<string, string> replacementsDictionary)
        {
            var factory = new LazyInitialModelContentsFactory(
                fileContentsTemplate,
                replacementsDictionary);

            return factory;
        }
    }
}
