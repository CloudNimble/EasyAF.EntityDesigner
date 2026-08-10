// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu
{
    /// <summary>
    /// A Windows 11-style context menu for the Entity Designer diagram surface.
    /// Features a bindable icon button bar at the top and traditional menu items below.
    /// </summary>
    internal partial class DiagramSurfaceContextMenu : UserControl
    {
        private Popup _popup;
        private EntityDesignerDiagram _diagram;
        private Action _closeCallback;
        private bool _isExecutingCommand;

        #region Dependency Properties

        /// <summary>
        /// Identifies the TopBarCommands dependency property.
        /// </summary>
        public static readonly DependencyProperty TopBarCommandsProperty =
            DependencyProperty.Register(
                nameof(TopBarCommands),
                typeof(ObservableCollection<MenuCommandDefinition>),
                typeof(DiagramSurfaceContextMenu),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the MenuItems dependency property.
        /// </summary>
        public static readonly DependencyProperty MenuItemsProperty =
            DependencyProperty.Register(
                nameof(MenuItems),
                typeof(ObservableCollection<object>),
                typeof(DiagramSurfaceContextMenu),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the collection of commands displayed in the top icon button bar.
        /// </summary>
        public ObservableCollection<MenuCommandDefinition> TopBarCommands
        {
            get => (ObservableCollection<MenuCommandDefinition>)GetValue(TopBarCommandsProperty);
            set => SetValue(TopBarCommandsProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of menu items (MenuCommandDefinition or MenuSeparatorDefinition).
        /// </summary>
        public ObservableCollection<object> MenuItems
        {
            get => (ObservableCollection<object>)GetValue(MenuItemsProperty);
            set => SetValue(MenuItemsProperty, value);
        }

        #endregion

        /// <summary>
        /// Event raised when a menu action is executed.
        /// </summary>
        internal event EventHandler<MenuActionEventArgs> ActionExecuted;

        public DiagramSurfaceContextMenu()
        {
            InitializeComponent();

            // Set DataContext to self for bindings
            DataContext = this;

            // Initialize default collections
            TopBarCommands = new ObservableCollection<MenuCommandDefinition>();
            MenuItems = new ObservableCollection<object>();

            // Subscribe to theme changes to update colors
            VSColorTheme.ThemeChanged += OnThemeChanged;
        }

        /// <summary>
        /// Shows the context menu at the specified screen position.
        /// </summary>
        /// <param name="diagram">The diagram associated with this menu.</param>
        /// <param name="screenPosition">The screen coordinates where the menu should appear.</param>
        /// <param name="closeCallback">Optional callback invoked when the menu closes.</param>
        internal void Show(EntityDesignerDiagram diagram, Point screenPosition, Action closeCallback = null)
        {
            _diagram = diagram;
            _closeCallback = closeCallback;

            // Create popup if needed
            if (_popup == null)
            {
                _popup = new Popup
                {
                    Child = this,
                    AllowsTransparency = true,
                    PopupAnimation = PopupAnimation.Fade,
                    StaysOpen = false,
                    Placement = PlacementMode.AbsolutePoint
                };

                _popup.Closed += OnPopupClosed;
            }

            // Position and show
            _popup.HorizontalOffset = screenPosition.X;
            _popup.VerticalOffset = screenPosition.Y;
            _popup.IsOpen = true;

            // Focus for keyboard navigation
            this.Focus();
        }

        /// <summary>
        /// Closes the context menu.
        /// </summary>
        internal void Close()
        {
            if (_popup != null && _popup.IsOpen)
            {
                _popup.IsOpen = false;
            }
        }

        /// <summary>
        /// Gets the currently associated diagram.
        /// </summary>
        internal EntityDesignerDiagram Diagram => _diagram;

        private void OnPopupClosed(object sender, EventArgs e)
        {
            _closeCallback?.Invoke();
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            // Resources are dynamic, so they update automatically
            // But we can force a visual refresh if needed
            InvalidateVisual();
        }

        /// <summary>
        /// Handles click events from top bar buttons.
        /// </summary>
        private void TopBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MenuCommandDefinition commandDef)
            {
                ExecuteCommand(commandDef);
            }
        }

        /// <summary>
        /// Handles click events from menu item buttons.
        /// </summary>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MenuCommandDefinition commandDef)
            {
                // If this item has children, don't execute - let the submenu handle it
                if (commandDef.HasChildren)
                {
                    return;
                }

                // Mark as handled to prevent double execution
                e.Handled = true;

                ExecuteCommand(commandDef);
            }
        }

        /// <summary>
        /// Handles click events from submenu items.
        /// </summary>
        private void SubMenuPanel_ItemClicked(object sender, MenuCommandDefinition commandDef)
        {
            ExecuteCommand(commandDef);
        }

        private void ExecuteCommand(MenuCommandDefinition commandDef)
        {
            if (commandDef == null)
            {
                return;
            }

            // Guard against double execution
            if (_isExecutingCommand)
            {
                return;
            }

            _isExecutingCommand = true;

            // Close the menu FIRST, before executing the command
            // This prevents focus conflicts with modal dialogs
            Close();

            try
            {
                // Try to execute the command directly (ExecuteAction or ICommand)
                if (!commandDef.Execute())
                {
                    // Fall back to raising the ActionExecuted event with the command ID
                    ActionExecuted?.Invoke(this, new MenuActionEventArgs(commandDef.Id));
                }
            }
            finally
            {
                _isExecutingCommand = false;
            }
        }

        #region Keyboard Navigation

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up resources when the control is unloaded.
        /// </summary>
        internal void Dispose()
        {
            VSColorTheme.ThemeChanged -= OnThemeChanged;

            if (_popup != null)
            {
                _popup.Closed -= OnPopupClosed;
                _popup.IsOpen = false;
                _popup = null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for menu action events.
    /// </summary>
    internal class MenuActionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the ID of the command that was executed.
        /// </summary>
        public string ActionName { get; }

        public MenuActionEventArgs(string actionName)
        {
            ActionName = actionName;
        }
    }
}
