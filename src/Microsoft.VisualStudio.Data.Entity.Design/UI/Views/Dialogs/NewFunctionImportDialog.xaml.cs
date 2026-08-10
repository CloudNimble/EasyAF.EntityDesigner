// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EntityProperty = Microsoft.Data.Entity.Design.Model.Entity.Property;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Database;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.Data.Entity.Design.VisualStudio.Data.Sql;
using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.Data.Tools.VSXmlDesignerBase.Common;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.PlatformUI;
using ComplexType = Microsoft.Data.Entity.Design.Model.Entity.ComplexType;
using EntityType = Microsoft.Data.Entity.Design.Model.Entity.EntityType;
using XmlDesignerBaseResources = Microsoft.Data.Tools.XmlDesignerBase.Resources;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal partial class NewFunctionImportDialog : DialogWindow
    {
        #region TESTS

        private static event EventHandler DialogActivatedTestEventStorage;
        private static event EventHandler GetResultColumnsCompletedEventStorage;

        internal static event EventHandler DialogActivatedTestEvent
        {
            add { DialogActivatedTestEventStorage += value; }
            remove { DialogActivatedTestEventStorage -= value; }
        }

        internal static event EventHandler GetResultColumnsCompletedEvent
        {
            add { GetResultColumnsCompletedEventStorage += value; }
            remove { GetResultColumnsCompletedEventStorage -= value; }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (FunctionImportNameTextBox.IsEnabled)
            {
                FunctionImportNameTextBox.Focus();
            }

            DialogActivatedTestEventStorage?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        private enum DialogMode
        {
            New,
            FullEdit
        }

        private const int NotSupportedDataType = -1;

        private bool _needsValidation;
        private bool _isWorkerRunning;
        private FeatureState _complexTypeFeatureState;
        private FeatureState _composableFunctionImportFeatureState;
        private FeatureState _getColumnInformationFeatureState;
        private readonly DialogMode _mode;
        private IDataSchemaProcedure _lastGeneratedStoredProc;
        private readonly ConceptualEntityContainer _container;
        private ConnectionManager.ConnectionString _connectionString;
        private Project _currentProject;
        private bool _updateSelectedComplexType;

        private readonly FunctionImport _editedFunctionImport;
        private IVsDataConnection _openedDbConnection;
        private readonly ICollection<Function> _functions;
        private string[] _sortedEdmPrimitiveTypes;

        internal NewFunctionImportDialog(
            Function baseFunction,
            string functionImportName,
            ICollection<Function> functions,
            IEnumerable<ComplexType> complexTypes,
            IEnumerable<EntityType> entityTypes,
            ConceptualEntityContainer container,
            object selectedElement)
        {
            _mode = DialogMode.New;
            _editedFunctionImport = container.FunctionImports().Where(x => x.LocalName.Value == functionImportName).FirstOrDefault();
            if (_editedFunctionImport != null)
            {
                _mode = DialogMode.FullEdit;
            }
            _functions = functions;
            _lastGeneratedStoredProc = null;
            _container = container;
            _updateSelectedComplexType = false;

            InitializeSupportedFeatures();
            InitializeComponent();
            this.HasHelpButton = false;

            if (_composableFunctionImportFeatureState.IsEnabled())
            {
                if (DialogMode.FullEdit == _mode)
                {
                    FunctionImportComposableCheckBox.IsChecked = (BoolOrNone.TrueValue == _editedFunctionImport.IsComposable.Value);
                }
                else
                {
                    Debug.Assert(_mode == DialogMode.New, "Unexpected mode");
                    FunctionImportComposableCheckBox.IsChecked = baseFunction != null && baseFunction.IsComposable.Value;
                }
            }

            if (!_getColumnInformationFeatureState.IsVisible())
            {
                UpdateComplexTypeButton.Visibility = Visibility.Collapsed;
                ReturnTypeShapeGroup.Visibility = Visibility.Collapsed;
            }

            PopulateComboBoxes(complexTypes, entityTypes, functions);
            UpdateStateComboBoxes(selectedElement, baseFunction, functionImportName);
            CheckOkButtonEnabled();
            UpdateReturnTypeComboBoxesState();
            UpdateReturnTypeInfoAreaState();
            SetCreateNewComplexTypeButtonProperties();

            Closed += OnDialogClosed;
        }

        private void OnDialogClosed(object sender, EventArgs e)
        {
            Debug.Assert(
                _openedDbConnection == null,
                "There is still open DBConnection when NewFunctionImportDialog is closing.");
            _openedDbConnection?.Close();
            _openedDbConnection = null;
        }

        #region Overriden Methods

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_needsValidation)
            {
                Debug.Assert(ReturnType != null, "ReturnType is null.");

                _needsValidation = false;
                if (!EscherAttributeContentValidator.IsValidCsdlFunctionImportName(FunctionImportName))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewFunctionImportDialog_InvalidFunctionImportNameMsg);
                    e.Cancel = true;
                    FunctionImportNameTextBox.Focus();
                }
                else
                {
                    if (_mode == DialogMode.New
                        && !ModelHelper.IsUniqueName(typeof(FunctionImport), _container, FunctionImportName, false, out string msg))
                    {
                        VsUtils.ShowErrorDialog(DialogsResource.NewFunctionImportDialog_EnsureUniqueNameMsg);
                        e.Cancel = true;
                        FunctionImportNameTextBox.Focus();
                        return;
                    }
                }

                if (ComplexTypeReturnButton.IsChecked == true
                    && ComplexTypeReturnComboBox.SelectedItem == null
                    && !EscherAttributeContentValidator.IsValidCsdlComplexTypeName(ComplexTypeReturnComboBox.Text))
                {
                    var errorMessage = String.Format(
                        CultureInfo.CurrentCulture, DialogsResource.NewFunctionImportDialog_NotValidComplexTypeName,
                        ComplexTypeReturnComboBox.Text);
                    VsUtils.ShowErrorDialog(errorMessage);
                    e.Cancel = true;
                    ComplexTypeReturnComboBox.Focus();
                    return;
                }
            }

            if (ComplexTypeReturnButton.IsChecked != true)
            {
                Schema = null;
            }
            else if (ComplexTypeReturnButton.IsChecked == true
                     && ComplexTypeReturnComboBox.SelectedItem != null
                     && !_updateSelectedComplexType)
            {
                Schema = null;
            }
        }

        #endregion

        #region Internal Methods

        internal Function Function => StoredProcComboBox.SelectedItem as Function;

        internal string FunctionImportName => FunctionImportNameTextBox.Text;

        internal object ReturnType
        {
            get
            {
                if (EmptyReturnTypeButton.IsChecked == true)
                {
                    return XmlDesignerBaseResources.NoneDisplayValueUsedForUX;
                }
                else if (ScalarTypeReturnButton.IsChecked == true)
                {
                    return ScalarTypeReturnComboBox.SelectedItem;
                }
                else if (ComplexTypeReturnButton.IsChecked == true)
                {
                    if (ComplexTypeReturnComboBox.SelectedItem != null)
                    {
                        return ComplexTypeReturnComboBox.SelectedItem;
                    }
                    else if (!String.IsNullOrEmpty(ComplexTypeReturnComboBox.Text))
                    {
                        return ComplexTypeReturnComboBox.Text;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (EntityTypeReturnButton.IsChecked == true)
                {
                    return EntityTypeReturnComboBox.SelectedItem;
                }
                else
                {
                    return null;
                }
            }
        }

        internal bool IsComposable => FunctionImportComposableCheckBox.IsChecked == true;

        internal IDataSchemaProcedure Schema
        {
            get => _lastGeneratedStoredProc;
            private set
            {
                _lastGeneratedStoredProc = value;
                SetCreateNewComplexTypeButtonProperties();
            }
        }

        private bool IsResultTypeAvailable => (Schema != null && Schema.Columns != null && Schema.Columns.Count > 0);

        #endregion

        #region Event handler

        private void StoredProcComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Schema = null;
            _updateSelectedComplexType = false;
            UpdateReturnTypeComboBoxesState();
            UpdateReturnTypeInfoAreaState();
            CheckOkButtonEnabled();
            SetComplexTypeReturnComboBoxStyle(false);
        }

        private void ScalarTypeReturnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReturnTypeComboBoxesState();
            CheckOkButtonEnabled();
        }

        private void EntityTypeReturnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReturnTypeComboBoxesState();
            CheckOkButtonEnabled();
        }

        private void ComplexTypeReturnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updateSelectedComplexType)
            {
                _updateSelectedComplexType = false;

                Debug.Assert(
                    Schema != null && Schema.Columns != null,
                    "Either schema or schema's columns is null when the dialog is in diff mode.");

                if (Schema != null && Schema.Columns != null)
                {
                    if (Schema.Columns.Count > 0)
                    {
                        UpdateComplexTypeList(Schema.Columns);
                        UpdateReturnTypeInfoAreaState();
                    }
                    else
                    {
                        DetectErrorLabel.Text = DialogsResource.NewFunctionImportDialog_NoColumnsReturned;
                        ReturnTypeShapeTabControl.SelectedItem = DetectErrorTabPage;
                        UpdateReturnTypeListState(false);
                    }
                }
            }

            UpdateReturnTypeComboBoxesState();
            CheckOkButtonEnabled();
            SetComplexTypeReturnComboBoxStyle(false);
        }

        private void FunctionImportNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckOkButtonEnabled();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _needsValidation = true;
            DialogResult = true;
        }

        private void FunctionImportComposableCheckBox_Click(object sender, RoutedEventArgs e)
        {
            PopulateStoreProcedureList(_functions);
            CheckOkButtonEnabled();
        }

        private void ReturnTypeButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateReturnTypeComboBoxesState();
            CheckOkButtonEnabled();
        }

        private void ReturnTypeButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender == EmptyReturnTypeButton)
            {
                if (e.Key == System.Windows.Input.Key.Up)
                {
                    StoredProcComboBox.Focus();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Down)
                {
                    ScalarTypeReturnButton.IsChecked = true;
                    ScalarTypeReturnButton.Focus();
                    e.Handled = true;
                }
            }
            else if (sender == ScalarTypeReturnButton)
            {
                if (e.Key == System.Windows.Input.Key.Up)
                {
                    EmptyReturnTypeButton.IsChecked = true;
                    EmptyReturnTypeButton.Focus();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Down)
                {
                    ComplexTypeReturnButton.IsChecked = true;
                    ComplexTypeReturnButton.Focus();
                    e.Handled = true;
                }
            }
            else if (sender == ComplexTypeReturnButton)
            {
                if (e.Key == System.Windows.Input.Key.Up)
                {
                    ScalarTypeReturnButton.IsChecked = true;
                    ScalarTypeReturnButton.Focus();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Down)
                {
                    EntityTypeReturnButton.IsChecked = true;
                    EntityTypeReturnButton.Focus();
                    e.Handled = true;
                }
            }
            else if (sender == EntityTypeReturnButton)
            {
                if (e.Key == System.Windows.Input.Key.Up)
                {
                    ComplexTypeReturnButton.IsChecked = true;
                    ComplexTypeReturnButton.Focus();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Down)
                {
                    GetColumnInformationButton.Focus();
                    e.Handled = true;
                }
            }
        }

        private void GetColumnInformationButton_Click(object sender, RoutedEventArgs e)
        {
            _updateSelectedComplexType = false;
            LaunchBackgroundProcessToRetrieveColumnsSchema();
        }

        private void CreateNewComplexTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_updateSelectedComplexType)
            {
                _updateSelectedComplexType = false;
                UpdateComplexTypeList(Schema.Columns);
            }
            UpdateComplexTypeButton.IsEnabled = false;

            ComplexTypeReturnComboBox.SelectedItem = null;
            ComplexTypeReturnButton.IsChecked = true;
            ComplexTypeReturnButton.FontWeight = FontWeights.Bold;

            SetComplexTypeReturnComboBoxStyle(true);
            ComplexTypeReturnComboBox.Focus();

            var complexTypeBaseName = (string.IsNullOrEmpty(FunctionImportName) ? Function.LocalName.Value : FunctionImportName);
            var complexTypeName = GetComplexTypeName(complexTypeBaseName);
            ComplexTypeReturnComboBox.Text = complexTypeName;
            CheckOkButtonEnabled();
        }

        private void UpdateComplexTypeButton_Click(object sender, RoutedEventArgs e)
        {
            _updateSelectedComplexType = true;
            if (Schema != null)
            {
                UpdateComplexTypeList(Schema.Columns);
            }
            else
            {
                LaunchBackgroundProcessToRetrieveColumnsSchema();
            }
        }

        #endregion

        #region Private Methods

        private void CheckOkButtonEnabled()
        {
            if (Function == null
                || String.IsNullOrEmpty(FunctionImportName)
                || ReturnType == null
                || _isWorkerRunning)
            {
                OkButton.IsEnabled = false;
            }
            else
            {
                OkButton.IsEnabled = true;
            }
        }

        private string GetComplexTypeName(string baseName)
        {
            Debug.Assert(!string.IsNullOrEmpty(baseName), "baseName should not be null or empty");

            var complexTypeName = String.Format(CultureInfo.CurrentCulture, "{0}_Result", baseName);
            var model = _container.Artifact.ConceptualModel();
            complexTypeName = ModelHelper.GetUniqueName(typeof(ComplexType), model, complexTypeName);

            return complexTypeName;
        }

        private void UpdateReturnTypeComboBoxesState()
        {
            ScalarTypeReturnComboBox.IsEnabled = ScalarTypeReturnButton.IsChecked == true;
            ComplexTypeReturnComboBox.IsEnabled = ComplexTypeReturnButton.IsChecked == true && _complexTypeFeatureState.IsEnabled();
            UpdateComplexTypeButton.IsEnabled = ComplexTypeReturnComboBox.IsEnabled
                                                && (ComplexTypeReturnComboBox.SelectedItem != null)
                                                && (StoredProcComboBox.SelectedItem != null);
            EntityTypeReturnComboBox.IsEnabled = EntityTypeReturnButton.IsChecked == true;

            EmptyReturnTypeButton.FontWeight = FontWeights.Normal;
            ScalarTypeReturnButton.FontWeight = FontWeights.Normal;
            ComplexTypeReturnButton.FontWeight = FontWeights.Normal;
            EntityTypeReturnButton.FontWeight = FontWeights.Normal;

            ComplexTypeReturnButton.IsEnabled = _complexTypeFeatureState.IsEnabled();
        }

        private void UpdateReturnTypeListState(bool isEnabled)
        {
            ReturnTypeShapeListView.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
            ReturnTypeShapeTabControl.Visibility = !isEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetDoWorkState(bool isRunning)
        {
            if (isRunning)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                FunctionImportNameTextBox.IsEnabled = false;
                FunctionImportComposableCheckBox.IsEnabled = false;
                StoredProcComboBox.IsEnabled = false;
                ScalarTypeReturnComboBox.IsEnabled = false;
                ComplexTypeReturnComboBox.IsEnabled = false;
                EntityTypeReturnComboBox.IsEnabled = false;
                EmptyReturnTypeButton.IsEnabled = false;
                ScalarTypeReturnButton.IsEnabled = false;
                ComplexTypeReturnButton.IsEnabled = false;
                EntityTypeReturnButton.IsEnabled = false;
                GetColumnInformationButton.IsEnabled = false;
                CreateNewComplexTypeButton.IsEnabled = false;
                ReturnTypeShapeTabControl.SelectedItem = DetectingTabPage;
            }
            else
            {
                Mouse.OverrideCursor = null;

                FunctionImportNameTextBox.IsEnabled = true;
                FunctionImportComposableCheckBox.IsEnabled = _composableFunctionImportFeatureState.IsEnabled();
                StoredProcComboBox.IsEnabled = true;
                EmptyReturnTypeButton.IsEnabled = true;
                ScalarTypeReturnButton.IsEnabled = true;
                ComplexTypeReturnButton.IsEnabled = true;
                EntityTypeReturnButton.IsEnabled = true;
                GetColumnInformationButton.IsEnabled = true;
                ReturnTypeShapeTabControl.SelectedItem = DetectTabPage;
                UpdateReturnTypeComboBoxesState();
            }

            CheckOkButtonEnabled();
        }

        private bool UpdateComplexTypeList(IList<IDataSchemaColumn> columns)
        {
            ReturnTypeShapeListView.Items.Clear();

            if (!_updateSelectedComplexType)
            {
                // Normal mode - show stored procedure return columns
                var view = ReturnTypeShapeListView.View as GridView;
                if (view != null && view.Columns.Contains(ColumnAction))
                {
                    view.Columns.Remove(ColumnAction);
                }

                foreach (var column in columns)
                {
                    ReturnTypeShapeListView.Items.Add(CreateReturnTypeShapeListItem(column, null, null));
                }
            }
            else
            {
                // Update complex type mode - show diff
                var view = ReturnTypeShapeListView.View as GridView;
                if (view != null && !view.Columns.Contains(ColumnAction))
                {
                    view.Columns.Insert(0, ColumnAction);
                }

                List<IDataSchemaColumn> sortedColumns = columns.OrderBy(col => col.Name).ToList();
                ComplexType selectedComplexType = ComplexTypeReturnComboBox.SelectedItem as ComplexType;
                Debug.Assert(selectedComplexType != null, "There is no selected complex type.");

                if (selectedComplexType != null)
                {
                    List<EntityProperty> sortedProperties = selectedComplexType.Properties().OrderBy(
                        entityProperty =>
                        EdmUtils.GetFunctionImportResultColumnName(_editedFunctionImport, entityProperty)).ToList();

                    var propertyIndex = 0;
                    EntityProperty prop;
                    var propertyName = String.Empty;

                    var storageModel = _container.Artifact.StorageModel();
                    for (var i = 0; i < sortedColumns.Count; i++)
                    {
                        var col = sortedColumns[i];
                        prop = null;

                        while (propertyIndex < sortedProperties.Count)
                        {
                            prop = sortedProperties[propertyIndex];
                            propertyName = EdmUtils.GetFunctionImportResultColumnName(_editedFunctionImport, prop);
                            if (String.Compare(propertyName, col.Name, StringComparison.CurrentCulture) >= 0)
                            {
                                break;
                            }
                            ReturnTypeShapeListView.Items.Add(
                                CreateReturnTypeShapeListItem(
                                    null, prop, DialogsResource.NewFunctionImportDialog_ReturnTypeListDeleteAction));
                            propertyIndex++;
                        }

                        if (prop == null)
                        {
                            ReturnTypeShapeListView.Items.Add(
                                CreateReturnTypeShapeListItem(col, null, DialogsResource.NewFunctionImportDialog_ReturnTypeListAddAction));
                        }
                        else if (String.Compare(col.Name, propertyName, StringComparison.CurrentCulture) == 0)
                        {
                            if (ModelHelper.IsPropertyEquivalentToSchemaColumn(storageModel, prop, col))
                            {
                                ReturnTypeShapeListView.Items.Add(
                                    CreateReturnTypeShapeListItem(col, null, DialogsResource.NewFunctionImportDialog_ReturnTypeListNoAction));
                            }
                            else
                            {
                                ReturnTypeShapeListView.Items.Add(
                                    CreateReturnTypeShapeListItem(
                                        col, prop, DialogsResource.NewFunctionImportDialog_ReturnTypeListUpdateAction));
                            }
                            propertyIndex++;
                        }
                        else
                        {
                            ReturnTypeShapeListView.Items.Add(
                                CreateReturnTypeShapeListItem(col, null, DialogsResource.NewFunctionImportDialog_ReturnTypeListAddAction));
                        }
                    }

                    while (propertyIndex < sortedProperties.Count)
                    {
                        prop = sortedProperties[propertyIndex];
                        ReturnTypeShapeListView.Items.Add(
                            CreateReturnTypeShapeListItem(null, prop, DialogsResource.NewFunctionImportDialog_ReturnTypeListDeleteAction));
                        propertyIndex++;
                    }
                }
            }

            return (ReturnTypeShapeListView.Items.Count > 0);
        }

        private ReturnTypeColumnItem CreateReturnTypeShapeListItem(IDataSchemaColumn column, EntityProperty property, string action)
        {
            var storageModel = _container.Artifact.StorageModel();
            Debug.Assert(storageModel != null, "Storage model is null");
            Debug.Assert(column != null || property != null, "Both data schema column and complex type property is null");

            var isUpdateMode = action == DialogsResource.NewFunctionImportDialog_ReturnTypeListUpdateAction;

            PrimitiveType columnPrimitiveType = null;
            if (column != null && column.ProviderDataType != NotSupportedDataType)
            {
                columnPrimitiveType = ModelHelper.GetPrimitiveType(storageModel, column.NativeDataType, column.ProviderDataType);
            }

            if (column != null && columnPrimitiveType == null && !String.IsNullOrEmpty(action))
            {
                action = DialogsResource.NewFunctionImportDialog_ReturnTypeListNoAction;
            }

            var item = new ReturnTypeColumnItem
            {
                Action = action ?? string.Empty,
                Name = column != null ? column.Name : property?.LocalName.Value ?? string.Empty
            };

            PrimitiveType propertyPrimitiveType = null;
            if (property != null)
            {
                propertyPrimitiveType = ModelHelper.GetPrimitiveTypeFromString(property.TypeName);
            }

            if (column != null && columnPrimitiveType == null)
            {
                item.EdmType = DialogsResource.NewFunctionImportDialog_NotSupportedColumnType;
            }
            else
            {
                item.EdmType = GetReturnTypeListViewCellText(
                    isUpdateMode,
                    propertyPrimitiveType?.GetEdmPrimitiveType().Name,
                    columnPrimitiveType?.GetEdmPrimitiveType().Name);
            }

            item.DbType = GetReturnTypeListViewCellText(false, null, columnPrimitiveType?.Name);

            item.Nullable = GetReturnTypeListViewCellText(
                isUpdateMode,
                property != null ? GetColumnNullableFacetText(property.Nullable) : null,
                column != null ? GetColumnNullableFacetText(column.IsNullable) : null);

            string propertySize = null;
            if (property != null
                && ModelHelper.IsValidModelFacet(property.TypeName, EntityProperty.AttributeMaxLength)
                && property.MaxLength != null
                && property.MaxLength.Value != null)
            {
                propertySize = property.MaxLength.Value.ToString();
            }
            item.MaxLength = GetReturnTypeListViewCellText(
                isUpdateMode, propertySize,
                column != null ? ModelHelper.GetMaxLengthFacetText(column.Size) : null);

            string propertyPrecision = null;
            if (property != null
                && ModelHelper.IsValidModelFacet(property.TypeName, EntityProperty.AttributePrecision)
                && property.Precision != null)
            {
                propertyPrecision = property.Precision.Value.ToString();
            }
            item.Precision = GetReturnTypeListViewCellText(
                isUpdateMode, propertyPrecision,
                column != null && column.Precision != null ? column.Precision.ToString() : null);

            string propertyScale = null;
            if (property != null
                && ModelHelper.IsValidModelFacet(property.TypeName, EntityProperty.AttributeScale)
                && property.Scale != null)
            {
                propertyScale = property.Scale.Value.ToString();
            }
            item.Scale = GetReturnTypeListViewCellText(
                isUpdateMode, propertyScale,
                column != null && column.Scale != null ? column.Scale.ToString() : null);

            return item;
        }

        private static string GetColumnNullableFacetText(DefaultableValueBoolOrNone nullableDefaultableValue)
        {
            var primitiveValue = nullableDefaultableValue.Value.PrimitiveValue;
            if (null != nullableDefaultableValue.Value.StringValue)
            {
                StringOrPrimitive<bool> defaultValue = nullableDefaultableValue.DefaultValue;
                if (null == defaultValue.StringValue)
                {
                    primitiveValue = defaultValue.PrimitiveValue;
                }
                else
                {
                    return null;
                }
            }

            return primitiveValue ? Model.Mapping.Condition.IsNullConstant : Model.Mapping.Condition.IsNotNullConstant;
        }

        private static string GetColumnNullableFacetText(bool isNullable)
        {
            return isNullable ? Model.Mapping.Condition.IsNullConstant : Model.Mapping.Condition.IsNotNullConstant;
        }

        private static string GetReturnTypeListViewCellText(bool isUpdateMode, string sourceText, string targetText)
        {
            if (String.IsNullOrEmpty(sourceText) && String.IsNullOrEmpty(targetText))
            {
                return String.Empty;
            }
            else if (isUpdateMode)
            {
                if (String.Compare(sourceText, targetText, StringComparison.CurrentCulture) != 0)
                {
                    return String.Format(
                        CultureInfo.CurrentCulture,
                        DialogsResource.NewFunctionImportDialog_ReturnTypeListViewItemChange, sourceText, targetText);
                }
                else
                {
                    return targetText;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(targetText))
                {
                    return targetText;
                }
                else if (!string.IsNullOrEmpty(sourceText))
                {
                    return sourceText;
                }
            }
            return String.Empty;
        }

        private void PopulateComboBoxes(
            IEnumerable<ComplexType> complexTypes,
            IEnumerable<EntityType> entityTypes,
            ICollection<Function> functions)
        {
            foreach (var type in _sortedEdmPrimitiveTypes)
            {
                ScalarTypeReturnComboBox.Items.Add(type);
            }

            if (complexTypes != null)
            {
                var complexTypesArray = complexTypes.ToArray();
                Array.Sort(complexTypesArray, new EFNameableItemComparer());
                foreach (var ct in complexTypesArray)
                {
                    ComplexTypeReturnComboBox.Items.Add(ct);
                }
            }

            if (entityTypes != null)
            {
                var entityTypesArray = entityTypes.ToArray();
                Array.Sort(entityTypesArray, new EFNameableItemComparer());
                foreach (var et in entityTypesArray)
                {
                    EntityTypeReturnComboBox.Items.Add(et);
                }
            }

            PopulateStoreProcedureList(functions);
        }

        private void UpdateReturnTypeInfoAreaState()
        {
            TabItem page;

            GetColumnInformationButton.IsEnabled = false;

            if (CurrentProject == null || ConnectionString == null)
            {
                page = ConnectionTabPage;
            }
            else if (!_complexTypeFeatureState.IsEnabled()
                     && Schema != null
                     && Schema.Columns.Count > 1)
            {
                page = DetectErrorTabPage;
            }
            else if (StoredProcComboBox.SelectedItem != null)
            {
                GetColumnInformationButton.IsEnabled = true;
                page = DetectTabPage;
            }
            else
            {
                page = SelectTabPage;
            }

            UpdateReturnTypeListState(IsResultTypeAvailable);
            ReturnTypeShapeTabControl.SelectedItem = page;
        }

        private void UpdateStateComboBoxes(object selectedElement, Function baseFunction, string functionImportName)
        {
            if (selectedElement is string selectedElementAsString
                && XmlDesignerBaseResources.NoneDisplayValueUsedForUX != selectedElementAsString)
            {
                if (ScalarTypeReturnComboBox.Items.Contains(selectedElementAsString))
                {
                    ScalarTypeReturnComboBox.SelectedItem = selectedElementAsString;
                    ScalarTypeReturnButton.IsChecked = true;
                }
                else
                {
                    Debug.Fail(
                        "Selected Element " + selectedElementAsString
                        + " represents a primitive type but is not in the primitive types drop-down");
                    EmptyReturnTypeButton.IsChecked = true;
                }
            }
            else if (selectedElement is ComplexType selectedElementAsComplexType)
            {
                if (ComplexTypeReturnComboBox.Items.Contains(selectedElementAsComplexType))
                {
                    ComplexTypeReturnComboBox.SelectedItem = selectedElementAsComplexType;
                    ComplexTypeReturnButton.IsChecked = true;
                }
                else
                {
                    Debug.Fail(
                        "Selected Element " + selectedElementAsComplexType.ToPrettyString()
                        + " is a ComplexType but is not in ComplexType drop-down");
                    EmptyReturnTypeButton.IsChecked = true;
                }
            }
            else if (selectedElement is EntityType selectedElementAsEntityType)
            {
                if (EntityTypeReturnComboBox.Items.Contains(selectedElementAsEntityType))
                {
                    EntityTypeReturnComboBox.SelectedItem = selectedElementAsEntityType;
                    EntityTypeReturnButton.IsChecked = true;
                }
                else
                {
                    Debug.Fail(
                        "Selected Element " + selectedElementAsEntityType.ToPrettyString()
                        + " is an EntityType but is not in EntityType drop-down");
                    EmptyReturnTypeButton.IsChecked = true;
                }
            }
            else
            {
                EmptyReturnTypeButton.IsChecked = true;
            }

            if (false == string.IsNullOrEmpty(functionImportName))
            {
                FunctionImportNameTextBox.Text = functionImportName;
            }

            if (baseFunction != null)
            {
                StoredProcComboBox.SelectedItem = baseFunction;
                if (string.IsNullOrEmpty(FunctionImportNameTextBox.Text)
                    && baseFunction.LocalName != null
                    && false == string.IsNullOrEmpty(baseFunction.LocalName.Value))
                {
                    FunctionImportNameTextBox.Text = ModelHelper.GetUniqueName(
                        typeof(FunctionImport), _container, baseFunction.LocalName.Value);
                }
            }

            FunctionImportComposableCheckBox.IsEnabled = _composableFunctionImportFeatureState.IsEnabled();
        }

        private void OnDoWork(object sender, DoWorkEventArgs e)
        {
            IDataSchemaProcedure storedProc = null;
            _openedDbConnection = Connection;
            if (_openedDbConnection != null)
            {
                DataSchemaServer server = new DataSchemaServer(_openedDbConnection);
                Function function = (Function)e.Argument;
                storedProc = server.GetProcedureOrFunction(function.DatabaseSchemaName, function.DatabaseFunctionName);
            }
            e.Result = storedProc;
        }

        private void OnWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _isWorkerRunning = false;

            SetDoWorkState(false);

            try
            {
                if (e.Error != null)
                {
                    if (CriticalException.IsCriticalException(e.Error))
                    {
                        throw e.Error;
                    }
                    var errMsgWithInnerExceptions = VsUtils.ConstructInnerExceptionErrorMessage(e.Error);
                    var errMsg = string.Format(
                        CultureInfo.CurrentCulture, DialogsResource.NewFunctionImportDialog_GetColumnInfoException,
                        e.Error.GetType().FullName, errMsgWithInnerExceptions);
                    DetectErrorLabel.Text = errMsg;
                    ReturnTypeShapeTabControl.SelectedItem = DetectErrorTabPage;
                }
                else if (!e.Cancelled)
                {
                    if (e.Result is not IDataSchemaProcedure schemaProcedure)
                    {
                        Debug.Assert(
                            Function != null,
                            "this.Function should not be null.");
                        var sprocName = (Function == null ? string.Empty : (DatabaseObject.CreateFromFunction(Function).ToString()));
                        DetectErrorLabel.Text = string.Format(
                            CultureInfo.CurrentCulture, DialogsResource.NewFunctionImportDialog_CouldNotFindStoredProcedure, sprocName);
                        ReturnTypeShapeTabControl.SelectedItem = DetectErrorTabPage;
                    }
                    else
                    {
                        if (schemaProcedure.Columns != null)
                        {
                            if (UpdateComplexTypeList(schemaProcedure.Columns))
                            {
                                Schema = schemaProcedure;
                                UpdateReturnTypeListState(IsResultTypeAvailable);
                                if (ReturnTypeShapeListView.Items.Count > 0)
                                {
                                    ReturnTypeShapeListView.SelectedIndex = 0;
                                }
                            }
                            else
                            {
                                DetectErrorLabel.Text = DialogsResource.NewFunctionImportDialog_NoColumnsReturned;
                                ReturnTypeShapeTabControl.SelectedItem = DetectErrorTabPage;
                            }
                        }
                    }
                }
            }
            finally
            {
                _openedDbConnection?.Close();
                _openedDbConnection = null;
            }

            GetResultColumnsCompletedEventStorage?.Invoke(this, EventArgs.Empty);
        }

        private IVsDataConnection Connection
        {
            get
            {
                IVsDataConnection connection = null;

                if (ConnectionString != null)
                {
                    var designTimeConnectionString = ConnectionString.GetDesignTimeProviderConnectionString(CurrentProject);
                    var provider = ConnectionString.Provider;
                    IVsDataConnectionManager dcm = (IVsDataConnectionManager)Services.ServiceProvider.GetService(typeof(IVsDataConnectionManager));
                    connection = dcm.GetConnection(provider, designTimeConnectionString, false);
                }

                return connection;
            }
        }

        private ConnectionManager.ConnectionString ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    if (CurrentProject != null && _container.LocalName != null)
                    {
                        _connectionString = ConnectionManager.GetConnectionStringObject(CurrentProject, _container.LocalName.Value);
                    }
                }
                return _connectionString;
            }
        }

        private Project CurrentProject
        {
            get
            {
                if (_currentProject == null)
                {
                    var documentPath = _container.Artifact.Uri.LocalPath;
                    _currentProject = VSHelpers.GetProjectForDocument(documentPath, PackageManager.Package);
                }
                return _currentProject;
            }
        }

        private void InitializeSupportedFeatures()
        {
            _complexTypeFeatureState = FeatureState.VisibleButDisabled;
            _composableFunctionImportFeatureState = FeatureState.VisibleButDisabled;
            _getColumnInformationFeatureState = FeatureState.VisibleAndEnabled;

            if (_container == null)
            {
                Debug.Fail("_container should be non-null");
            }
            else if (_container.Artifact == null)
            {
                Debug.Fail("_container.Artifact should be non-null");
            }
            else if (_container.Artifact.SchemaVersion == null)
            {
                Debug.Fail("_container.Artifact.SchemaVersion should be non-null");
            }
            else
            {
                var schemaVersion = _container.Artifact.SchemaVersion;
                _complexTypeFeatureState = EdmFeatureManager.GetFunctionImportReturningComplexTypeFeatureState(schemaVersion);
                _composableFunctionImportFeatureState = EdmFeatureManager.GetComposableFunctionImportFeatureState(schemaVersion);
                _getColumnInformationFeatureState = EdmFeatureManager.GetFunctionImportColumnInformationFeatureState(_container.Artifact);
                _sortedEdmPrimitiveTypes = ModelHelper.AllPrimitiveTypesSorted(schemaVersion);
            }
        }

        private void PopulateStoreProcedureList(ICollection<Function> functions)
        {
            StoredProcComboBox.Items.Clear();

            if (functions != null)
            {
                var functionsToList = from function in functions
                                      where (function.IsComposable.Value == (FunctionImportComposableCheckBox.IsChecked == true))
                                      select function;

                if (FunctionImportComposableCheckBox.IsChecked == true)
                {
                    Debug.Assert(functionsToList.All(f => f.IsComposable.Value), "Unexpected non-composable function.");
                    functionsToList = functionsToList.Where(f => f.ReturnType.Value == null);
                }

                foreach (var function in functionsToList)
                {
                    StoredProcComboBox.Items.Add(function);
                }
            }
        }

        private void SetComplexTypeReturnComboBoxStyle(bool isEditable)
        {
            ComplexTypeReturnComboBox.IsEditable = isEditable;
        }

        private void SetCreateNewComplexTypeButtonProperties()
        {
            CreateNewComplexTypeButton.IsEnabled = (Schema != null) && _complexTypeFeatureState.IsEnabled();
        }

        private void LaunchBackgroundProcessToRetrieveColumnsSchema()
        {
            if (CurrentProject != null
                && ConnectionString != null
                && Function != null)
            {
                _isWorkerRunning = true;
                SetDoWorkState(true);

                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = false;
                bw.WorkerSupportsCancellation = false;
                bw.DoWork += OnDoWork;
                bw.RunWorkerCompleted += OnWorkCompleted;
                bw.RunWorkerAsync(Function);
            }
        }

        #endregion
    }

    /// <summary>
    /// Data item for the return type columns ListView.
    /// </summary>
    internal class ReturnTypeColumnItem
    {
        public string Action { get; set; }
        public string Name { get; set; }
        public string EdmType { get; set; }
        public string DbType { get; set; }
        public string Nullable { get; set; }
        public string MaxLength { get; set; }
        public string Precision { get; set; }
        public string Scale { get; set; }
    }
}
