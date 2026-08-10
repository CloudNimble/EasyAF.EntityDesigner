// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    internal partial class NewInheritanceDialog : DialogWindow
    {
        private readonly ISet<ConceptualEntityType> _entityTypes;

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

        internal NewInheritanceDialog(ConceptualEntityType baseType, IEnumerable<ConceptualEntityType> entityTypes)
        {
            InitializeComponent();
            this.HasHelpButton = false;

            _entityTypes = new SortedSet<ConceptualEntityType>(new EFNameableItemComparer());

            foreach (var et in entityTypes)
            {
                _entityTypes.Add(et);
            }

            foreach (var entityType in _entityTypes)
            {
                BaseEntityComboBox.Items.Add(entityType);
            }

            if (baseType != null)
            {
                BaseEntityComboBox.SelectedItem = baseType;
            }

            CheckOkButtonEnabled();
        }

        internal ConceptualEntityType BaseEntityType => BaseEntityComboBox.SelectedItem as ConceptualEntityType;

        internal EntityType DerivedEntityType => DerivedEntityComboBox.SelectedItem as EntityType;

        private void BaseEntityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DerivedEntityComboBox.Items.Clear();
            foreach (var entityType in _entityTypes)
            {
                if (entityType != BaseEntityType
                    && entityType.BaseType.Target == null)
                {
                    if (BaseEntityType == null
                        || !BaseEntityType.IsDerivedFrom(entityType))
                    {
                        DerivedEntityComboBox.Items.Add(entityType);
                    }
                }
            }
            if (DerivedEntityComboBox.Items.Count > 0)
            {
                DerivedEntityComboBox.SelectedIndex = 0;
            }

            CheckOkButtonEnabled();
        }

        private void CheckOkButtonEnabled()
        {
            OkButton.IsEnabled = BaseEntityType != null && DerivedEntityType != null;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
