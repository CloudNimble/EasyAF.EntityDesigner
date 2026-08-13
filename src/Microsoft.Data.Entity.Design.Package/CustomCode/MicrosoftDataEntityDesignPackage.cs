// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Data.Entity.Design.Dsl.View;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Model;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.DataDesign.Interfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using ModelChangeEventArgs = Microsoft.Data.Entity.Design.VisualStudio.Package.ModelChangeEventArgs;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Package;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.UI.Views.MappingDetails;

namespace Microsoft.Data.Entity.Design.Package
{
    // Transient because this package cannot service a shell-initiated restore of this window. The shell asks for a
    // persisted frame through IVsPackage.CreateTool, which reaches ModelingPackage.CreateToolWindow - a non-virtual
    // method that resolves persistence slots only against the registry AddToolWindow fills. That registry is typed to
    // the DSL SDK's ToolWindow, and EntityDesignExplorerWindow is a plain shell ToolWindowPane, so the slot cannot be
    // registered and the restore throws ArgumentNullException into the window's own frame. Marking the window
    // transient keeps it out of the persisted layout, so that path is never taken; it is still opened on demand
    // through FindToolWindow, which honours this attribute, and ProvideToolWindowVisibility below already ties it to
    // the EDMX editor context.
    [ProvideToolWindow(typeof(EntityDesignExplorerWindow),
        MultiInstances = false,
        Style = VsDockStyle.Tabbed,
        Orientation = ToolWindowOrientation.Right,
        Transient = true,
        Window = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}")]
    [ProvideToolWindowVisibility(typeof(EntityDesignExplorerWindow), Constants.MicrosoftDataEntityDesignEditorFactoryId)]
    [ProvideToolWindow(typeof(MappingDetailsWindow),
        MultiInstances = false,
        Style = VsDockStyle.Tabbed,
        Orientation = ToolWindowOrientation.Left,
        Window = "{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}")]
    [ProvideToolWindowVisibility(typeof(MappingDetailsWindow), Constants.MicrosoftDataEntityDesignEditorFactoryId)]
    [MyProvideMenuResource(CommonPackageConstants.ctmenuResourceId, CommonPackageConstants.ctmenuVersion)]
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [ProvideEditorLogicalView(typeof(MicrosoftDataEntityDesignEditorFactory), PackageConstants.guidLogicalViewString, IsTrusted = true)]
    // Auto-load when .edmx file is selected (for context menu support)
    [ProvideAutoLoad(PackageConstants.UICONTEXT_AddNewEntityDataModel, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(PackageConstants.UICONTEXT_AddNewEntityDataModel,
        name: "Auto load Entity Data Model Package",
        expression: "DotEdmx",
        termNames: new[] { "DotEdmx" },
        termValues: new[] { "HierSingleSelectionName:.edmx$" })]
    internal sealed partial class MicrosoftDataEntityDesignPackage : IEdmPackage, IVsTrackProjectRetargetingEvents
    {
        private OleMenuCommand _viewExplorerCmd;
        private OleMenuCommand _viewMappingCmd;
        private ExplorerWindow _explorerWindow;
        private MappingDetailsWindow _mappingDetailsWindow;
        private Dispatcher _dispatcher; // foreground thread
        private bool? _isBuildingFromCommandLine;
        private uint _trackProjectRetargetingEventsCookie;

        public IEntityDesignCommandSet CommandSet { get; set; }

        #region Initialize/Dispose

        protected override void Initialize()
        {
            base.Initialize();

            // for command line builds we only have a minimal set of functionality to only support building
            if (IsBuildingFromCommandLine)
            {
                InitializeForCommandLineBuilds();
            }
            else
            {
                // HACK HACK -- find a better place to do this.
                EFModelErrorTaskNavigator.DslDesignerOnNavigate = DSLDesignerNavigationHelper.NavigateTo;
                // --

                PackageManager.Package = this;
                _dispatcher = Dispatcher.CurrentDispatcher;

                // make sure that we can load the XML Editor package
                IVsShell vsShell = (IVsShell)GetService(typeof(SVsShell));
                if (vsShell != null)
                {
                    var editorPackageGuid = CommonPackageConstants.xmlEditorPackageGuid;
                    NativeMethods.ThrowOnFailure(vsShell.LoadPackage(ref editorPackageGuid, out IVsPackage editorPackage));
                }

                DocumentFrameMgr = new EntityDesignDocumentFrameMgr(PackageManager.Package);
                ModelChangeEventListener = new ModelChangeEventListener();
                AggregateProjectTypeGuidCache = new AggregateProjectTypeGuidCache();
                ModelGenErrorCache = new ModelGenErrorCache();
                ConnectionManager = new ConnectionManager();

                // Only MappingDetailsWindow is registered here: ModelingPackage's tool window registry, and the
                // GetToolWindow lookup that reads it, are typed to the DSL SDK's ToolWindow. EntityDesignExplorerWindow
                // is a plain shell ToolWindowPane, so it is resolved through FindToolWindow and its ProvideToolWindow
                // attribute instead.
                AddToolWindow(typeof(MappingDetailsWindow));

                // Register for VS Events
                ErrorListHelper.RegisterForNotifications();

                // Add the handler to show our Explorer. This is for the top-level 'Entity Data Model Browser' command that is added to the
                // 'View' main menu. This is different from the 'Model Browser' command on the designer context menu.
                _viewExplorerCmd = AddCommand(
                    ShowExplorerWindow, ShowExplorerWindow_BeforeQueryStatus, MicrosoftDataEntityDesignCommands.ViewExplorer);

                // Add the handler to show our MappingDesigner. This is for the top-level 'Entity Data Model Mapping Details' command that is added
                // to the 'View' main menu. This is different from the 'Mapping Details' command on the designer context menu.
                _viewMappingCmd = AddCommand(
                    ShowMappingDetailsWindow, ShowMappingDetailsWindow_BeforeQueryStatus, MicrosoftDataEntityDesignCommands.ViewMapping);

                // Subscribe to Project's target framework retargeting
                IVsTrackProjectRetargeting projectRetargetingService = GetService(typeof(SVsTrackProjectRetargeting)) as IVsTrackProjectRetargeting;
                Debug.Assert(null != projectRetargetingService, "TrackProjectRetargeting service is null");
                _trackProjectRetargetingEventsCookie = 0;
                projectRetargetingService?.AdviseTrackProjectRetargetingEvents(this, out _trackProjectRetargetingEventsCookie);

                // There is no SQL CE support dev12 onward, so removing the references

                IVsSqlDatabaseFileUpgradeService sqlDatabaseFileUpgradeService =
                    GetGlobalService(typeof(IVsSqlDatabaseFileUpgradeService)) as IVsSqlDatabaseFileUpgradeService;

                if (sqlDatabaseFileUpgradeService == null)
                {
                    // attempt to start IVsSqlDatabaseFileUpgradeService
                    BootstrapVSDesigner();

                    sqlDatabaseFileUpgradeService ??=
                            GetGlobalService(typeof(IVsSqlDatabaseFileUpgradeService)) as IVsSqlDatabaseFileUpgradeService;
                }

                Debug.Assert(null != sqlDatabaseFileUpgradeService, "sqlDatabaseFileUpgradeService service is null");
                sqlDatabaseFileUpgradeService?.OnUpgradeProject += EdmUtils.SqlDatabaseFileUpgradeService_OnUpgradeProject;
            }
        }

        private void BootstrapVSDesigner()
        {
            // Bootstrap VSDesigner services so that we can subscribe to an event service which is set up in VSDesigner initialization
            Guid iunknown = new Guid("00000000-0000-0000-C000-000000000046");
            Guid vsDesignerBootstrap = new Guid("AD028B85-FA21-41b1-AB4A-08672F633506");
            IOleServiceProvider site = (IOleServiceProvider)GetService(typeof(IOleServiceProvider));
            if (site != null)
            {
                var hr = site.QueryService(ref vsDesignerBootstrap, ref iunknown, out IntPtr ppvObject);
                if (NativeMethods.Succeeded(hr) && ppvObject != IntPtr.Zero)
                {
                    Marshal.Release(ppvObject);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // HACK HACK -- change when the hack is removed above
                EFModelErrorTaskNavigator.DslDesignerOnNavigate = null;
                // --

                // always dispose and null out items that use VS resources
                _viewExplorerCmd = null;
                _viewMappingCmd = null;
                _explorerWindow?.Dispose();
                _explorerWindow = null;

                if (_mappingDetailsWindow != null)
                {
                    // don't need to call this, the MDF takes care of this one
                    //_mappingDetailsWindow.Dispose();
                    _mappingDetailsWindow = null;
                }

                // remove all errors
                ErrorListHelper.RemoveAll();

                // Unregister for VS Events
                ErrorListHelper.UnregisterForNotifications();

                // dispose of our classes in reverse order than we created them
                ConnectionManager?.Dispose();
                ConnectionManager = null;
                ModelChangeEventListener?.Dispose();
                ModelChangeEventListener = null;
                DocumentFrameMgr?.Dispose();
                DocumentFrameMgr = null;

                ModelManager.Dispose();

                // UnSubscribe from the SqlDatabaseFile upgrade service
                IVsSqlDatabaseFileUpgradeService sqlDatabaseFileUpgradeService =
                    GetGlobalService(typeof(IVsSqlDatabaseFileUpgradeService)) as IVsSqlDatabaseFileUpgradeService;
                sqlDatabaseFileUpgradeService?.OnUpgradeProject -= EdmUtils.SqlDatabaseFileUpgradeService_OnUpgradeProject;

                // clear out any static references
                PackageManager.Package = null;
                Services.ServiceProvider = null;
                _dispatcher = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Command Handling

        /// <summary>
        ///     Define a command handler.
        ///     When the user press the button corresponding to the CommandID
        ///     the EventHandler will be called.
        /// </summary>
        /// <param name="id">The CommandID (Guid/ID pair) as defined in the .ctc file</param>
        /// <param name="invocationEventHandler">Method that should be called to implement the command</param>
        /// <param name="queryStatusEventHandler">Method that should be called to ensure the status (visible/enabled) of the menu command</param>
        /// <returns>The menu command. This can be used to set parameter such as the default visibility once the package is loaded</returns>
        internal OleMenuCommand AddCommand(EventHandler invocationEventHandler, EventHandler queryStatusEventHandler, CommandID id)
        {
            // if the package is zombied, we don't want to add commands
            if (Zombied)
            {
                return null;
            }

            var menuService = Services.OleMenuCommandService;
            OleMenuCommand command = null;
            if (null != menuService)
            {
                // Add the command handler
                command = new OleMenuCommand(invocationEventHandler, null, queryStatusEventHandler, id);
                menuService.AddCommand(command);
            }

            return command;
        }

        #endregion

        #region Command-line builds

        /// <summary>
        ///     When the user builds through the command line, we only load the necessary, non-UI functionality:
        ///     1. Xml Editor package for XLinq tree
        ///     2. Model Manager
        ///     3. Connection Manager and listener
        ///     4. Error List, and notifications that perform our specialized validation on build
        /// </summary>
        private void InitializeForCommandLineBuilds()
        {
            PackageManager.Package = this;
            _dispatcher = Dispatcher.CurrentDispatcher;

            // make sure that we can load the XML Editor package
            IVsShell vsShell = (IVsShell)GetService(typeof(SVsShell));
            if (vsShell != null)
            {
                var editorPackageGuid = CommonPackageConstants.xmlEditorPackageGuid;
                NativeMethods.ThrowOnFailure(vsShell.LoadPackage(ref editorPackageGuid, out IVsPackage editorPackage));
            }

            ErrorListHelper.RegisterForNotifications();
        }

        #endregion

        #region Explorer Window / Mapping Details Showing

        /// <summary>
        ///     Event handler to decide whether the ShowExplorerWindow menu item is visible and enabled.
        /// </summary>
        private void ShowExplorerWindow_BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuCommand)
            {
                // if an EDMX file is currently open then this menu item should be visible and enabled
                if (PackageManager.Package.ModelManager.Artifacts != null
                    && PackageManager.Package.ModelManager.Artifacts.Count > 0)
                {
                    menuCommand.Enabled = true;
                    menuCommand.Visible = true;
                }
                else
                {
                    menuCommand.Enabled = false;
                    menuCommand.Visible = false;
                }
            }
        }

        /// <summary>
        ///     Event handler for our invocation of the ShowExplorerWindow menu item.
        ///     This results in the tool window being shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void ShowExplorerWindow(object sender, EventArgs arguments)
        {
            ExplorerWindow.Show();
        }

        /// <summary>
        ///     Event handler to decide whether the ShowMappingDetailsWindow menu item is visible and enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowMappingDetailsWindow_BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuCommand)
            {
                // if an EDMX file is currently open then this menu item should be visible and enabled
                if (PackageManager.Package.ModelManager.Artifacts != null
                    && PackageManager.Package.ModelManager.Artifacts.Count > 0)
                {
                    menuCommand.Enabled = true;
                    menuCommand.Visible = true;
                }
                else
                {
                    menuCommand.Enabled = false;
                    menuCommand.Visible = false;
                }
            }
        }

        /// <summary>
        ///     Event handler for our invocation of the ShowMappingDetailsWindow menu item.
        ///     This results in the tool window being shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void ShowMappingDetailsWindow(object sender, EventArgs arguments)
        {
            MappingDetailsWindow.Show();
        }

        #endregion

        #region IXmlDesignerPackage members

        public void InvokeOnForeground(SimpleDelegateClass.SimpleDelegate operation)
        {
            if (IsForegroundThread)
            {
                operation.Invoke();
            }
            else
            {
                _ = _dispatcher.BeginInvoke(DispatcherPriority.Normal, operation);
            }
        }

        public bool IsForegroundThread
        {
            get { return _dispatcher == Dispatcher.CurrentDispatcher; }
        }

        public void SynchronousInvokeOnForeground(SimpleDelegateClass.SimpleDelegate operation)
        {
            if (IsForegroundThread)
            {
                operation.Invoke();
            }
            else
            {
                _dispatcher.Invoke(DispatcherPriority.Normal, operation);
            }
        }

        ModelManager IXmlDesignerPackage.ModelManager
        {
            get { return ModelManager; }
        }

        public event ModelChangeEventHandler FileNameChanged;

        /// <summary>
        ///     This method loads a localized string based on the specified resource.
        /// </summary>
        /// <param name="resourceName">Resource to load</param>
        /// <returns>String loaded for the specified resource</returns>
        public string GetResourceString(string resourceName)
        {
            IVsResourceManager resourceManager = (IVsResourceManager)GetService(typeof(SVsResourceManager));
            if (resourceManager == null)
            {
                throw new InvalidOperationException(
                    "Could not get SVsResourceManager service. Make sure the package is Sited before calling this method");
            }
            var packageGuid = GetType().GUID;
            var hr = resourceManager.LoadResourceString(ref packageGuid, -1, resourceName, out string resourceValue);
            ErrorHandler.ThrowOnFailure(hr);
            return resourceValue;
        }

        #endregion

        #region IEdmPackage members

        public ExplorerWindow ExplorerWindow
        {
            get
            {
                if (_explorerWindow is null)
                {
                    using (DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware))
                    {
                        _explorerWindow = FindToolWindow(typeof(EntityDesignExplorerWindow), 0, true) as ExplorerWindow;
                    }
                }
                return _explorerWindow;
            }
        }

        public MappingDetailsWindow MappingDetailsWindow
        {
            get
            {
                if (_mappingDetailsWindow == null)
                {
                    using (DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware))
                    {
                        _mappingDetailsWindow = GetToolWindow(typeof(MappingDetailsWindow), true) as MappingDetailsWindow;
                    }
                }
                return _mappingDetailsWindow;
            }
        }

        public EntityDesignModelManager ModelManager { get; } = new EntityDesignModelManager(new VSArtifactFactory(), new VSArtifactSetFactory());

        public ConnectionManager ConnectionManager { get; private set; }

        public DocumentFrameMgr DocumentFrameMgr { get; private set; }

        public ModelChangeEventListener ModelChangeEventListener { get; private set; }

        public AggregateProjectTypeGuidCache AggregateProjectTypeGuidCache { get; private set; }

        public ModelGenErrorCache ModelGenErrorCache { get; private set; }

        /// <summary>
        ///     This method is called by the docdata whenever the inherent filename changes. Clients should not call this
        ///     method directly; they should subscribe to MicrosoftDataEntityDesignPackage.FileNameChanged event.
        /// </summary>
        /// <param name="oldFileName"></param>
        /// <param name="newFileName"></param>
        public void OnFileNameChanged(string oldFileName, string newFileName)
        {
            ModelChangeEventArgs args = new ModelChangeEventArgs();
            args.OldFileName = oldFileName;
            args.NewFileName = newFileName;
            FileNameChanged(this, args);
        }

        /// <summary>
        ///     A user can build from the command line. This method determines if they have
        ///     executed a build from the command line so we can suppress any UI on our end.
        /// </summary>
        public bool IsBuildingFromCommandLine
        {
            get
            {
                // only calculate this once since the package load/unload is contained within a devenv /build
                if (_isBuildingFromCommandLine == null)
                {
                    try
                    {
                        // use the source code control property setting to determine if we are in command line mode
                        if (GetService(typeof(SVsShell)) is IVsShell shell)
                        {
                            var hr = shell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out object o);
                            if (NativeMethods.Succeeded(hr))
                            {
                                _isBuildingFromCommandLine = o as bool?;
                            }
                        }
                    }
                    catch (COMException)
                    {
                        // we catch and handle this in the finally block below
                    }
                    finally
                    {
                        // if for some reason we can't get the property from the shell, the shell is null, we get a COM exception, we can't cast, etc.
                        // then we assume the most popular case which is we aren't building from the command line.
                        _isBuildingFromCommandLine ??= false;
                    }
                }

                Debug.Assert(_isBuildingFromCommandLine != null, "Why couldn't we figure out if we were building from the command line?");
                return _isBuildingFromCommandLine.GetValueOrDefault(false);
            }
        }

        #endregion

        #region IVsTrackProjectRetargetingEvents members

        int IVsTrackProjectRetargetingEvents.OnRetargetingAfterChange(
            string projRef, IVsHierarchy pAfterChangeHier, string fromTargetFramework, string toTargetFramework)
        {
            new RetargetingHandler(pAfterChangeHier, PackageManager.Package).RetargetFilesInProject();
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingBeforeChange(
            string projRef, IVsHierarchy pBeforeChangeHier, string currentTargetFramework, string newTargetFramework, out bool pCanceled,
            out string ppReasonMsg)
        {
            pCanceled = false;
            ppReasonMsg = String.Empty;
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingBeforeProjectSave(
            string projRef, IVsHierarchy pBeforeChangeHier, string currentTargetFramework, string newTargetFramework)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingCanceledChange(
            string projRef, IVsHierarchy pBeforeChangeHier, string currentTargetFramework, string newTargetFramework)
        {
            return VSConstants.S_OK;
        }

        int IVsTrackProjectRetargetingEvents.OnRetargetingFailure(
            string projRef, IVsHierarchy pHier, string fromTargetFramework, string toTargetFramework)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
