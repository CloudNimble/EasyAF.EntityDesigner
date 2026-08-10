// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using XmlDesignerBaseResources = Microsoft.Data.Tools.XmlDesignerBase.Resources;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    internal partial class NewEntityDialog : DialogWindow
    {
        private readonly ConceptualEntityModel _model;
        private bool _needsValidation;

        #region Test support

        private static event EventHandler DialogActivatedTestEventStorage;

        internal static event EventHandler DialogActivatedTestEvent
        {
            add { DialogActivatedTestEventStorage += value; }
            remove { DialogActivatedTestEventStorage -= value; }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            DialogActivatedTestEventStorage?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        internal NewEntityDialog(ConceptualEntityModel model)
        {
            Debug.Assert(model != null, "model should not be null");
            _model = model;

            InitializeComponent();
            this.HasHelpButton = false;

            KeyPropertyCheckBox.IsChecked = true;

            foreach (var primitiveType in ModelHelper.AllPrimitiveTypesSorted(_model.Artifact.SchemaVersion))
            {
                PropertyTypeComboBox.Items.Add(primitiveType);
            }
            PropertyTypeComboBox.SelectedItem = ModelConstants.Int32PropertyType;

            BaseTypeComboBox.Items.Add(XmlDesignerBaseResources.NoneDisplayValueUsedForUX);
            foreach (var entityType in model.EntityTypes())
            {
                BaseTypeComboBox.Items.Add(entityType);
            }
            if (BaseTypeComboBox.Items.Count > 0)
            {
                BaseTypeComboBox.SelectedIndex = 0;
            }

            EntityNameTextBox.Text = ModelHelper.GetUniqueNameWithNumber(
                typeof(EntityType), model, Model.Resources.Model_DefaultEntityTypeName);
            PropertyNameTextBox.Text = Model.Resources.Model_IdPropertyName;
        }

        internal string EntityName => EntityNameTextBox.Text;

        internal string EntitySetName => EntitySetTextBox.Text;

        internal bool CreateKeyProperty => KeyPropertyCheckBox.IsEnabled && KeyPropertyCheckBox.IsChecked == true;

        internal string KeyPropertyName => PropertyNameTextBox.Text;

        internal string KeyPropertyType => PropertyTypeComboBox.SelectedItem as string;

        internal ConceptualEntityType BaseEntityType
        {
            get
            {
                var cet = BaseTypeComboBox.SelectedItem as ConceptualEntityType;
#if DEBUG
                var et = BaseTypeComboBox.SelectedItem as EntityType;
                Debug.Assert(et != null ? cet != null : true, "EntityType is not ConceptualEntityType");
#endif
                return cet;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_needsValidation)
            {
                _needsValidation = false;

                if (!EscherAttributeContentValidator.IsValidCsdlEntityTypeName(EntityNameTextBox.Text))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewEntityDialog_InvalidEntityNameMsg);
                    e.Cancel = true;
                    EntityNameTextBox.Focus();
                }
                else
                {
                    if (!ModelHelper.IsUniqueName(typeof(EntityType), _model, EntityNameTextBox.Text, false, out string msg))
                    {
                        VsUtils.ShowErrorDialog(DialogsResource.NewEntityDialog_EnsureUniqueNameMsg);
                        e.Cancel = true;
                        EntityNameTextBox.Focus();
                        return;
                    }

                    if (EntitySetTextBox.IsEnabled)
                    {
                        if (!EscherAttributeContentValidator.IsValidCsdlEntitySetName(EntitySetName))
                        {
                            VsUtils.ShowErrorDialog(DialogsResource.NewEntityDialog_InvalidEntitySetMsg);
                            e.Cancel = true;
                            EntitySetTextBox.Focus();
                            return;
                        }

                        if (!ModelHelper.IsUniqueName(typeof(EntitySet), _model.FirstEntityContainer, EntitySetName, false, out msg))
                        {
                            VsUtils.ShowErrorDialog(DialogsResource.NewEntityDialog_EnsureUniqueSetNameMsg);
                            e.Cancel = true;
                            EntitySetTextBox.Focus();
                            return;
                        }
                    }

                    if (PropertyNameTextBox.IsEnabled)
                    {
                        if (!EscherAttributeContentValidator.IsValidCsdlPropertyName(PropertyNameTextBox.Text))
                        {
                            VsUtils.ShowErrorDialog(DialogsResource.NewEntityDialog_InvalidKeyPropertyNameMsg);
                            e.Cancel = true;
                            PropertyNameTextBox.Focus();
                            return;
                        }
                        else if (PropertyNameTextBox.Text.Equals(EntityName, StringComparison.Ordinal))
                        {
                            VsUtils.ShowErrorDialog(DialogsResource.SameEntityAndPropertyNameMsg);
                            e.Cancel = true;
                            PropertyNameTextBox.Focus();
                            return;
                        }
                    }
                }
            }
        }

        private void BaseTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettingsFromGui();
        }

        private void EntityNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSettingsFromGui();
        }

        private void UpdateSettingsFromGui()
        {
            if (!EscherAttributeContentValidator.IsValidCsdlEntityTypeName(EntityNameTextBox.Text))
            {
                BaseTypeComboBox.IsEnabled = false;
                EntitySetTextBox.IsEnabled = false;
                KeyPropertyGroupBox.IsEnabled = false;
            }
            else
            {
                KeyPropertyGroupBox.IsEnabled = true;
                BaseTypeComboBox.IsEnabled = true;

                if (BaseEntityType == null)
                {
                    EntitySetTextBox.IsEnabled = true;
                    KeyPropertyGroupBox.IsEnabled = true;
                    var proposedEntitySetName = ModelHelper.ConstructProposedEntitySetName(_model.Artifact, EntityName);
                    EntitySetTextBox.Text = ModelHelper.GetUniqueName(typeof(EntitySet), _model.FirstEntityContainer, proposedEntitySetName);
                    KeyPropertyCheckBox.IsChecked = true;
                }
                else
                {
                    KeyPropertyCheckBox.IsChecked = false;
                    EntitySetTextBox.IsEnabled = false;
                    EntitySetTextBox.Text = BaseEntityType.EntitySet.LocalName.Value;
                    KeyPropertyGroupBox.IsEnabled = false;
                }
            }
        }

        private void KeyPropertyCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Guard against event firing during XAML initialization
            if (PropertyNameTextBox is null)
            {
                return;
            }

            var isChecked = KeyPropertyCheckBox.IsChecked == true;
            PropertyNameTextBox.IsEnabled = isChecked;
            PropertyNameLabel.IsEnabled = isChecked;
            PropertyTypeComboBox.IsEnabled = isChecked;
            PropertyTypeLabel.IsEnabled = isChecked;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _needsValidation = true;
            DialogResult = true;
        }
    }
}
