// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Microsoft.Data.Tools.XmlDesignerBase.Model.StandAlone;

namespace Microsoft.Data.Entity.Tests.Shared.EFDesigner
{
    /// <summary>
    /// A mock EFArtifactHelper for use in unit tests.
    /// Creates and manages EFArtifacts without requiring the VS Package.
    /// </summary>
    internal class MockEFArtifactHelper : EFArtifactHelper, IDisposable
    {
        private readonly XmlModelProvider _modelProvider;
        private readonly MockPackage _package;

        internal MockEFArtifactHelper()
            : base(new EntityDesignModelManager(new EFArtifactFactory(), new EFArtifactSetFactory()))
        {
            _modelProvider = new VanillaXmlModelProvider();
            _package = new MockPackage((EntityDesignModelManager)_modelManager);
        }

        internal override EFArtifact GetNewOrExistingArtifact(Uri uri)
        {
            Debug.Assert(uri != null, "uri must not be null.");

            return GetNewOrExistingArtifact(uri, _modelProvider);
        }

        public void Dispose()
        {
            foreach (var uri in _modelManager.Artifacts.Select(a => a.Uri).ToArray())
            {
                ClearArtifact(uri);
            }

            _package?.Dispose();
        }
    }
}
