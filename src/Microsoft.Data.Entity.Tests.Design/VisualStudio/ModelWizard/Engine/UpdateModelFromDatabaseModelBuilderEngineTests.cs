// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Engine
{
    using System;
    using System.Xml.Linq;
    using Moq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

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
                var mockEdmxHelper = new Mock<EdmxHelper>(new XDocument());
                new UpdateModelFromDatabaseModelBuilderEngineFake()
                    .UpdateDesignerInfoInvoker(mockEdmxHelper.Object, new ModelBuilderSettings());

                mockEdmxHelper
                    .Verify(h => h.UpdateDesignerOptionProperty(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
            }
        }
    }
}
