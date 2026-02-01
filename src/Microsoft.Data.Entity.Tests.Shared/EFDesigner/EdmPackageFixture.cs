// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VisualStudio.Package;

namespace Microsoft.Data.Entity.Tests.Shared.EFDesigner
{
    /// <summary>
    /// Test fixture that creates a mock package for tests that need PackageManager.Package set.
    /// </summary>
    public class EdmPackageFixture : IDisposable
    {
        private readonly EntityDesignModelManager _modelManager;
        private readonly MockPackage _package;

        public EdmPackageFixture()
        {
            _modelManager = new EntityDesignModelManager(
                new EFArtifactFactory(),
                new EFArtifactSetFactory());

            _package = new MockPackage(_modelManager);
        }

        public void Dispose()
        {
            _package?.Dispose();
        }
    }
}
