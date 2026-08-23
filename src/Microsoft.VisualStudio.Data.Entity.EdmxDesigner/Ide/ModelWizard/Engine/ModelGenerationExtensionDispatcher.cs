// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.Extensibility;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Engine
{
    internal class ModelGenerationExtensionDispatcher
    {
        private readonly WizardKind _wizardKind;
        private readonly XDocument _fromDatabaseDocument;
        private readonly XDocument _currentXDocument;
        private readonly Project _project;
        private bool _hasCurrentChanged;

        internal ModelGenerationExtensionDispatcher(WizardKind wizardKind, XDocument dbDocument, XDocument currentDocument, Project project)
        {
            _wizardKind = wizardKind;
            _fromDatabaseDocument = dbDocument;
            _currentXDocument = currentDocument;
            _project = project;
        }

        protected XDocument CurrentDocument
        {
            get { return _currentXDocument; }
        }

        protected XDocument FromDatabaseDocument
        {
            get { return _fromDatabaseDocument; }
        }

        protected Project Project
        {
            get { return _project; }
        }

        protected WizardKind WizardKind
        {
            get { return _wizardKind; }
        }

        internal bool HasCurrentChanged
        {
            get { return _hasCurrentChanged; }
        }

        protected virtual ModelGenerationExtensionContext CreateContext()
        {
            Debug.Assert(VsUtils.EntityFrameworkSupportedInProject(_project, PackageManager.Package, allowMiscProject: false), "VsUtils.EntityFrameworkSupportedInProject(_project, PackageManager.Package, allowMiscProject: false)");

            var targetSchemaVersion = EdmUtils.GetEntityFrameworkVersion(_project, PackageManager.Package);
            return new ModelGenerationExtensionContextImpl(
                _project, targetSchemaVersion, _currentXDocument, _fromDatabaseDocument, WizardKind);
        }

        protected virtual void DispatchToSingleExtension(IModelGenerationExtension extension, ModelGenerationExtensionContext context)
        {
            extension.OnAfterModelGenerated(context);
        }

        protected virtual void PreDispatch()
        {
            _fromDatabaseDocument?.Changing += BeforeEventHandler;

            CurrentDocument?.Changing += BeforeChangingCurrentEventHandler;
        }

        protected virtual void PostDispatch()
        {
            try
            {
                _fromDatabaseDocument?.Changing -= BeforeEventHandler;
            }
            finally
            {
                // be sure to unhook from the current document
                CurrentDocument?.Changing -= BeforeChangingCurrentEventHandler;
            }
        }

        internal void Dispatch()
        {
            PreDispatch();
            DispatchInternal();
            PostDispatch();
        }

        private void DispatchInternal()
        {
            Debug.Assert(
                WizardKind == WizardKind.UpdateModel || WizardKind == WizardKind.Generate, "Unexpected value for WizardKind = " + WizardKind);

            var modelGenerationExtensions = EdmxExtensionPointManager.LoadModelGenerationExtensions();
            if (modelGenerationExtensions.Length > 0) // don't create context if not needed
            {
                var modelGenerationExtensionContext = CreateContext();

                foreach (var exportInfo in modelGenerationExtensions)
                {
                    var extension = exportInfo.Value;
                    try
                    {
                        DispatchToSingleExtension(extension, modelGenerationExtensionContext);
                    }
                    catch (Exception e)
                    {
                        VsUtils.ShowErrorDialog(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                EdmxDesignerResources.Extensibility_ErrorOccurredDuringCallToExtension,
                                extension.GetType().FullName,
                                VsUtils.ConstructInnerExceptionErrorMessage(e)));
                    }
                }
            }
        }

        // event handler to ensure that the extension doesn't edit any 
        // document except the current one
        protected EventHandler<XObjectChangeEventArgs> _beforeEvent;

        protected EventHandler<XObjectChangeEventArgs> BeforeEventHandler
        {
            get
            {
                _beforeEvent ??= OnBeforeChange;
                return _beforeEvent;
            }
        }

        protected void OnBeforeChange(object sender, XObjectChangeEventArgs e)
        {
            throw new InvalidOperationException(EdmxDesignerResources.Extensibility_CantEditModel);
        }

        // event handler to record when an extension makes changes to the current document
        protected EventHandler<XObjectChangeEventArgs> _beforeChangingCurrentEvent;

        protected EventHandler<XObjectChangeEventArgs> BeforeChangingCurrentEventHandler
        {
            get
            {
                _beforeChangingCurrentEvent ??= OnBeforeChangingCurrent;
                return _beforeChangingCurrentEvent;
            }
        }

        protected void OnBeforeChangingCurrent(object sender, XObjectChangeEventArgs e)
        {
            _hasCurrentChanged = true;
        }
    }
}
