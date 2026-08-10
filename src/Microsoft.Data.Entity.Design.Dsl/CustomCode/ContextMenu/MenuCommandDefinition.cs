// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu
{
    /// <summary>
    /// Defines a command that can be displayed in the context menu.
    /// Can be used for both top bar icon buttons and regular menu items.
    /// Supports hierarchical menus through the Children property.
    /// </summary>
    public class MenuCommandDefinition : INotifyPropertyChanged
    {
        private string _id;
        private string _label;
        private string _tooltip;
        private ImageMoniker _icon;
        private string _keyboardShortcut;
        private bool _isEnabled = true;
        private bool _isVisible = true;
        private bool _isChecked;
        private bool _isToggle;
        private ICommand _command;
        private object _commandParameter;
        private ObservableCollection<object> _children;
        private Action _executeAction;

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the unique identifier for this command.
        /// Used to identify the command when ActionExecuted is raised.
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the display label for the command.
        /// </summary>
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        /// <summary>
        /// Gets or sets the tooltip text shown on hover.
        /// </summary>
        public string Tooltip
        {
            get => _tooltip;
            set => SetProperty(ref _tooltip, value);
        }

        /// <summary>
        /// Gets or sets the icon moniker from the VS Image Catalog.
        /// </summary>
        public ImageMoniker Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// Gets or sets the keyboard shortcut display text (e.g., "Ctrl+S").
        /// Only displayed for regular menu items, not top bar buttons.
        /// </summary>
        public string KeyboardShortcut
        {
            get => _keyboardShortcut;
            set => SetProperty(ref _keyboardShortcut, value);
        }

        /// <summary>
        /// Gets or sets whether the command is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// Gets or sets whether the command is visible.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// Gets or sets whether this is a toggle command (displays as a toggle button).
        /// </summary>
        public bool IsToggle
        {
            get => _isToggle;
            set => SetProperty(ref _isToggle, value);
        }

        /// <summary>
        /// Gets or sets the checked state for toggle commands.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        /// <summary>
        /// Gets or sets the ICommand to execute when the item is clicked.
        /// If null, the ActionExecuted event is raised instead with the Id.
        /// </summary>
        public ICommand Command
        {
            get => _command;
            set => SetProperty(ref _command, value);
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the Command.
        /// </summary>
        public object CommandParameter
        {
            get => _commandParameter;
            set => SetProperty(ref _commandParameter, value);
        }

        /// <summary>
        /// Gets or sets the child menu items for hierarchical menus.
        /// Can contain MenuCommandDefinition or MenuSeparatorDefinition items.
        /// </summary>
        public ObservableCollection<object> Children
        {
            get => _children;
            set
            {
                if (SetProperty(ref _children, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChildren)));
                }
            }
        }

        /// <summary>
        /// Gets whether this command has child items (is a submenu).
        /// </summary>
        public bool HasChildren => _children != null && _children.Count > 0;

        /// <summary>
        /// Gets or sets the action to execute when this command is invoked.
        /// This provides a direct way to execute code without using ICommand or events.
        /// </summary>
        public Action ExecuteAction
        {
            get => _executeAction;
            set => SetProperty(ref _executeAction, value);
        }

        /// <summary>
        /// Executes this command. First tries ExecuteAction, then ICommand,
        /// otherwise returns false to indicate the ActionExecuted event should be raised.
        /// </summary>
        /// <returns>True if the command was executed, false if event handling is needed.</returns>
        public bool Execute()
        {
            if (_executeAction != null)
            {
                _executeAction();
                return true;
            }

            if (_command != null && _command.CanExecute(_commandParameter))
            {
                _command.Execute(_commandParameter);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new menu command definition.
        /// </summary>
        public MenuCommandDefinition()
        {
            _children = new ObservableCollection<object>();
            _children.CollectionChanged += (s, e) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChildren)));
            };
        }

        /// <summary>
        /// Creates a new menu command definition with the specified properties.
        /// </summary>
        public MenuCommandDefinition(string id, string label, ImageMoniker icon, string tooltip = null)
        {
            _id = id;
            _label = label;
            _icon = icon;
            _tooltip = tooltip ?? label;
            _children = new ObservableCollection<object>();
            _children.CollectionChanged += (s, e) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChildren)));
            };
        }

        /// <summary>
        /// Creates a new menu command definition with the specified properties and execute action.
        /// </summary>
        public MenuCommandDefinition(string id, string label, ImageMoniker icon, Action executeAction, string tooltip = null)
            : this(id, label, icon, tooltip)
        {
            _executeAction = executeAction;
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value changed.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
