// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.UI.Views.Dialogs;
using Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.WizardFramework;

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Gui
{
    internal partial class WizardPageDbGenSummary : WizardPageBase
    {
        private CancellationTokenSource _generationCancellation;
        private Label _statusLabel;
        private SynchronizationContext _synchronizationContext;
        private bool _addedDbConfigPage;
        private string _ddlFileExtension;

        // TODO: create strongly-typed properties in an options page type to store this information
        private const string RegKeyNameDdlOverwriteWarning = "DbGenShowOverwriteDDLWarning";
        private const string RegKeyNameEdmxOverwriteWarning = "DbGenShowEdmxOverwriteWarning";

        internal WizardPageDbGenSummary(ModelBuilderWizardForm wizard)
            : base(wizard)
        {
            InitializeComponent();

            Logo = Properties.Resources.PageIcon;
            Headline = Properties.Resources.DbGenSummary_Title;
            Id = "WizardPageGenerateDatabaseScriptId";
            ShowInfoPanel = false;

            HelpKeyword = null;

            _addedDbConfigPage = false;
        }

        public override bool OnActivate()
        {
            if (!Visited)
            {
                SummaryTabs.Enabled = false;
                txtSaveDdlAs.Enabled = false;
            }

            return base.OnActivate();
        }

        public override void OnActivated()
        {
            base.OnActivated();

            Debug.Assert(
                !Wizard.MovingNext || _generationCancellation is null,
                "Possible memory leak: We should have cancelled the old generation when activating WizardPageDbGenSummary");

            if (_generationCancellation is null)
            {
                if (LocalDataUtil.IsSqlMobileConnectionString(Wizard.ModelBuilderSettings.DesignTimeProviderInvariantName))
                {
                    _ddlFileExtension = DatabaseGenerationEngine._sqlceFileExtension;
                }
                else
                {
                    _ddlFileExtension = DatabaseGenerationEngine._ddlFileExtension;
                }

                // Add in the DbConfig page before if we have found a connection
                if (Wizard.Mode == ModelBuilderWizardForm.WizardMode.PerformDBGenSummaryOnly
                    && _addedDbConfigPage == false)
                {
                    Wizard.InsertPageBefore(Id, new WizardPageDbConfig(Wizard));
                    _addedDbConfigPage = true;
                }

                // Display the default path for the DDL
                var artifactProjectItem = VsUtils.GetProjectItemForDocument(
                    Wizard.ModelBuilderSettings.Artifact.Uri.LocalPath, PackageManager.Package);
                if (artifactProjectItem != null)
                {
                    txtSaveDdlAs.Text = DatabaseGenerationEngine.CreateDefaultDdlFileName(artifactProjectItem) + _ddlFileExtension;
                }

                // Disable all buttons except for Previous and Cancel
                Wizard.EnableButton(ButtonType.Previous, true);
                Wizard.EnableButton(ButtonType.Next, false);
                Wizard.EnableButton(ButtonType.Finish, false);
                Wizard.EnableButton(ButtonType.Cancel, true);

                // Display a status message
                ShowStatus(Properties.Resources.DbGenSummary_StatusDeterminingDDL);

                // Extract the XML from the EDMX file and convert it into an EdmItemCollection for the workflow
                EdmItemCollection edm = null;
                using (new VsUtils.HourglassHelper())
                {
                    edm = Wizard.ModelBuilderSettings.Artifact.GetEdmItemCollectionFromArtifact(out IList<EdmSchemaError> schemaErrors);

                    Debug.Assert(
                        edm != null && schemaErrors.Count == 0,
                        "EdmItemCollection schema errors found; we should have performed validation on the EdmItemCollection before instantiating the wizard.");
                }

                var existingSsdl = Wizard.ModelBuilderSettings.Artifact.GetSsdlAsString();

                // Attempt to get the template path and database schema name from the artifact. If we don't find them, we'll use defaults.
                var templatePath = DatabaseGenerationEngine.GetTemplatePathFromArtifact(Wizard.ModelBuilderSettings.Artifact);
                var databaseSchemaName = DatabaseGenerationEngine.GetDatabaseSchemaNameFromArtifact(Wizard.ModelBuilderSettings.Artifact);

                // Save off the SynchronizationContext so we can post methods to the UI event queue when
                // responding to workflow events (since they are executed in a separate thread)
                _synchronizationContext = SynchronizationContext.Current;

                // Generation is synchronous, so run it on the thread pool to keep the wizard responsive. The Workflow
                // engine used to provide the background thread implicitly.
                var artifactPath = Wizard.ModelBuilderSettings.Artifact.Uri.LocalPath;
                var project = Wizard.Project;
                var initialCatalog = Wizard.ModelBuilderSettings.InitialCatalog;
                var providerInvariantName = Wizard.ModelBuilderSettings.RuntimeProviderInvariantName;
                var connectionString = Wizard.ModelBuilderSettings.AppConfigConnectionString;
                var providerManifestToken = Wizard.ModelBuilderSettings.ProviderManifestToken;
                var schemaVersion = Wizard.ModelBuilderSettings.Artifact.SchemaVersion;

                _generationCancellation = new CancellationTokenSource();
                var cancellationToken = _generationCancellation.Token;

                Task.Run(
                    () => DatabaseGenerationEngine.GenerateDatabaseScript(
                        _synchronizationContext,
                        project,
                        artifactPath,
                        templatePath,
                        edm,
                        existingSsdl,
                        databaseSchemaName,
                        initialCatalog,
                        providerInvariantName,
                        connectionString,
                        providerManifestToken,
                        schemaVersion))
                    .ContinueWith(
                        task => OnGenerationCompleted(task, cancellationToken),
                        TaskScheduler.Default);
            }
        }

        // <summary>
        //     Marshals the result of database script generation back onto the UI thread.
        // </summary>
        // <remarks>
        //     Replaces the WorkflowApplication Completed and OnUnhandledException callbacks. Cancellation is checked
        //     against the token captured when the run started, so a run abandoned by navigating back cannot post
        //     results over a newer one.
        // </remarks>
        private void OnGenerationCompleted(Task<DatabaseScript> task, CancellationToken cancellationToken)
        {
            _synchronizationContext.Post(
                state =>
                    {
                        // If we navigated away from this page, the run is stale: leave the buttons to whoever
                        // cancelled it and discard the output.
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (task.IsFaulted)
                        {
                            // AggregateException.Message enumerates every inner exception, which reads poorly in a
                            // dialog; the first inner exception is the one the generators actually threw.
                            Exception exception = task.Exception.InnerException ?? task.Exception;
                            HandleError(exception.Message, true);
                            return;
                        }

                        var script = task.Result;

                        // Hide the status message on the DDL tab
                        HideStatus();

                        SummaryTabs.Enabled = true;
                        txtSaveDdlAs.Enabled = true;

                        // Immediately enable the Finish button now that generation has succeeded. We also enable going
                        // back to the connection page and cancelling out of the wizard completely.
                        Wizard.EnableButton(ButtonType.Previous, true);
                        Wizard.EnableButton(ButtonType.Next, false);
                        Wizard.EnableButton(ButtonType.Finish, true);
                        Wizard.EnableButton(ButtonType.Cancel, true);

                        if (!String.IsNullOrEmpty(script.Ssdl))
                        {
                            Wizard.ModelBuilderSettings.SsdlStringReader = new StringReader(script.Ssdl);
                        }

                        if (!String.IsNullOrEmpty(script.Msl))
                        {
                            Wizard.ModelBuilderSettings.MslStringReader = new StringReader(script.Msl);
                        }

                        if (!String.IsNullOrEmpty(script.Ddl))
                        {
                            Wizard.ModelBuilderSettings.DdlStringReader = new StringReader(script.Ddl);
                            InferTablesAndDisplayDDL(script.Ddl);
                        }

                        txtSaveDdlAs.Focus();
                    }, null);
        }

        // <summary>
        //     Invoked by the VS Wizard framework when this page is exited or when the "Finish" button is clicked.
        //     Updates ModelBuilderSettings from the GUI
        // </summary>
        public override bool OnDeactivate()
        {
            if (Wizard.MovingPrevious)
            {
                CleanupWorkflow();
            }

            return base.OnDeactivate();
        }

        internal override bool OnWizardFinish()
        {
            if (Wizard.ModelBuilderSettings.DdlStringReader != null)
            {
                // Make sure that the DDL filename is not null
                if (String.IsNullOrEmpty(txtSaveDdlAs.Text))
                {
                    VsUtils.ShowErrorDialog(Properties.Resources.ErrorDdlFileNameIsNull);
                    return false;
                }

                // Resolve the project URI
                Uri projectUri = null;
                var projectFullName = VsUtils.GetProjectPathWithName(Wizard.Project, out bool projectHasFilename);

                try
                {
                    if (false == Uri.TryCreate(projectFullName, UriKind.Absolute, out projectUri)
                        || projectUri == null)
                    {
                        VsUtils.ShowErrorDialog(
                            String.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorResolvingProjectFile, projectFullName));
                        return false;
                    }
                }
                catch (UriFormatException)
                {
                    VsUtils.ShowErrorDialog(
                        String.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorResolvingProjectFile, projectFullName));
                }

                // Attempt to create a URI from the DDL path, either relative to the project URI or absolute. 
                Uri ddlUri = null;
                try
                {
                    if (false == Uri.TryCreate(projectUri, txtSaveDdlAs.Text, out ddlUri)
                        || ddlUri == null)
                    {
                        VsUtils.ShowErrorDialog(
                            String.Format(
                                CultureInfo.CurrentCulture, Properties.Resources.ErrorResolvingDdlFileNameException, txtSaveDdlAs.Text));
                        return false;
                    }
                }
                catch (UriFormatException)
                {
                    VsUtils.ShowErrorDialog(
                        String.Format(
                            CultureInfo.CurrentCulture, Properties.Resources.ErrorResolvingDdlFileNameException, txtSaveDdlAs.Text));
                }

                var ddlFilePath = ddlUri.LocalPath;

                // Validate the file name
                try
                {
                    var ddlFileName = Path.GetFileName(ddlFilePath);
                    if (String.IsNullOrEmpty(ddlFileName))
                    {
                        VsUtils.ShowErrorDialog(
                            String.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorDdlPathNotFile, ddlFilePath));
                        return false;
                    }

                    if (!VsUtils.IsValidFileName(ddlFileName))
                    {
                        VsUtils.ShowErrorDialog(String.Format(CultureInfo.CurrentCulture, Resources.ErrorNonValidFileName, ddlFilePath));
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    VsUtils.ShowErrorDialog(
                        String.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorResolvingDdlFileNameException, ddlFilePath));
                    return false;
                }

                // Add ".sql" if the extension is not already .sql
                if (!Path.GetExtension(ddlFilePath).Equals(_ddlFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    ddlFilePath += _ddlFileExtension;
                }

                // Now we should have a valid, non-null filename
                Debug.Assert(
                    !String.IsNullOrEmpty(ddlFilePath),
                    "DDL filename should either be not null or we should have handled an exception before continuing...");
                if (String.IsNullOrEmpty(ddlFilePath))
                {
                    VsUtils.ShowErrorDialog(Properties.Resources.ErrorDdlFileNameIsNull);
                    return false;
                }

                // If the parent directory does not exist, then we do not proceed
                try
                {
                    FileInfo fileInfo = new FileInfo(ddlFilePath);
                    var parentDirInfo = fileInfo.Directory;
                    if (parentDirInfo != null)
                    {
                        if (false == parentDirInfo.Exists)
                        {
                            VsUtils.ShowErrorDialog(
                                String.Format(CultureInfo.CurrentCulture, Properties.Resources.ErrorNoDdlParentDir, ddlFilePath));
                            return false;
                        }
                    }
                }
                catch (Exception e)
                {
                    // various exceptions could occur here, such as PathTooLong or Security. In this case we will display an error.
                    VsUtils.ShowErrorDialog(
                        String.Format(
                            CultureInfo.CurrentCulture, Properties.Resources.ErrorCouldNotParseDdlFileName, ddlFilePath, e.Message));
                    return false;
                }

                // Display the DDL Overwrite Warning Dialog
                if (File.Exists(ddlFilePath))
                {
                    var displayDdlOverwriteWarning = true;
                    try
                    {
                        var ddlOverwriteWarningString = EdmUtils.GetUserSetting(RegKeyNameDdlOverwriteWarning);
                        if (false == String.IsNullOrEmpty(ddlOverwriteWarningString)
                            && false == Boolean.TryParse(ddlOverwriteWarningString, out displayDdlOverwriteWarning))
                        {
                            displayDdlOverwriteWarning = true;
                        }
                        if (displayDdlOverwriteWarning)
                        {
                            var cancelledDuringOverwriteDdl = DismissableWarningDialog.ShowWarningDialogAndSaveDismissOption(
                                Resources.DatabaseCreation_DDLOverwriteWarningTitle,
                                String.Format(CultureInfo.CurrentCulture, Resources.DatabaseCreation_WarningOverwriteDdl, ddlFilePath),
                                RegKeyNameDdlOverwriteWarning,
                                DismissableWarningDialog.ButtonMode.YesNo);
                            if (cancelledDuringOverwriteDdl)
                            {
                                return false;
                            }
                        }
                    }
                    catch (SecurityException e)
                    {
                        // We should at least alert the user of why this is failing so they can take steps to fix it.
                        VsUtils.ShowMessageBox(
                            Services.ServiceProvider,
                            String.Format(
                                CultureInfo.CurrentCulture, Resources.ErrorReadingWritingUserSetting, RegKeyNameDdlOverwriteWarning,
                                e.Message),
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                            OLEMSGICON.OLEMSGICON_WARNING);
                    }
                }

                // At this point we will save off the DDL Filename into the wizard settings
                Wizard.ModelBuilderSettings.DdlFileName = ddlFilePath;
            }

            if ((Wizard.ModelBuilderSettings.SsdlStringReader != null ||
                 Wizard.ModelBuilderSettings.MslStringReader != null)
                && (Wizard.ModelBuilderSettings.Artifact != null)
                && (Wizard.ModelBuilderSettings.Artifact.StorageModel() != null)
                && (!Wizard.ModelBuilderSettings.Artifact.StorageModel().IsEmpty))
            {
                // Display the SSDL/MSL Overwrite Warning Dialog
                var displayEdmxOverwriteWarning = true;
                try
                {
                    var edmxOverwriteWarningString = EdmUtils.GetUserSetting(RegKeyNameEdmxOverwriteWarning);
                    if (false == String.IsNullOrEmpty(edmxOverwriteWarningString)
                        && false == Boolean.TryParse(edmxOverwriteWarningString, out displayEdmxOverwriteWarning))
                    {
                        displayEdmxOverwriteWarning = true;
                    }
                    if (displayEdmxOverwriteWarning)
                    {
                        var cancelledDuringOverwriteSsdl = DismissableWarningDialog.ShowWarningDialogAndSaveDismissOption(
                            Resources.DatabaseCreation_EdmxOverwriteWarningTitle,
                            Resources.DatabaseCreation_WarningOverwriteMappings,
                            RegKeyNameEdmxOverwriteWarning,
                            DismissableWarningDialog.ButtonMode.YesNo);
                        if (cancelledDuringOverwriteSsdl)
                        {
                            return false;
                        }
                    }
                }
                catch (SecurityException e)
                {
                    // We should at least alert the user of why this is failing so they can take steps to fix it.
                    VsUtils.ShowMessageBox(
                        Services.ServiceProvider,
                        String.Format(
                            CultureInfo.CurrentCulture, Resources.ErrorReadingWritingUserSetting, RegKeyNameEdmxOverwriteWarning,
                            e.Message),
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                        OLEMSGICON.OLEMSGICON_WARNING);
                }
            }

            using (new VsUtils.HourglassHelper())
            {
                // Now we output the DDL, update app/web.config, update the edmx, and open the SQL file that gets produced
                return DatabaseGenerationEngine.UpdateEdmxAndEnvironment(Wizard.ModelBuilderSettings);
            }
        }

        internal override void OnWizardCancel()
        {
            base.OnWizardCancel();

            Wizard.EnableButton(ButtonType.Cancel, false);
            CleanupWorkflow();
        }

        private void HideStatus()
        {
            if (_statusLabel != null)
            {
                SummaryTabs.SelectedTab.Controls.Remove(_statusLabel);
                _statusLabel = null;
            }
        }

        private void ShowStatus(string message)
        {
            if (_statusLabel == null)
            {
                _statusLabel = new Label();
                _statusLabel.BackColor = txtDDL.BackColor;
                _statusLabel.Size = txtDDL.ClientSize;
                _statusLabel.Location = new Point(
                    txtDDL.Left + SystemInformation.Border3DSize.Width,
                    txtDDL.Top + SystemInformation.Border3DSize.Height);
                _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
                _statusLabel.Anchor = txtDDL.Anchor;
            }

            _statusLabel.Text = message;

            var tabPage = SummaryTabs.SelectedTab;
            tabPage.Controls.Add(_statusLabel);
            tabPage.Controls.SetChildIndex(_statusLabel, 0);
        }

        // <summary>
        //     Abandons an in-flight generation run and restores the buttons for navigating back.
        // </summary>
        // <remarks>
        //     Generation cannot be interrupted once started, so this signals the token rather than killing the work:
        //     the run finishes on the thread pool and its continuation discards the result. The old
        //     WorkflowApplication.Abort had the same practical effect.
        // </remarks>
        private void CleanupWorkflow()
        {
            if (_generationCancellation is null)
            {
                return;
            }

            _generationCancellation.Cancel();
            _generationCancellation.Dispose();
            _generationCancellation = null;

            Wizard.EnableButton(ButtonType.Previous, false);
            Wizard.EnableButton(ButtonType.Next, true);
            Wizard.EnableButton(ButtonType.Finish, false);
            Wizard.EnableButton(ButtonType.Cancel, true);
        }

        private void HandleError(string message, bool displayErrorDialog)
        {
            // Generation failed
            if (displayErrorDialog)
            {
                VsUtils.ShowErrorDialog(message);
            }
            ShowStatus(message);

            // ...Otherwise, just stay on the summary page and
            // disable all buttons except for Previous and Cancel
            Wizard.EnableButton(ButtonType.Previous, true);
            Wizard.EnableButton(ButtonType.Next, false);
            Wizard.EnableButton(ButtonType.Finish, false);
            Wizard.EnableButton(ButtonType.Cancel, true);
        }

        private void InferTablesAndDisplayDDL(string ddl)
        {
            // first we'll get the right TSData Extension from the provider

            // then we'll load up a ModelBuilder

            // finally, AddObjects(ddl)

            // from the ModelBuilder, we can examine the tables, etc.

            // we'll also serialize the ddl
            txtDDL.Text = ddl;
        }

        private void txtSaveDdlAs_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSaveDdlAs.Text.Trim()))
            {
                Wizard.EnableButton(ButtonType.Finish, false);
            }
            else
            {
                Wizard.EnableButton(ButtonType.Finish, true);
            }
        }
    }
}