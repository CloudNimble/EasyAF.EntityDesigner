// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    internal partial class ComplexTypePickerDialog : DialogWindow
    {
        internal ComplexTypePickerDialog(ConceptualEntityModel cModel)
        {
            InitializeComponent();
            this.HasHelpButton = false;

            Debug.Assert(cModel != null, "Please specify ConceptualEntityModel");
            if (cModel != null)
            {
                var complexTypes = new List<ComplexType>(cModel.ComplexTypes());
                complexTypes.Sort(EFElement.EFElementDisplayNameComparison);
                foreach (var complexType in complexTypes)
                {
                    ComplexTypesListBox.Items.Add(complexType);
                }
            }

            UpdateOkButtonState();
        }

        /// <summary>
        /// Use this constructor if you want to remove a ComplexType (for example currently selected) from the list
        /// </summary>
        internal ComplexTypePickerDialog(ConceptualEntityModel cModel, ComplexType complexTypeToRemove)
            : this(cModel)
        {
            Debug.Assert(complexTypeToRemove != null, "Null ComplexType passed");
            if (complexTypeToRemove != null)
            {
                ComplexTypesListBox.Items.Remove(complexTypeToRemove);
            }
        }

        internal ComplexType ComplexType => ComplexTypesListBox.SelectedItem as ComplexType;

        private void UpdateOkButtonState()
        {
            OkButton.IsEnabled = ComplexType != null;
        }

        private void ComplexTypesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateOkButtonState();
        }

        private void ComplexTypesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ComplexType != null)
            {
                DialogResult = true;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
