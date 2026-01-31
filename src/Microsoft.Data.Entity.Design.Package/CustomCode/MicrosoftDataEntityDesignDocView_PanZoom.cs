// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.Data.Entity.Design.EntityDesigner.View;
using Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu;
using Microsoft.Data.Entity.Design.EntityDesigner.View.Controls;
using Microsoft.Data.Entity.Design.Model.Eventing;
using Microsoft.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Shell;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Data.Entity.Design.Package
{
    /// <summary>
    ///     This partial class adds the floating zoom control and context menu to the diagram canvas.
    /// </summary>
    internal partial class MicrosoftDataEntityDesignDocView
    {
        private readonly List<Action> _themeChangedActions = [];

        private DiagramSurfaceContextMenuService _contextMenuService;
        private FloatingZoomControl _floatingZoomControl;
        private ElementHost _floatingZoomHost;
        private MenuCommandDefinition _showGridCommand;
        private MenuCommandDefinition _snapToGridCommand;

        /// <summary>
        ///     Override the base class method to add the floating zoom control,
        ///     context menu, and theme support to the diagram view.
        /// </summary>
        public override VSDiagramView CreateDiagramView()
        {
            // Let the base class create and initialise the standard view
            var view = base.CreateDiagramView();
            Debug.Assert(view.DiagramClientView != null, "DiagramClientView was null");

            // Add handler for ZoomChanged event so we can persist
            // zoom level regardless of where the change came from
            view.DiagramClientView.ZoomChanged += DiagramClientView_ZoomChanged;

            // Standard view sometimes contains a phantom panel that interferes with controls
            var fantomPanel = view.Controls.OfType<Panel>().FirstOrDefault();
            if (fantomPanel != null)
            {
                view.Controls.Remove(fantomPanel);
            }

            var vscroll = view.Controls.OfType<VScrollBar>().FirstOrDefault();
            Debug.Assert(vscroll != null, "couldn't find the vertical scroll bar");

            var scrollbarWidth = vscroll.Width;

            // Set theme colors before returning new view.
            UpdateTheme(view);

            // Hookup event handler so that we can keep colors updated if user changes theme.
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

            // Initialize the Windows 11-style context menu for the diagram surface
            _contextMenuService = new DiagramSurfaceContextMenuService(this, view.DiagramClientView);

            // Add floating zoom control in the top-right corner
            _floatingZoomControl = new FloatingZoomControl();
            _floatingZoomControl.AttachToDiagramView(view);

            
            // Add Zoom to 100% command
            _floatingZoomControl.Commands.Add(new MenuCommandDefinition(
                "Zoom100",
                "Zoom to 100%",
                KnownMonikers.ViewBox,
                () => CurrentDesigner?.ZoomAtViewCenter(1),
                "Zoom to 100%"));

            // Add Zoom to Fit command
            _floatingZoomControl.Commands.Add(new MenuCommandDefinition(
                "ZoomToFit",
                "Zoom to Fit",
                KnownMonikers.FitToScreen,
                () => (CurrentDiagram as EntityDesignerDiagram)?.ZoomToFit(),
                "Zoom to fit all entities"));

            // Add separator before toggle commands
            _floatingZoomControl.Commands.Add(MenuSeparatorDefinition.Instance);

            // Create grid toggle commands
            _showGridCommand = new MenuCommandDefinition
            {
                Id = "ShowGrid",
                Tooltip = "Show Grid",
                Icon = KnownMonikers.Grid,
                IsToggle = true,
                IsChecked = false
            };
            _showGridCommand.PropertyChanged += ShowGridCommand_PropertyChanged;

            _snapToGridCommand = new MenuCommandDefinition
            {
                Id = "SnapToGrid",
                Tooltip = "Snap to Grid",
                Icon = KnownMonikers.SnapToGrid,
                IsToggle = true,
                IsChecked = false
            };
            _snapToGridCommand.PropertyChanged += SnapToGridCommand_PropertyChanged;

            // Add commands to the floating zoom control
            _floatingZoomControl.Commands.Add(_showGridCommand);
            _floatingZoomControl.Commands.Add(_snapToGridCommand);

            // Add separator before expand/collapse commands
            _floatingZoomControl.Commands.Add(MenuSeparatorDefinition.Instance);

            // Add Expand All command
            _floatingZoomControl.Commands.Add(new MenuCommandDefinition(
                "ExpandAll",
                "Expand All",
                KnownMonikers.ExpandAll,
                () => (CurrentDiagram as EntityDesignerDiagram)?.ExpandAllEntityTypeShapes(),
                "Expand all entity shapes"));

            // Add Collapse All command
            _floatingZoomControl.Commands.Add(new MenuCommandDefinition(
                "CollapseAll",
                "Collapse All",
                KnownMonikers.CollapseAll,
                () => (CurrentDiagram as EntityDesignerDiagram)?.CollapseAllEntityTypeShapes(),
                "Collapse all entity shapes"));

            // Add separator before zoom/layout commands
            _floatingZoomControl.Commands.Add(MenuSeparatorDefinition.Instance);

            // Add Layout command
            _floatingZoomControl.Commands.Add(new MenuCommandDefinition(
                "Layout",
                "Auto Layout",
                KnownMonikers.ShowAllFiles,
                () => (CurrentDiagram as EntityDesignerDiagram)?.AutoLayoutDiagram(),
                "Auto-arrange entity layout"));

            // Calculate margin based on scrollbar width
            var rightMargin = scrollbarWidth + 4;  // scrollbar width + small gap

            _floatingZoomHost = new ElementHost
            {
                Child = _floatingZoomControl,
                BackColor = Color.Transparent,
                AutoSize = true,
                Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Position in top-right corner with margin matching scrollbar
            _floatingZoomHost.Left = view.Width - _floatingZoomHost.Width - rightMargin - 10;
            _floatingZoomHost.Top = rightMargin - 10;  // Same distance from top as from right, adjusted up

            // Update position when view is resized
            view.ClientSizeChanged += (sender, e) =>
            {
                _floatingZoomHost.Left = view.Width - _floatingZoomHost.Width - rightMargin - 10;
            };

            view.Controls.Add(_floatingZoomHost);
            _floatingZoomHost.BringToFront();

            return view;
        }

        /// <summary>
        ///     Set colors, e.g. background and watermark,
        ///     and colorize icons according to the theme
        /// </summary>
        private void UpdateTheme(VSDiagramView view)
        {
            if (view.HasWatermark)
            {
                VSHelpers.AssignLinkLabelColor(view.Watermark);
            }

            view.BackColor = VSColorTheme.GetThemedColor(EnvironmentColors.ScrollBarBackgroundColorKey);

            foreach (var action in _themeChangedActions)
            {
                action();
            }

            view.Invalidate();
        }

        /// <summary>
        ///     Handle updates to VS theme
        /// </summary>
        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            UpdateTheme(CurrentDesigner);
        }

        /// <summary>
        ///     Handler for ZoomChanged event that will persist current zoom level when changed
        /// </summary>
        private void DiagramClientView_ZoomChanged(object sender, DiagramEventArgs e)
        {
            // make sure that the Model Diagram has already been created or translated before persisting ZoomLevel
            if (CurrentDiagram is not EntityDesignerDiagram diagram
                || DocData is not MicrosoftDataEntityDesignDocData docData
                || !docData.IsModelDiagramLoaded)
            {
                return;
            }

            // Sync grid command states on first zoom event (when diagram is loaded)
            SyncGridCommandStates(diagram);

            try
            {
                diagram.PersistZoomLevel();
            }
            catch (FileNotEditableException fileNotEditableException)
            {
                VsUtils.ShowErrorDialog(fileNotEditableException.Message);
            }
        }

        /// <summary>
        ///     Syncs the grid toggle command states with the diagram's current settings.
        /// </summary>
        private void SyncGridCommandStates(EntityDesignerDiagram diagram)
        {
            if (_showGridCommand != null && _showGridCommand.IsChecked != diagram.ShowGrid)
            {
                // Temporarily unhook the event to avoid persisting the state we're loading
                _showGridCommand.PropertyChanged -= ShowGridCommand_PropertyChanged;
                _showGridCommand.IsChecked = diagram.ShowGrid;
                _showGridCommand.PropertyChanged += ShowGridCommand_PropertyChanged;
            }

            if (_snapToGridCommand != null && _snapToGridCommand.IsChecked != diagram.SnapToGrid)
            {
                // Temporarily unhook the event to avoid persisting the state we're loading
                _snapToGridCommand.PropertyChanged -= SnapToGridCommand_PropertyChanged;
                _snapToGridCommand.IsChecked = diagram.SnapToGrid;
                _snapToGridCommand.PropertyChanged += SnapToGridCommand_PropertyChanged;
            }
        }

        /// <summary>
        ///     Handler for ShowGrid command's IsChecked property changes
        /// </summary>
        private void ShowGridCommand_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MenuCommandDefinition.IsChecked))
            {
                return;
            }

            if (CurrentDiagram is not EntityDesignerDiagram diagram)
            {
                return;
            }

            diagram.ShowGrid = _showGridCommand.IsChecked;
            diagram.PersistShowGrid();
        }

        /// <summary>
        ///     Handler for SnapToGrid command's IsChecked property changes
        /// </summary>
        private void SnapToGridCommand_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MenuCommandDefinition.IsChecked))
            {
                return;
            }

            if (CurrentDiagram is not EntityDesignerDiagram diagram)
            {
                return;
            }

            diagram.SnapToGrid = _snapToGridCommand.IsChecked;
            diagram.PersistSnapToGrid();
        }

    }
}
