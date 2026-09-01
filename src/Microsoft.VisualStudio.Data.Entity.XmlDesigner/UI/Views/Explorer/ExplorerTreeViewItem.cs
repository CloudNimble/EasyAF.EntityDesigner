// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.Commands;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.Views.Explorer
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class ExplorerTreeViewItem : TreeViewItem
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public static readonly DependencyProperty IndentProperty = DependencyProperty.Register(
            "Indent", typeof(double), typeof(ExplorerTreeViewItem));

        static ExplorerTreeViewItem()
        {
            CommandManager.RegisterClassInputBinding(
                typeof(ExplorerTreeViewItem),
                new MouseBinding(WorkspaceCommands.Activate, new MouseGesture(MouseAction.LeftDoubleClick)));
            CommandManager.RegisterClassInputBinding(
                typeof(ExplorerTreeViewItem),
                new KeyBinding(WorkspaceCommands.Activate, new KeyGesture(Key.Return)));

            // register left-click binding to allow rename mode for "slow-double-click"
            CommandManager.RegisterClassInputBinding(
                typeof(ExplorerTreeViewItem),
                new MouseBinding(WorkspaceCommands.PutInRenameMode, new MouseGesture(MouseAction.LeftClick)));
        }

        private static readonly Dictionary<string, object> _iconCache = [];

        // Automation (and accessibility) clients call this method
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ExplorerTreeViewItemAutomationPeer(this);
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public double Indent
        {
            get { return (double)GetValue(IndentProperty); }
            set { SetValue(IndentProperty, value); }
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public object Icon
        {
            get
            {
                object result = null;
                var viewModelElement = GetViewModelElement();
                if (null != viewModelElement)
                {
                    var resourceKey = viewModelElement.ExplorerImageResourceKeyName;
                    if (string.IsNullOrEmpty(resourceKey))
                    {
                        Debug.Assert(false, "Resource key is null or empty for view model element named " + viewModelElement.Name);
                    }
                    else
                    {
                        if (!_iconCache.TryGetValue(resourceKey, out result))
                        {
                            try
                            {
                                result = FindResource(resourceKey);
                                if (result != null)
                                {
                                    _iconCache.Add(resourceKey, result);
                                }
                            }
                            catch (ResourceReferenceKeyNotFoundException)
                            {
                                // do nothing - just Assert
                                Debug.Assert(false, "Could not find resource with key " + resourceKey);
                            }
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            ExplorerTreeViewItem treeViewItem = new ExplorerTreeViewItem();
            treeViewItem.Indent = Indent + 19;
            return treeViewItem;
        }

        internal ExplorerEFElement GetViewModelElement()
        {
            return DataContext as ExplorerEFElement;
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="e">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            // Code below handles the logic to determine which item that will get selected and focus if one of the child is deleted.
            // Skip if the treeviewitem is not selected.
            if (e.Action == NotifyCollectionChangedAction.Remove && IsSelected)
            {
                var setKeyboardFocus = IsKeyboardFocusWithin;
                TreeViewItem itemToBeSetFocusOn = this;

                // If a child item is deleted, select its previous sibling. If there is no previous sibling, then select the parent.
                if (e.OldStartingIndex > 0)
                {
                    // There is no guarantee that the Child UI Elements have been created yet; so we check the item container generator status.
                    if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        TreeViewItem item = ItemContainerGenerator.ContainerFromIndex(e.OldStartingIndex - 1) as TreeViewItem;
                        Debug.Assert(item != null, "Could not get previous sibling of the deleted item");
                        if (item != null)
                        {
                            item.IsSelected = true;
                            itemToBeSetFocusOn = item;
                        }
                    }
                }

                if (setKeyboardFocus)
                {
                    Keyboard.Focus(itemToBeSetFocusOn);
                }
            }
        }
    }

}
