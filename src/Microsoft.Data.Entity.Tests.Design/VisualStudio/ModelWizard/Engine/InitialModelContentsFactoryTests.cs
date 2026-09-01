// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Engine
{
    [TestClass]
    public class InitialModelContentsFactoryTests
    {
        [TestMethod]
        public void GetInitialModelContents_returns_contents()
        {
            foreach (var targetSchemaVersion in EntityFrameworkVersion.GetAllVersions())
            {
                new InitialModelContentsFactory().GetInitialModelContents(targetSchemaVersion)
                    .Should().Be(EdmUtils.CreateEdmxString(targetSchemaVersion, string.Empty, string.Empty, string.Empty));
            }
        }
    }
}
