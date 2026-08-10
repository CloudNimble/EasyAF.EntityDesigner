// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Infrastructure.Pluralization;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    internal partial class NewAssociationDialog : DialogWindow
    {
        private bool _needsValidation;
        private readonly bool _foreignKeysSupported;
        private readonly IPluralizationService _pluralizationService;

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

        internal NewAssociationDialog(IEnumerable<EntityType> entityTypes, EntityType entity1, EntityType entity2)
        {
            Debug.Assert(entity1 != null && entity2 != null, "both entity1 and entity2 should be non-null");

            // Ensure _foreignKeysSupported is initialized before we initialize UI components.
            _foreignKeysSupported =
                EdmFeatureManager.GetForeignKeysInModelFeatureState(entity1.Artifact.SchemaVersion)
                    .IsEnabled();

            // pluralization service is based on English only for Dev10
            var pluralize = ModelHelper.GetDesignerPropertyValueFromArtifactAsBool(
                OptionsDesignerInfo.ElementName,
                OptionsDesignerInfo.AttributeEnablePluralization, OptionsDesignerInfo.EnablePluralizationDefault, entity1.Artifact);
            if (pluralize)
            {
                _pluralizationService = DependencyResolver.GetService<IPluralizationService>();
            }

            InitializeComponent();
            this.HasHelpButton = false;

            foreach (var entityType in entityTypes)
            {
                Entity1ComboBox.Items.Add(entityType);
                Entity2ComboBox.Items.Add(entityType);
            }

            Multiplicity1ComboBox.Items.Add(new MultiplicityComboBoxItem(Design.Resources.PropertyWindow_Value_MultiplicityOne, ModelConstants.Multiplicity_One));
            Multiplicity1ComboBox.Items.Add(new MultiplicityComboBoxItem(Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, ModelConstants.Multiplicity_ZeroOrOne));
            Multiplicity1ComboBox.Items.Add(new MultiplicityComboBoxItem(Design.Resources.PropertyWindow_Value_MultiplicityMany, ModelConstants.Multiplicity_Many));

            Multiplicity2ComboBox.Items.Add(new MultiplicityComboBoxItem(Design.Resources.PropertyWindow_Value_MultiplicityOne, ModelConstants.Multiplicity_One));
            Multiplicity2ComboBox.Items.Add(new MultiplicityComboBoxItem(Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, ModelConstants.Multiplicity_ZeroOrOne));
            Multiplicity2ComboBox.Items.Add(new MultiplicityComboBoxItem(Design.Resources.PropertyWindow_Value_MultiplicityMany, ModelConstants.Multiplicity_Many));

            // Set values before adding event handlers
            Multiplicity1ComboBox.SelectedIndex = 0;
            Multiplicity2ComboBox.SelectedIndex = 2;
            Entity1ComboBox.SelectedItem = entity1;
            Entity2ComboBox.SelectedItem = entity2;

            // Now add event handlers
            Multiplicity1ComboBox.SelectionChanged += Multiplicity1ComboBox_SelectionChanged;
            Multiplicity2ComboBox.SelectionChanged += Multiplicity2ComboBox_SelectionChanged;

            // Update calculated fields
            UpdateAssociationName();
            UpdateEnd1NavigationPropertyName();
            UpdateEnd2NavigationPropertyName();
            UpdateExplanationText();
            UpdateCreateForeignKeysCheckBox();
        }

        internal string AssociationName => AssociationNameTextBox.Text;

        internal ConceptualEntityType End1Entity
        {
            get
            {
                var cet = Entity1ComboBox.SelectedItem as ConceptualEntityType;
                Debug.Assert(Entity1ComboBox.SelectedItem is EntityType ? cet != null : true, "EntityType is not ConceptualEntityType");
                return cet;
            }
        }

        internal ConceptualEntityType End2Entity
        {
            get
            {
                var cet = Entity2ComboBox.SelectedItem as ConceptualEntityType;
                Debug.Assert(Entity2ComboBox.SelectedItem is EntityType ? cet != null : true, "EntityType is not ConceptualEntityType");
                return cet;
            }
        }

        internal string End1Multiplicity => (Multiplicity1ComboBox.SelectedItem as MultiplicityComboBoxItem)?.Value;

        internal string End2Multiplicity => (Multiplicity2ComboBox.SelectedItem as MultiplicityComboBoxItem)?.Value;

        internal string End1MultiplicityText => (Multiplicity1ComboBox.SelectedItem as MultiplicityComboBoxItem)?.ToString() ?? string.Empty;

        internal string End2MultiplicityText => (Multiplicity2ComboBox.SelectedItem as MultiplicityComboBoxItem)?.ToString() ?? string.Empty;

        internal string End1NavigationPropertyName => NavigationProperty1Checkbox.IsChecked == true ? NavigationProperty1TextBox.Text : string.Empty;

        internal string End2NavigationPropertyName => NavigationProperty2Checkbox.IsChecked == true ? NavigationProperty2TextBox.Text : string.Empty;

        internal bool CreateForeignKeyProperties => _foreignKeysSupported && CreateForeignKeysCheckBox.IsEnabled && CreateForeignKeysCheckBox.IsChecked == true;

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_needsValidation)
            {
                _needsValidation = false;

                if (!EscherAttributeContentValidator.IsValidCsdlAssociationName(AssociationName))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewAssociationDialog_InvalidAssociationNameMsg);
                    e.Cancel = true;
                    AssociationNameTextBox.Focus();
                }
                else if (NavigationProperty1Checkbox.IsChecked == true
                         && !EscherAttributeContentValidator.IsValidCsdlNavigationPropertyName(End1NavigationPropertyName))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewAssociationDialog_InvalidNavigationPropertyNameMsg);
                    e.Cancel = true;
                    NavigationProperty1TextBox.Focus();
                }
                else if (NavigationProperty2Checkbox.IsChecked == true
                         && !EscherAttributeContentValidator.IsValidCsdlNavigationPropertyName(End2NavigationPropertyName))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewAssociationDialog_InvalidNavigationPropertyNameMsg);
                    e.Cancel = true;
                    NavigationProperty2TextBox.Focus();
                }
                else if (!ModelHelper.IsUniqueName(typeof(Association), End1Entity.Parent, AssociationName, false, out string msg))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewAssociationDialog_EnsureUniqueNameMsg);
                    e.Cancel = true;
                    AssociationNameTextBox.Focus();
                }
                else if (NavigationProperty1Checkbox.IsChecked == true
                         && End1NavigationPropertyName.Equals(End1Entity.LocalName.Value, StringComparison.Ordinal))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.SameEntityAndPropertyNameMsg);
                    e.Cancel = true;
                    NavigationProperty1TextBox.Focus();
                }
                else if (NavigationProperty1Checkbox.IsChecked == true
                         && !ModelHelper.IsUniquePropertyName(End1Entity, End1NavigationPropertyName, true))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewAssociationDialog_EnsureUniquePropertyNameMsg);
                    e.Cancel = true;
                    NavigationProperty1TextBox.Focus();
                }
                else if (End2NavigationPropertyName.Equals(End2Entity.LocalName.Value, StringComparison.Ordinal))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.SameEntityAndPropertyNameMsg);
                    e.Cancel = true;
                    NavigationProperty2TextBox.Focus();
                }
                else if (NavigationProperty2Checkbox.IsChecked == true
                         && (!ModelHelper.IsUniquePropertyName(End2Entity, End2NavigationPropertyName, true)
                             || (End1Entity == End2Entity && NavigationProperty2Checkbox.IsChecked == true
                                 && End2NavigationPropertyName.Equals(End1NavigationPropertyName, StringComparison.Ordinal))))
                {
                    VsUtils.ShowErrorDialog(DialogsResource.NewAssociationDialog_EnsureUniquePropertyNameMsg);
                    e.Cancel = true;
                    NavigationProperty2TextBox.Focus();
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _needsValidation = true;
            DialogResult = true;
        }

        private void Entity1ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAssociationName();
            UpdateEnd1NavigationPropertyName();
            UpdateEnd2NavigationPropertyName();
        }

        private void Entity2ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAssociationName();
            UpdateEnd1NavigationPropertyName();
            UpdateEnd2NavigationPropertyName();
        }

        private void AssociationNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!EscherAttributeContentValidator.IsValidCsdlAssociationName(AssociationName))
            {
                End1GroupBox.IsEnabled = false;
                End2GroupBox.IsEnabled = false;
            }
            else
            {
                End1GroupBox.IsEnabled = true;
                End2GroupBox.IsEnabled = true;
            }
        }

        private void NavigationProperty1TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExplanationText();
        }

        private void NavigationProperty2TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExplanationText();
        }

        private void Multiplicity1ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEnd2NavigationPropertyName();
            UpdateExplanationText();
        }

        private void Multiplicity2ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEnd1NavigationPropertyName();
            UpdateExplanationText();
        }

        private void NavigationProperty1Checkbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Guard against event firing during XAML initialization
            if (NavigationProperty1TextBox is null)
            {
                return;
            }

            NavigationProperty1TextBox.IsEnabled = NavigationProperty1Checkbox.IsChecked == true;
        }

        private void NavigationProperty2Checkbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Guard against event firing during XAML initialization
            if (NavigationProperty2TextBox is null)
            {
                return;
            }

            NavigationProperty2TextBox.IsEnabled = NavigationProperty2Checkbox.IsChecked == true;
        }

        private void UpdateAssociationName()
        {
            TryGetPrincipalAndDependentEntities(out EntityType principal, out EntityType dependent);

            var principalName = principal?.LocalName.Value ?? End1Entity?.LocalName.Value;
            var dependentName = dependent?.LocalName.Value ?? End2Entity?.LocalName.Value;

            if (End1Entity != null)
            {
                AssociationNameTextBox.Text = ModelHelper.GetUniqueName(typeof(Association), End1Entity.Parent, principalName + dependentName);
            }
        }

        private void UpdateEnd1NavigationPropertyName()
        {
            if (End1Entity != null && End2Entity != null)
            {
                var namesToAvoid = new HashSet<string>();
                if (End1Entity.Equals(End2Entity))
                {
                    namesToAvoid.Add(NavigationProperty2TextBox.Text);
                }

                var proposedNavPropName = ModelHelper.ConstructProposedNavigationPropertyName(
                    _pluralizationService, End2Entity.LocalName.Value, End2Multiplicity);
                NavigationProperty1TextBox.Text = ModelHelper.GetUniqueConceptualPropertyName(proposedNavPropName, End1Entity, namesToAvoid);
            }
        }

        private void UpdateEnd2NavigationPropertyName()
        {
            if (End1Entity != null && End2Entity != null)
            {
                var namesToAvoid = new HashSet<string>();
                if (End1Entity.Equals(End2Entity))
                {
                    namesToAvoid.Add(NavigationProperty1TextBox.Text);
                }

                var proposedNavPropName = ModelHelper.ConstructProposedNavigationPropertyName(
                    _pluralizationService, End1Entity.LocalName.Value, End1Multiplicity);
                NavigationProperty2TextBox.Text = ModelHelper.GetUniqueConceptualPropertyName(proposedNavPropName, End2Entity, namesToAvoid);
            }
        }

        private static bool AreForeignKeysSupportedByCardinality(string end1, string end2)
        {
            // FKs are enabled when one end is "many" and the other end is not many
            var supported = false;

            if (string.Compare(end1, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture) == 0)
            {
                if (string.Compare(end2, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture) != 0)
                {
                    supported = true;
                }
            }
            else if (string.Compare(end2, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture) == 0)
            {
                if (string.Compare(end1, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture) != 0)
                {
                    supported = true;
                }
            }

            return supported;
        }

        private void TryGetPrincipalAndDependentEntities(out EntityType principal, out EntityType dependent)
        {
            principal = null;
            dependent = null;

            if (string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityOne, StringComparison.CurrentCulture)
                && string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityOne, StringComparison.CurrentCulture))
            {
                dependent = End2Entity;
                principal = End1Entity;
            }
            else if (string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, StringComparison.CurrentCulture)
                     && string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, StringComparison.CurrentCulture))
            {
                dependent = End2Entity;
                principal = End1Entity;
            }
            else if (string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, StringComparison.CurrentCulture)
                     && string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture))
            {
                dependent = End2Entity;
                principal = End1Entity;
            }
            else if (string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, StringComparison.CurrentCulture)
                     && string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture))
            {
                dependent = End1Entity;
                principal = End2Entity;
            }
            else
            {
                if (string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityOne, StringComparison.CurrentCulture)
                    && (string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, StringComparison.CurrentCulture)
                        || string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture)))
                {
                    dependent = End2Entity;
                    principal = End1Entity;
                }
                else if (string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityOne, StringComparison.CurrentCulture)
                         && (string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityZeroOrOne, StringComparison.CurrentCulture)
                             || string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityMany, StringComparison.CurrentCulture)))
                {
                    dependent = End1Entity;
                    principal = End2Entity;
                }
            }
        }

        private void UpdateCreateForeignKeysCheckBox()
        {
            var enableFKCheckbox = _foreignKeysSupported && AreForeignKeysSupportedByCardinality(End1MultiplicityText, End2MultiplicityText);

            if (!enableFKCheckbox)
            {
                CreateForeignKeysCheckBox.IsChecked = false;
                CreateForeignKeysCheckBox.IsEnabled = false;
            }
            else
            {
                CreateForeignKeysCheckBox.IsEnabled = true;
                TryGetPrincipalAndDependentEntities(out EntityType principal, out EntityType dependent);

                if (dependent != null)
                {
                    CreateForeignKeysCheckBox.Content = string.Format(
                        CultureInfo.CurrentCulture, DialogsResource.NewAssociationDialog_CreateForeignKeysLabel, dependent.LocalName.Value);
                }
                else
                {
                    CreateForeignKeysCheckBox.Content = DialogsResource.NewAssociationDialog_CreateForeignKeysLabel_Default;
                }
            }
        }

        private void UpdateExplanationText()
        {
            UpdateCreateForeignKeysCheckBox();

            if (End1Entity == null || End2Entity == null)
            {
                return;
            }

            string sentence1;
            string sentence2;
            var sentenceBase1 = !string.IsNullOrEmpty(End1NavigationPropertyName)
                                    ? DialogsResource.NewAssociationDialog_ExplanationText1
                                    : DialogsResource.NewAssociationDialog_ExplanationText1EmptyNavProp;
            var sentenceBase2 = !string.IsNullOrEmpty(End1NavigationPropertyName)
                                    ? DialogsResource.NewAssociationDialog_ExplanationText2
                                    : DialogsResource.NewAssociationDialog_ExplanationText2EmptyNavProp;

            sentence1 = string.Format(
                CultureInfo.CurrentCulture,
                string.Equals(End2MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityOne, StringComparison.CurrentCulture)
                    ? sentenceBase1
                    : sentenceBase2,
                End1Entity.LocalName.Value,
                End2MultiplicityText,
                End2Entity.LocalName.Value,
                End1NavigationPropertyName);

            sentenceBase1 = !string.IsNullOrEmpty(End2NavigationPropertyName)
                                ? DialogsResource.NewAssociationDialog_ExplanationText1
                                : DialogsResource.NewAssociationDialog_ExplanationText1EmptyNavProp;
            sentenceBase2 = !string.IsNullOrEmpty(End2NavigationPropertyName)
                                ? DialogsResource.NewAssociationDialog_ExplanationText2
                                : DialogsResource.NewAssociationDialog_ExplanationText2EmptyNavProp;

            sentence2 = string.Format(
                CultureInfo.CurrentCulture,
                string.Equals(End1MultiplicityText, Design.Resources.PropertyWindow_Value_MultiplicityOne, StringComparison.CurrentCulture)
                    ? sentenceBase1
                    : sentenceBase2,
                End2Entity.LocalName.Value,
                End1MultiplicityText,
                End1Entity.LocalName.Value,
                End2NavigationPropertyName);

            ExplanationTextBox.Text = sentence1 + "\r\n\r\n" + sentence2;
        }

        private class MultiplicityComboBoxItem
        {
            private readonly string _text;
            private readonly string _value;

            public MultiplicityComboBoxItem(string text, string value)
            {
                Debug.Assert(!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(value), "neither text nor value should be null or empty");
                _text = text;
                _value = value;
            }

            public override string ToString() => _text;

            public string Value => _value;
        }
    }
}
