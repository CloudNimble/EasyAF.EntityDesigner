// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine;

namespace Microsoft.VisualStudio.Data.Entity.Tests.Package.VisualStudio.ModelWizard.Engine
{
    [TestClass]
    public class LazyInitialModelContentsFactoryTests
    {
        [TestMethod]
        public void GetInitialModelContents_returns_contents()
        {
            const string fileContentsTemplate = "Contents";
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string>();
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            factory.GetInitialModelContents(EntityFrameworkVersion.Version3).Should().Be(fileContentsTemplate);
        }

        [TestMethod]
        public void GetInitialModelContents_replaces_tokens()
        {
            var fileContentsTemplate = "$test$";
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string> { { "$test$", "Passed" } };
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            factory.GetInitialModelContents(EntityFrameworkVersion.Version3).Should().Be("Passed");
        }

        [TestMethod]
        public void GetInitialModelContents_replaces_version_specific_tokens()
        {
            var fileContentsTemplate = "$edmxversion$";
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string>();
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            factory.GetInitialModelContents(EntityFrameworkVersion.Version3).Should().Be("3.0");
        }

        [TestMethod]
        public void GetInitialModelContents_appends_version_specific_tokens_to_replacements()
        {
            var fileContentsTemplate = "Contents";
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string> { { "$first$", "First" } };
            var factory = CreateFactory(fileContentsTemplate, replacementsDictionary);

            factory.GetInitialModelContents(EntityFrameworkVersion.Version3);

            replacementsDictionary.Count.Should().Be(11);
            replacementsDictionary.First().Key.Should().Be("$first$");
        }

        [TestMethod]
        public void GetInitialModelContents_is_idempotent()
        {
            const string fileContentsTemplate = "Contents";
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string>();
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
            LazyInitialModelContentsFactory factory = new LazyInitialModelContentsFactory(
                fileContentsTemplate,
                replacementsDictionary);

            return factory;
        }
    }
}
