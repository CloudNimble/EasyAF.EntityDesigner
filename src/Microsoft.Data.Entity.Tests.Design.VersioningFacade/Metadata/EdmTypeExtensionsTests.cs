// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.Metadata
{
    using System.Data.Entity.Core.Metadata.Edm;
    using Microsoft.Data.Entity.Design.VersioningFacade.Metadata;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using FluentAssertions;

    [TestClass]
    public class EdmTypeExtensionsTests
    {
        [TestMethod]
        public void EdmType_GetDataSpace_returns_correct_space_for_EdmType()
        {
            PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32).GetDataSpace().Should().Be(DataSpace.CSpace);
        }
    }
}
