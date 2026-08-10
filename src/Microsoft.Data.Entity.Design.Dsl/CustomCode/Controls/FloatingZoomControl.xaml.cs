// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Controls
{
    /// <summary>
    /// A floating zoom control bar that provides zoom in/out buttons, displays the current zoom level,
    /// and supports additional bindable command definitions.
    /// </summary>
    internal partial class FloatingZoomControl : UserControl
    {
        private DiagramView _diagramView;
        private bool _isUpdatingZoomText;

        /// <summary>
        /// Gets or sets the minimum zoom level (as a percentage).
        /// </summary>
        public int MinZoom { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum zoom level (as a percentage).
        /// </summary>
        public int MaxZoom { get; set; } = 400;

        /// <summary>
        /// Gets or sets the zoom step amount (as a percentage).
        /// </summary>
        public int ZoomStep { get; set; } = 10;

        #region Dependency Properties

        /// <summary>
        /// Identifies the Commands dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandsProperty =
            DependencyProperty.Register(
                nameof(Commands),
                typeof(ObservableCollection<object>),
                typeof(FloatingZoomControl),
                new PropertyMetadata(null, OnCommandsChanged));

        /// <summary>
        /// Gets or sets the command definitions to display in the control.
        /// Can contain MenuCommandDefinition or MenuSeparatorDefinition items.
        /// </summary>
        public ObservableCollection<object> Commands
        {
            get => (ObservableCollection<object>)GetValue(CommandsProperty);
            set => SetValue(CommandsProperty, value);
        }

        private static void OnCommandsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FloatingZoomControl)d;

            if (e.OldValue is ObservableCollection<object> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnCommandsCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<object> newCollection)
            {
                newCollection.CollectionChanged += control.OnCommandsCollectionChanged;
            }

            control.UpdateCommandsSeparatorVisibility();
        }

        private void OnCommandsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCommandsSeparatorVisibility();
        }

        private void UpdateCommandsSeparatorVisibility()
        {
            if (CommandsSeparator != null)
            {
                CommandsSeparator.Visibility = Commands != null && Commands.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        #endregion

        /// <summary>
        /// Event raised when a command is executed.
        /// </summary>
        public event EventHandler<MenuCommandDefinition> CommandExecuted;

        public FloatingZoomControl()
        {
            InitializeComponent();

            // Initialize the commands collection
            Commands = new ObservableCollection<object>();

            // Subscribe to theme changes
            VSColorTheme.ThemeChanged += OnThemeChanged;
        }

        /// <summary>
        /// Attaches the zoom control to a diagram view.
        /// </summary>
        /// <param name="diagramView">The diagram view to control.</param>
        public void AttachToDiagramView(DiagramView diagramView)
        {
            // Detach from previous view if any
            if (_diagramView != null)
            {
                _diagramView.DiagramClientView.ZoomChanged -= OnZoomChanged;
            }

            _diagramView = diagramView;

            if (_diagramView != null)
            {
                _diagramView.DiagramClientView.ZoomChanged += OnZoomChanged;
                UpdateZoomDisplay();
            }
        }

        /// <summary>
        /// Updates the zoom percentage display.
        /// </summary>
        public void UpdateZoomDisplay()
        {
            if (_diagramView?.DiagramClientView != null && !_isUpdatingZoomText)
            {
                _isUpdatingZoomText = true;
                try
                {
                    int zoomPercent = (int)(_diagramView.DiagramClientView.ZoomFactor * 100);
                    ZoomPercentageTextBox.Text = $"{zoomPercent} %";

                    // Update button states
                    ZoomOutButton.IsEnabled = zoomPercent > MinZoom;
                    ZoomInButton.IsEnabled = zoomPercent < MaxZoom;
                }
                finally
                {
                    _isUpdatingZoomText = false;
                }
            }
        }

        private void OnZoomChanged(object sender, DiagramEventArgs e)
        {
            // Use Dispatcher to ensure we're on the UI thread
            // Fire and forget is intentional here - we don't need to await the UI update
            _ = Dispatcher.BeginInvoke(new Action(UpdateZoomDisplay));
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            // Resources are dynamic, so they update automatically
            InvalidateVisual();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_diagramView == null)
            {
                return;
            }

            int currentZoom = (int)(_diagramView.DiagramClientView.ZoomFactor * 100);
            int newZoom = Math.Max(MinZoom, currentZoom - ZoomStep);

            // Round to nearest step
            newZoom = (newZoom / ZoomStep) * ZoomStep;
            if (newZoom < MinZoom)
            {
                newZoom = MinZoom;
            }

            _diagramView.ZoomAtViewCenter((float)newZoom / 100);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (_diagramView == null)
            {
                return;
            }

            int currentZoom = (int)(_diagramView.DiagramClientView.ZoomFactor * 100);
            int newZoom = Math.Min(MaxZoom, currentZoom + ZoomStep);

            // Round to nearest step
            newZoom = ((newZoom + ZoomStep - 1) / ZoomStep) * ZoomStep;
            if (newZoom > MaxZoom)
            {
                newZoom = MaxZoom;
            }

            _diagramView.ZoomAtViewCenter((float)newZoom / 100);
        }

        private void ZoomPercentageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyZoomFromTextBox();
                e.Handled = true;

                // Move focus away from the textbox
                Keyboard.ClearFocus();
            }
            else if (e.Key == Key.Escape)
            {
                // Revert to current zoom
                UpdateZoomDisplay();
                e.Handled = true;

                // Move focus away from the textbox
                Keyboard.ClearFocus();
            }
        }

        private void ZoomPercentageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyZoomFromTextBox();
        }

        private void ApplyZoomFromTextBox()
        {
            if (_diagramView == null || _isUpdatingZoomText)
            {
                return;
            }

            string text = ZoomPercentageTextBox.Text.Trim();

            // Remove the % symbol if present
            text = text.Replace("%", "").Trim();

            if (int.TryParse(text, out int zoomPercent))
            {
                // Clamp to valid range
                zoomPercent = Math.Max(MinZoom, Math.Min(MaxZoom, zoomPercent));

                _diagramView.ZoomAtViewCenter((float)zoomPercent / 100);
            }
            else
            {
                // Invalid input, revert to current zoom
                UpdateZoomDisplay();
            }
        }

        private void Command_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MenuCommandDefinition commandDef)
            {
                // Try to execute the command directly
                if (!commandDef.Execute())
                {
                    // Raise event for external handling
                    CommandExecuted?.Invoke(this, commandDef);
                }
            }
        }

        private void ToggleCommand_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton && toggleButton.Tag is MenuCommandDefinition commandDef)
            {
                // The IsChecked binding will update the command definition
                // Execute the action if defined
                if (!commandDef.Execute())
                {
                    // Raise event for external handling
                    CommandExecuted?.Invoke(this, commandDef);
                }
            }
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        public void Dispose()
        {
            VSColorTheme.ThemeChanged -= OnThemeChanged;

            if (_diagramView != null)
            {
                _diagramView.DiagramClientView.ZoomChanged -= OnZoomChanged;
                _diagramView = null;
            }

            if (Commands != null)
            {
                Commands.CollectionChanged -= OnCommandsCollectionChanged;
            }
        }
    }

    /// <summary>
    /// Template selector that chooses between button, toggle button, and separator templates
    /// based on the type of the item.
    /// </summary>
    internal class CommandTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template for regular button commands.
        /// </summary>
        public DataTemplate ButtonTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for toggle button commands.
        /// </summary>
        public DataTemplate ToggleTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for separator items.
        /// </summary>
        public DataTemplate SeparatorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is MenuSeparatorDefinition)
            {
                return SeparatorTemplate;
            }

            if (item is MenuCommandDefinition commandDef)
            {
                return commandDef.IsToggle ? ToggleTemplate : ButtonTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
