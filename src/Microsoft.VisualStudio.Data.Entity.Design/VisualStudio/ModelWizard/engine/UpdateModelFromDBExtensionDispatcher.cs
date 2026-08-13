// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.Data.Entity.Design.Extensibility;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Package;
using Microsoft.VisualStudio.Data.Entity.Design.Extensibility;

namespace Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.ModelWizard.engine
{
    internal class UpdateModelFromDBExtensionDispatcher : ModelGenerationExtensionDispatcher
    {
        private readonly ProjectItem _projectItem;
        private readonly XDocument _updateModelDocument;
        private readonly XDocument _originalDocument;

        internal UpdateModelFromDBExtensionDispatcher(
            WizardKind wizardKind, XDocument dbDocument, XDocument currentDocument, ProjectItem projectItem, XDocument originalDocument,
            XDocument updateModelDocument)
            :
                base(wizardKind, dbDocument, currentDocument, projectItem.ContainingProject)
        {
            _projectItem = projectItem;
            _originalDocument = originalDocument;
            _updateModelDocument = updateModelDocument;
        }

        private ProjectItem ProjectItem
        {
            get { return _projectItem; }
        }

        private XDocument OriginalDocument
        {
            get { return _originalDocument; }
        }

        private XDocument UpdateModelDocument
        {
            get { return _updateModelDocument; }
        }

        protected override ModelGenerationExtensionContext CreateContext()
        {
            Debug.Assert(VsUtils.EntityFrameworkSupportedInProject(Project, PackageManager.Package, allowMiscProject: false));

            var targetSchemaVersion = EdmUtils.GetEntityFrameworkVersion(Project, PackageManager.Package);
            return new UpdateModelFromDatabaseExtensionContextImpl(
                Project, ProjectItem, targetSchemaVersion,
                CurrentDocument, FromDatabaseDocument, OriginalDocument, UpdateModelDocument);
        }

        protected override void DispatchToSingleExtension(IModelGenerationExtension extension, ModelGenerationExtensionContext context)
        {
            UpdateModelExtensionContext umfdbContext = context as UpdateModelExtensionContext;
            Debug.Assert(umfdbContext != null, "Unexpected type of context!");
            if (context != null)
            {
                extension.OnAfterModelUpdated(umfdbContext);
            }
        }

        protected override void PreDispatch()
        {
            base.PreDispatch();

            _originalDocument?.Changing += BeforeEventHandler;

            _updateModelDocument?.Changing += BeforeEventHandler;
        }

        protected override void PostDispatch()
        {
            _originalDocument?.Changing -= BeforeEventHandler;

            _updateModelDocument?.Changing -= BeforeEventHandler;

            base.PostDispatch();
        }
    }
}
