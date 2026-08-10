// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using EnvDTE;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model.Eventing;

namespace Microsoft.Data.Entity.Design.Extensibility
{
    // <summary>
    //     This class is used by Escher extensions to update annotation values in the EDMX.
    // </summary>
    internal class PropertyExtensionContextImpl : PropertyExtensionContext, IChangeScopeContainer
    {
        internal const string PROPERTY_EXTENSION_ORIGINATOR_ID = "__property_extension";

        private readonly EditingContext _editingContext;
        private readonly ProjectItem _projectItem;
        private readonly Version _targetSchemaVersion;
        private readonly byte[] _extensionToken;
        private ChangeScopeImpl _scope;

        internal PropertyExtensionContextImpl(
            EditingContext editingContext, ProjectItem projectItem, Version targetSchemaVersion, byte[] extensionToken)
        {
            Debug.Assert(editingContext != null, "editingContext should not be null");
            Debug.Assert(editingContext.GetEFArtifactService().Artifact != null, "editingContext should not have null artifact");
            Debug.Assert(projectItem != null, "projectItem should not be null");
            Debug.Assert(extensionToken != null, "extensionToken should not be null");

            _editingContext = editingContext;
            _projectItem = projectItem;
            _targetSchemaVersion = targetSchemaVersion;
            _extensionToken = extensionToken;
        }

        public override ProjectItem ProjectItem
        {
            get { return _projectItem; }
        }

        public override Version EntityFrameworkVersion
        {
            get { return _targetSchemaVersion; }
        }

        public override Project Project
        {
            get { return _projectItem.ContainingProject; }
        }

        public override EntityDesignerChangeScope CreateChangeScope(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name should not be null.");
            }

            if (_scope != null)
            {
                throw new InvalidOperationException(Resources.Extensibility_StartChangeScopeFailed);
            }

            EfiTransaction txn = new EfiTransaction(_editingContext.GetEFArtifactService().Artifact, PROPERTY_EXTENSION_ORIGINATOR_ID, name);
            _scope = new ChangeScopeImpl(txn, _editingContext, _extensionToken, this);
            return _scope;
        }

        void IChangeScopeContainer.OnScopeDisposed()
        {
            _scope = null;
        }
    }
}
