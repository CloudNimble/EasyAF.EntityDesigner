// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu
{
    /// <summary>
    /// A panel that displays a submenu popup on hover after a system-defined delay.
    /// </summary>
    internal class SubMenuPanel : ContentControl
    {
        private Popup _submenuPopup;
        private DispatcherTimer _openTimer;
        private DispatcherTimer _closeTimer;
        private ItemsControl _submenuItemsControl;
        private Border _submenuBorder;
        private bool _isMouseOverSubmenu;

        /// <summary>
        /// Identifies the Children dependency property.
        /// </summary>
        public static readonly DependencyProperty ChildrenProperty =
            DependencyProperty.Register(
                nameof(Children),
                typeof(System.Collections.IEnumerable),
                typeof(SubMenuPanel),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the ItemTemplateSelector dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(ItemTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(SubMenuPanel),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the CommandDefinition dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandDefinitionProperty =
            DependencyProperty.Register(
                nameof(CommandDefinition),
                typeof(MenuCommandDefinition),
                typeof(SubMenuPanel),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the child items to display in the submenu.
        /// </summary>
        public System.Collections.IEnumerable Children
        {
            get => (System.Collections.IEnumerable)GetValue(ChildrenProperty);
            set => SetValue(ChildrenProperty, value);
        }

        /// <summary>
        /// Gets or sets the template selector for child items.
        /// </summary>
        public DataTemplateSelector ItemTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
            set => SetValue(ItemTemplateSelectorProperty, value);
        }

        /// <summary>
        /// Gets or sets the command definition this panel is associated with.
        /// </summary>
        public MenuCommandDefinition CommandDefinition
        {
            get => (MenuCommandDefinition)GetValue(CommandDefinitionProperty);
            set => SetValue(CommandDefinitionProperty, value);
        }

        /// <summary>
        /// Event raised when a menu item in the submenu is clicked.
        /// </summary>
        public event EventHandler<MenuCommandDefinition> SubMenuItemClicked;

        public SubMenuPanel()
        {
            // Get system menu show delay (default is 400ms)
            int delay = SystemParameters.MenuShowDelay;

            _openTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(delay)
            };
            _openTimer.Tick += OnOpenTimerTick;

            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(delay)
            };
            _closeTimer.Tick += OnCloseTimerTick;

            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _closeTimer.Stop();

            var commandDef = CommandDefinition;
            if (commandDef != null && commandDef.HasChildren)
            {
                _openTimer.Start();
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _openTimer.Stop();

            // Only start close timer if mouse is not over the submenu
            // Use a short delay to allow mouse to transition to submenu
            if (_submenuPopup != null && _submenuPopup.IsOpen && !_isMouseOverSubmenu)
            {
                _closeTimer.Start();
            }
        }

        private void OnOpenTimerTick(object sender, EventArgs e)
        {
            _openTimer.Stop();
            ShowSubmenu();
        }

        private void OnCloseTimerTick(object sender, EventArgs e)
        {
            _closeTimer.Stop();
            HideSubmenu();
        }

        private void ShowSubmenu()
        {
            var commandDef = CommandDefinition;
            if (commandDef == null || !commandDef.HasChildren)
            {
                return;
            }

            EnsureSubmenuCreated();

            _submenuItemsControl.ItemsSource = commandDef.Children;

            // Use the ItemTemplateSelector property if set, otherwise find it from resources
            var selector = ItemTemplateSelector;
            if (selector == null)
            {
                selector = TryFindResource("MenuItemTemplateSelector") as DataTemplateSelector;
            }
            _submenuItemsControl.ItemTemplateSelector = selector;

            // Position the popup
            PositionSubmenu();

            _submenuPopup.IsOpen = true;
        }

        private void HideSubmenu()
        {
            if (_submenuPopup != null)
            {
                _submenuPopup.IsOpen = false;
            }
        }

        private void EnsureSubmenuCreated()
        {
            if (_submenuPopup != null)
            {
                return;
            }

            // Create the submenu items control
            _submenuItemsControl = new ItemsControl
            {
                Margin = new Thickness(0, 2, 0, 4)
            };

            // Create the submenu border with VS theming
            _submenuBorder = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("MenuBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("MenuBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4),
                Child = _submenuItemsControl,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = 0.3,
                    Direction = 270
                }
            };

            // Copy essential resources to the border so templates can find them
            // (Popup is in a separate visual tree, so it can't access parent resources via StaticResource)
            CopyResourceToBorder(_submenuBorder, "BoolToVisibilityConverter");
            CopyResourceToBorder(_submenuBorder, "NullToCollapsedConverter");
            CopyResourceToBorder(_submenuBorder, "CountToVisibilityConverter");
            CopyResourceToBorder(_submenuBorder, "FirstItemToCollapsedConverter");
            CopyResourceToBorder(_submenuBorder, "MenuBackgroundBrush");
            CopyResourceToBorder(_submenuBorder, "MenuBorderBrush");
            CopyResourceToBorder(_submenuBorder, "MenuTextBrush");
            CopyResourceToBorder(_submenuBorder, "MenuTextDisabledBrush");
            CopyResourceToBorder(_submenuBorder, "MenuSeparatorBrush");
            CopyResourceToBorder(_submenuBorder, "MenuHighlightBrush");
            CopyResourceToBorder(_submenuBorder, "MenuHighlightBorderBrush");
            CopyResourceToBorder(_submenuBorder, "ShortcutTextBrush");
            CopyResourceToBorder(_submenuBorder, "IconButtonStyle");
            CopyResourceToBorder(_submenuBorder, "MenuItemStyle");
            CopyResourceToBorder(_submenuBorder, "VerticalSeparatorStyle");
            CopyResourceToBorder(_submenuBorder, "HorizontalSeparatorStyle");
            CopyResourceToBorder(_submenuBorder, "MenuItemTemplate");
            CopyResourceToBorder(_submenuBorder, "MenuSeparatorTemplate");
            CopyResourceToBorder(_submenuBorder, "MenuItemTemplateSelector");

            // Set image theming on the border
            Microsoft.VisualStudio.PlatformUI.ImageThemingUtilities.SetImageBackgroundColor(
                _submenuBorder,
                (System.Windows.Media.Color)FindResource(
                    Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowBackgroundColorKey));

            _submenuPopup = new Popup
            {
                Child = _submenuBorder,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                StaysOpen = true,  // We manage closing ourselves via timers
                Placement = PlacementMode.Custom,
                CustomPopupPlacementCallback = PositionSubmenuCallback
            };

            _submenuPopup.Closed += OnSubmenuClosed;

            // Handle mouse enter/leave on submenu to manage closing
            _submenuBorder.MouseEnter += OnSubmenuMouseEnter;
            _submenuBorder.MouseLeave += OnSubmenuMouseLeave;

            // Handle item clicks in submenu
            _submenuItemsControl.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnSubmenuItemClick));
        }

        private void OnSubmenuClosed(object sender, EventArgs e)
        {
            _closeTimer.Stop();
            _isMouseOverSubmenu = false;
        }

        private void OnSubmenuMouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseOverSubmenu = true;
            _closeTimer.Stop();
        }

        private void OnSubmenuMouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseOverSubmenu = false;

            // Check if mouse went back to the parent item
            if (IsMouseOver)
            {
                // Mouse is over parent, don't close
                return;
            }

            _closeTimer.Start();
        }

        private void OnSubmenuItemClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button button && button.Tag is MenuCommandDefinition clickedCommand)
            {
                // If this item has children, don't close - let it open its own submenu
                if (clickedCommand.HasChildren)
                {
                    return;
                }

                // Mark as handled to prevent double execution from event bubbling
                e.Handled = true;

                // Raise the SubMenuItemClicked event
                SubMenuItemClicked?.Invoke(this, clickedCommand);

                // Close the submenu
                HideSubmenu();
            }
        }

        private void PositionSubmenu()
        {
            if (_submenuPopup == null)
            {
                return;
            }

            _submenuPopup.PlacementTarget = this;
        }

        private CustomPopupPlacement[] PositionSubmenuCallback(Size popupSize, Size targetSize, Point offset)
        {
            // Get screen bounds
            var screenBounds = System.Windows.Forms.Screen.FromPoint(
                new System.Drawing.Point((int)offset.X, (int)offset.Y)).WorkingArea;

            // Get the position of this control on screen
            var controlPosition = PointToScreen(new Point(0, 0));

            // Calculate right-side position
            double rightX = targetSize.Width - 4; // Slight overlap
            double topY = -4; // Align with top of parent item

            // Check if there's enough space on the right
            bool fitsOnRight = controlPosition.X + rightX + popupSize.Width <= screenBounds.Right;

            // If not enough space on right, try left
            double leftX = -popupSize.Width + 4; // Slight overlap

            var placements = new CustomPopupPlacement[2];

            if (fitsOnRight)
            {
                placements[0] = new CustomPopupPlacement(new Point(rightX, topY), PopupPrimaryAxis.Horizontal);
                placements[1] = new CustomPopupPlacement(new Point(leftX, topY), PopupPrimaryAxis.Horizontal);
            }
            else
            {
                placements[0] = new CustomPopupPlacement(new Point(leftX, topY), PopupPrimaryAxis.Horizontal);
                placements[1] = new CustomPopupPlacement(new Point(rightX, topY), PopupPrimaryAxis.Horizontal);
            }

            return placements;
        }

        /// <summary>
        /// Copies a resource from the parent visual tree to the border's resources.
        /// </summary>
        private void CopyResourceToBorder(Border border, string resourceKey)
        {
            var resource = TryFindResource(resourceKey);
            if (resource != null && !border.Resources.Contains(resourceKey))
            {
                border.Resources.Add(resourceKey, resource);
            }
        }

        /// <summary>
        /// Clean up timers.
        /// </summary>
        public void Dispose()
        {
            _openTimer?.Stop();
            _closeTimer?.Stop();

            if (_submenuPopup != null)
            {
                _submenuPopup.IsOpen = false;
            }
        }
    }
}
