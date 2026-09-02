// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Tests.Package.VisualStudio.ModelWizard.Engine
{
    [TestClass]
    public class UpdateModelFromDatabaseModelBuilderEngineTests
    {
        [TestClass]
    public class UpdateDesignerInfoTests
        {
            private class UpdateModelFromDatabaseModelBuilderEngineFake
                : UpdateModelFromDatabaseModelBuilderEngine
            {
                internal void UpdateDesignerInfoInvoker(EdmxHelper edmxHelper, ModelBuilderSettings settings)
                {
                    UpdateDesignerInfo(edmxHelper, settings);
                }
            }

            [TestMethod]
            public void UpdateDesignerInfo_updates_no_properties_in_designer_section()
            {
                Mock<EdmxHelper> mockEdmxHelper = new Mock<EdmxHelper>(new XDocument());
                new UpdateModelFromDatabaseModelBuilderEngineFake()
                    .UpdateDesignerInfoInvoker(mockEdmxHelper.Object, new ModelBuilderSettings());

                mockEdmxHelper
                    .Verify(h => h.UpdateDesignerOptionProperty(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
            }
        }
    }
}
