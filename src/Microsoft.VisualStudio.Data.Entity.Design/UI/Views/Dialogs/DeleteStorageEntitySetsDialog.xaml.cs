// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.UI.Views.Dialogs
{
    internal partial class DeleteStorageEntitySetsDialog : DialogWindow
    {
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

        /// <summary>
        /// Gets the dialog result indicating which button was clicked.
        /// Yes = delete the entity sets, No = keep them, null = cancelled
        /// </summary>
        internal bool? UserChoice { get; private set; }

        internal DeleteStorageEntitySetsDialog(ICollection<StorageEntitySet> storageEntitySets)
        {
            InitializeComponent();
            this.HasHelpButton = false;

            // default result is to cancel
            UserChoice = null;

            // display StorageEntitySets ordered by name
            Debug.Assert(storageEntitySets != null, "Constructor requires a Collection of StorageEntitySets");
            if (storageEntitySets != null)
            {
                var entitySets = new List<StorageEntitySet>(storageEntitySets);
                entitySets.Sort(EFElement.EFElementDisplayNameComparison);
                foreach (var entitySet in entitySets)
                {
                    StorageEntitySetsListBox.Items.Add(entitySet);
                }
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = true;
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = false;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = null;
            DialogResult = false;
        }
    }
}
