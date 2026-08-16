// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.Data.Entity.Design;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Common;
using Microsoft.Data.Entity.Design.Extensibility;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Eventing;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio;
using Microsoft.Data.Tools.XmlDesignerBase.Model;
using Microsoft.VisualStudio.Data.Entity.Design.Extensibility;
using Microsoft.VisualStudio.Data.Entity.Design.Ide.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Resources = Microsoft.VisualStudio.Data.Entity.Design.Resources;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide.Model
{
    internal class VSArtifact : EntityDesignArtifact
    {
        private HashSet<string> _namespaces;
        private LayerManager _layerManager;

        internal event EventHandler<EventArgs> AfterLoadedArtifact;

        internal override EditingContext EditingContext
        {
            get { return PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(Uri); }
        }

        internal virtual LayerManager LayerManager
        {
            get { return _layerManager; }
        }

        // <summary>
        //     Creates an instance of an EFArtifact for use inside Visual Studio
        // </summary>
        // <param name="modelManager">A reference of ModelManager</param>
        // <param name="uri">The URI to the EDMX file that this artifact will load</param>
        // <param name="xmlModelProvider">We are ignoring this parameter, sending null to base class so that it will call CreateModelProvider()</param>
        internal VSArtifact(ModelManager modelManager, Uri uri, XmlModelProvider xmlModelProvider)
            : base(modelManager, uri, xmlModelProvider)
        {
        }

        internal override void Init()
        {
            base.Init();
            _layerManager = new LayerManager(this);
            AddEventHandler();
        }

        internal override void FireArtifactReloadedEvent()
        {
            var context = PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(Uri);
            Debug.Assert(context != null, "context should not be null");
            context?.OnReloaded(EventArgs.Empty);
        }

        internal override void OnLoaded()
        {
            base.OnLoaded();

            // Register the artifact into the ModelBus if it is resolved.
            if (State == EFElementState.Resolved)
            {
                if (AfterLoadedArtifact != null)
                {
                    AfterLoadedArtifact(this, null);
                }
            }
        }

        protected override void OnBeforeHandleXmlModelTransactionCompleted(object sender, XmlTransactionEventArgs args)
        {
            base.OnBeforeHandleXmlModelTransactionCompleted(sender, args);

#if DEBUG
            var rDT = new RunningDocumentTable(PackageManager.Package);
            uint cookie = 0;
            rDT.FindDocument(Uri.LocalPath, out cookie);

            var info = rDT.GetDocumentInfo(cookie);
            Debug.Print(
                string.Format(
                    CultureInfo.CurrentCulture, "There are now {0} Edit Locks, and {1} Read Locks.", info.EditLocks, info.ReadLocks));
#endif
        }

        protected internal override HashSet<string> GetNamespaces()
        {
            _namespaces ??= [.. SchemaManager.GetAllNamespacesForVersion(SchemaVersion)];

            return _namespaces;
        }

        // <summary>
        //     Ensure we have the correct provider loaded before we reload
        // </summary>
        internal override void ReloadArtifact()
        {
            VsUtils.EnsureProvider(this);
            base.ReloadArtifact();
        }

        // <summary>
        //     This will do analysis to determine if a document should be opened
        //     only in the XmlEditor.
        // </summary>
        internal override void DetermineIfArtifactIsDesignerSafe()
        {
            VsUtils.EnsureProvider(this);

            base.DetermineIfArtifactIsDesignerSafe();
            //
            // TODO:  we need to figure out how to deal with errors from the wizard. 
            //        when we clear the error list below, we lose errors that we put into the error
            //        list when running the wizard.  
            // 

            //
            // Now update the VS error list with all of the errors we want to display, which are now in the EFArtifactSet.  
            //
            var errorInfos = ArtifactSet.GetAllErrors();
            if (errorInfos.Count == 0)
            {
                return;
            }

            var currentProject = VSHelpers.GetProjectForDocument(Uri.LocalPath, PackageManager.Package);
            var hierarchy = currentProject is null ? null : VsUtils.GetVsHierarchy(currentProject, PackageManager.Package);

            // The document may not belong to a loaded project, or the project's hierarchy may not be resolvable.
            // Fall back to the doc data's own hierarchy so that the errors are still reported: previously both of
            // these cases fell out of the method silently, leaving a designer-unsafe artifact with no explanation.
            if (hierarchy is null)
            {
                AddErrorInfosUsingDocData(errorInfos);
                return;
            }

            VSFileFinder fileFinder = new VSFileFinder(Uri.LocalPath);
            fileFinder.FindInProject(hierarchy);

            Debug.Assert(fileFinder.MatchingFiles.Count <= 1, "Unexpected count of matching files in project");

            // if the EDMX file is not part of the project.
            if (fileFinder.MatchingFiles.Count == 0)
            {
                AddErrorInfosUsingDocData(errorInfos);
                return;
            }

            foreach (var vsFileInfo in fileFinder.MatchingFiles)
            {
                if (vsFileInfo.Hierarchy != hierarchy)
                {
                    continue;
                }

                var errorList = ErrorListHelper.GetSingleDocErrorList(vsFileInfo.Hierarchy, vsFileInfo.ItemId);
                if (errorList is null)
                {
                    Debug.Fail("errorList is null!");
                    continue;
                }

                errorList.Clear();
                ErrorListHelper.AddErrorInfosToErrorList(errorInfos, vsFileInfo.Hierarchy, vsFileInfo.ItemId);
            }
        }

        // <summary>
        //     Reports validation errors against the doc data's own hierarchy. Used when the document's project or
        //     hierarchy cannot be resolved, so that a designer-unsafe artifact still surfaces a reason to the user
        //     instead of opening blank against an empty Error List.
        // </summary>
        private void AddErrorInfosUsingDocData(ICollection<ErrorInfo> errorInfos)
        {
            if (VSHelpers.GetDocData(PackageManager.Package, Uri.LocalPath) is not IEntityDesignDocData docData)
            {
                var message = "Could not resolve an IEntityDesignDocData for '" + Uri.LocalPath + "'; " + errorInfos.Count
                              + " validation error(s) will not be shown.";
                VsUtils.LogToActivityLog(message, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
                Debug.Fail(message);
                return;
            }

            if (docData.Hierarchy is null)
            {
                var message = "IEntityDesignDocData.Hierarchy is null for '" + Uri.LocalPath + "'; " + errorInfos.Count
                              + " validation error(s) will not be shown.";
                VsUtils.LogToActivityLog(message, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
                Debug.Fail(message);
                return;
            }

            VsUtils.LogToActivityLog(
                "Reporting " + errorInfos.Count + " validation error(s) for '" + Uri.LocalPath
                + "' against the document's own hierarchy; the document could not be matched to a project.",
                __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING);

            ErrorListHelper.AddErrorInfosToErrorList(errorInfos, docData.Hierarchy, docData.ItemId);
        }

        internal override bool IsXmlValid(out IList<string> validationErrors)
        {
            var errors = new List<string>();
            validationErrors = errors;

            // If there is a VSXmlModelProvider, we should be able to find a docdata for it.
            // In any other case, it doesn't matter whether there is document data or not.
            var docData = VSHelpers.GetDocData(PackageManager.Package, Uri.LocalPath);
            Debug.Assert(
                !(XmlModelProvider is VSXmlModelProvider) || docData != null, "Using a VSXmlModelProvider but docData is null for Artifact!");

            try
            {
                XmlDocument xmldoc;
                if (docData != null)
                {
                    var textLines = VSHelpers.GetVsTextLinesFromDocData(docData);
                    Debug.Assert(textLines != null, "Failed to get IVSTextLines from docdata");

                    xmldoc = EdmUtils.SafeLoadXmlFromString(VSHelpers.GetTextFromVsTextLines(textLines));
                }
                else
                {
                    // If there is no docdata then attempt to create the XmlDocument from the internal
                    // XLinq tree in the artifact
                    xmldoc = new XmlDocument();
                    xmldoc.Load(XDocument.CreateReader());
                }
                // For the most part, the Edmx schema version of an artifact should be in sync with the schema version 
                // that is compatible with the project's target framework; except when the user adds an existing edmx to a project (the version could be different).
                // For all cases, we always want to validate using the XSD's version that matches the artifact's version.
                var documentSchemaVersion = base.SchemaVersion;
                Debug.Assert(
                    EntityFrameworkVersion.IsValidVersion(documentSchemaVersion),
                    "The EF Schema Version is not valid. Value:"
                    + (documentSchemaVersion != null ? documentSchemaVersion.ToString() : "null"));

                // does the XML parse? If not, the load call below will throw
                if (EntityFrameworkVersion.IsValidVersion(documentSchemaVersion))
                {
                    var nsMgr = SchemaManager.GetEdmxNamespaceManager(xmldoc.NameTable, documentSchemaVersion);
                    // Do XSD validation on the document.
                    xmldoc.Schemas = EscherAttributeContentValidator.GetInstance(documentSchemaVersion).EdmxSchemaSet;
                    SchemaValidationErrorCollector svec = new SchemaValidationErrorCollector();

                    // remove runtime specific lines
                    // find the ConceptualModel Schema node
                    RemoveRunTimeNode(xmldoc, "/edmx:Edmx/edmx:Configurations", nsMgr);
                    RemoveRunTimeNode(xmldoc, "/edmx:Edmx/edmx:Runtime/edmx:ConceptualModels", nsMgr);
                    RemoveRunTimeNode(xmldoc, "/edmx:Edmx/edmx:Runtime/edmx:StorageModels", nsMgr);
                    RemoveRunTimeNode(xmldoc, "/edmx:Edmx/edmx:Runtime/edmx:Mappings", nsMgr);

                    xmldoc.Validate(svec.ValidationCallBack);

                    errors.AddRange(svec.Errors);
                    return svec.ErrorCount == 0;
                }

                // The schema version is not one we can validate against, so we cannot vouch for the document.
                errors.Add(
                    string.Format(
                        CultureInfo.CurrentCulture, Resources.XmlValidation_UnsupportedSchemaVersion,
                        documentSchemaVersion is null ? "(none)" : documentSchemaVersion.ToString()));
            }
            catch (Exception ex)
            {
                // Loading or validating threw. Report why rather than reporting a bare "the XML is not valid": the
                // exception message is the only description of the failure that exists.
                errors.Add(string.Format(CultureInfo.CurrentCulture, Resources.XmlValidation_ExceptionDuringValidation, ex.Message));
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    RemoveEventHandler();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        internal override LangEnum LanguageForCodeGeneration
        {
            get
            {
                var project = VSHelpers.GetProjectForDocument(Uri.LocalPath, PackageManager.Package);
                return VsUtils.GetLanguageForProject(project);
            }
        }

        internal override void DetermineIfArtifactIsVersionSafe()
        {
            // We want to move the user to the latest possible schemas - so if a user opens a v2 edmx
            // file in a project that has a reference to an EF assembly that can handle v3 schema we 
            // won't display the model but will show a watermark saying "please upgrade your schema"
            // There are two exceptions to this rule:
            // - a user opens an edmx without a project (a.k.a. Misc project) in that case we always show
            //   the model
            // - a user is targeting .NET Framework 4 and has references to both System.Data.Entity.dll 
            //   and EF6 EntityFramework.dll in which case we allow opening both v2 and v3 edmx files

            var project = GetProject();
            Debug.Assert(project != null);

            IsVersionSafe =
                VsUtils.EntityFrameworkSupportedInProject(project, ServiceProvider, allowMiscProject: true) &&
                VsUtils.SchemaVersionSupportedInProject(project, SchemaVersion, ServiceProvider);

            if (IsVersionSafe)
            {
                base.DetermineIfArtifactIsVersionSafe();
            }
        }

        // needed for mocking
        protected virtual IServiceProvider ServiceProvider
        {
            get { return PackageManager.Package; }
        }

        // needed for mocking
        protected virtual Project GetProject()
        {
            return VSHelpers.GetProjectForDocument(Uri.LocalPath, ServiceProvider);
        }

        private static void RemoveRunTimeNode(XmlDocument xmlDoc, string xpath, XmlNamespaceManager xmlNsm)
        {
            try
            {
                XmlElement runtimeNode = (XmlElement)xmlDoc.SelectSingleNode(xpath, xmlNsm);
                if (runtimeNode != null
                    && runtimeNode.ParentNode != null)
                {
                    runtimeNode.ParentNode.RemoveChild(runtimeNode);
                }
            }
            catch (XPathException)
            {
                Debug.Fail("The XPath expression contains a prefix which is not defined in the XmlNamespaceManager.");
            }
            catch (ArgumentException)
            {
                Debug.Fail("The oldChild is not a child of this node. Or this node is read-only. ");
            }
        }

        private EventHandler<XObjectChangeEventArgs> _beforeEvent;

        private EventHandler<XObjectChangeEventArgs> BeforeEventHandler
        {
            get
            {
                _beforeEvent ??= OnBeforeChange;
                return _beforeEvent;
            }
        }

        private void OnBeforeChange(object sender, XObjectChangeEventArgs e)
        {
            if (XmlModelProvider.CurrentTransaction == null)
            {
                //throw new InvalidOperationException(Resources.ChangingModelOutsideTransaction);
            }
        }

        protected override void OnAfterHandleXmlModelTransactionCompleted(
            object sender, XmlTransactionEventArgs xmlTransactionEventArgs, EfiChangeGroup changeGroup)
        {
            base.OnAfterHandleXmlModelTransactionCompleted(sender, xmlTransactionEventArgs, changeGroup);

            Debug.Assert(_layerManager != null, "LayerManager must not be null");
            if (_layerManager != null)
            {
                var changes = from ixc in xmlTransactionEventArgs.Transaction.Changes()
                              select new Tuple<XObject, XObjectChange>(ixc.Node, ixc.Action);
                _layerManager.OnAfterTransactionCommitted(changes);
            }
        }

        private void AddEventHandler()
        {
            XDocument?.Changing += BeforeEventHandler;
        }

        private void RemoveEventHandler()
        {
            XDocument?.Changing -= BeforeEventHandler;
        }

        internal override HashSet<string> GetFileExtensions()
        {
            return GetVSArtifactFileExtensions();
        }

        private static HashSet<string> _artifactFileExtensions;

        internal static HashSet<string> GetVSArtifactFileExtensions()
        {
            if (_artifactFileExtensions == null)
            {
                _artifactFileExtensions =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { ExtensionEdmx, ExtensionDiagram };

                // add any other extensions registered by converters
                foreach (var exportInfo in EscherExtensionPointManager.LoadModelConversionExtensions())
                {
                    var fileExtension = exportInfo.Metadata.FileExtension;

                    // ensure that the extension starts with a '.'
                    if (fileExtension.StartsWith(".", StringComparison.Ordinal) == false)
                    {
                        fileExtension = "." + fileExtension;
                    }

                    // add it if it isn't already in the list (duplicates will be checked for during load/save)
                    if (_artifactFileExtensions.Contains(fileExtension) == false)
                    {
                        _artifactFileExtensions.Add(fileExtension);
                    }
                }
            }
            return _artifactFileExtensions;
        }

        internal static void DispatchToSerializationExtensions(
            ICollection<Lazy<IModelTransformExtension>> exports, ModelTransformExtensionContext context, bool loading)
        {
            if (exports != null)
            {
                foreach (var exportInfo in exports)
                {
                    var extension = exportInfo.Value;
                    if (loading)
                    {
                        extension.OnAfterModelLoaded(context);
                    }
                    else
                    {
                        extension.OnBeforeModelSaved(context);
                    }
                }
            }
        }

        internal static void DispatchToConversionExtensions(
            ICollection<Lazy<IModelConversionExtension, IEntityDesignerConversionData>> exports, string fileExtension,
            ModelConversionExtensionContext context, bool loading)
        {
            // Every matching converter is collected before any of them runs. A converter *is* the interpretation of
            // its file format, so two registered for the same extension are competing alternatives rather than
            // pipeline stages - there is no correct order for them and the second would be handed EDMX when its
            // contract says it receives the custom format. Discovering that up front also means the failure happens
            // before any extension has mutated the document, instead of half way through.
            var matches = exports?
                .Where(export => IsConverterForExtension(export.Metadata.FileExtension, fileExtension))
                .ToList() ?? [];

            if (matches.Count == 0)
            {
                throw new InvalidOperationException(Resources.Extensibility_NoConverterForExtension);
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resources.Extensibility_TooManyConverters, DescribeConverters(matches)));
            }

            // Only now is the single winner instantiated, so a rejected converter is never constructed.
            var extension = matches[0].Value;

            if (loading)
            {
                extension.OnAfterFileLoaded(context);
            }
            else
            {
                extension.OnBeforeFileSaved(context);
            }
        }

        // <summary>
        //     Builds the list of conflicting converters for the "too many converters" message.
        // </summary>
        // <remarks>
        //     Named from MEF metadata rather than the exported type, because reading the type would mean
        //     instantiating converters that are about to be rejected. LayerName is optional, so converters that did
        //     not declare one are identified by position.
        // </remarks>
        private static string DescribeConverters(
            IList<Lazy<IModelConversionExtension, IEntityDesignerConversionData>> converters)
        {
            return string.Join(
                ", ",
                converters.Select(
                    (converter, index) => string.IsNullOrWhiteSpace(converter.Metadata.LayerName)
                        ? string.Format(CultureInfo.CurrentCulture, "#{0}", index + 1)
                        : converter.Metadata.LayerName));
        }

        // <summary>
        //     Determines whether a converter's declared file extension matches the file being loaded.
        // </summary>
        // <remarks>
        //     Extensions may be declared with or without the leading dot, so the converter's value is normalized
        //     before comparison. The file extension supplied by the caller always includes it.
        // </remarks>
        private static bool IsConverterForExtension(string converterFileExtension, string fileExtension)
        {
            if (converterFileExtension is null)
            {
                return false;
            }

            if (!converterFileExtension.StartsWith(".", StringComparison.Ordinal))
            {
                converterFileExtension = "." + converterFileExtension;
            }

            return string.Equals(fileExtension, converterFileExtension, StringComparison.OrdinalIgnoreCase);
        }

        internal override List<EdmSchemaError> GetModelGenErrors()
        {
            return PackageManager.Package.ModelGenErrorCache.GetErrors(Uri.LocalPath);
        }
    }
}