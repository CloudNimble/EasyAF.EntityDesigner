// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Diagrams.Layout;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.Controls;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Shell;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.ContextMenu;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.Data.Entity.Package;

namespace Microsoft.VisualStudio.Data.Entity.Package
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
        private MenuCommandDefinition _advancedLayoutCommand;
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
                () => (CurrentDiagram as EntityDesignerSurface)?.ZoomToFit(),
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
                () => (CurrentDiagram as EntityDesignerSurface)?.ExpandAllEntityTypeShapes(),
                "Expand all entity shapes"));

            // Add Collapse All command
            _floatingZoomControl.Commands.Add(new MenuCommandDefinition(
                "CollapseAll",
                "Collapse All",
                KnownMonikers.CollapseAll,
                () => (CurrentDiagram as EntityDesignerSurface)?.CollapseAllEntityTypeShapes(),
                "Collapse all entity shapes"));

            // Add separator before zoom/layout commands
            _floatingZoomControl.Commands.Add(MenuSeparatorDefinition.Instance);

            // Add Layout command
            _floatingZoomControl.Commands.Add(new MenuCommandDefinition(
                "Layout",
                "Auto Layout",
                KnownMonikers.ShowAllFiles,
                () => (CurrentDiagram as EntityDesignerSurface)?.AutoLayoutDiagram(),
                "Auto-arrange entity layout"));

            // Chooses which engine the Layout command above runs. Sits next to it rather than with the grid
            // toggles because it changes what that button does rather than what the surface looks like.
            _advancedLayoutCommand = new MenuCommandDefinition
            {
                Id = "AdvancedLayout",
                Tooltip = "Advanced Layout",
                Icon = KnownMonikers.MagicWand,
                IsToggle = true,
                IsChecked = false
            };
            _advancedLayoutCommand.PropertyChanged += AdvancedLayoutCommand_PropertyChanged;

            _floatingZoomControl.Commands.Add(_advancedLayoutCommand);

            // TEMPORARY. Lets the connector routing modes be compared on a live diagram while the layout engine is
            // being tuned. Remove once one of them is chosen; the shipping UI is meant to be the toggle alone.
            _floatingZoomControl.Commands.Add(CreateRoutingCommand());

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

            // The model diagram is not loaded yet at this point, so the commands start disabled and are enabled once
            // the designer is actually able to service them.
            UpdateCommandAvailability();

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
            // Re-evaluate before the guard below: this is the point at which the model diagram has typically finished
            // loading, and the commands need to be enabled once it has.
            UpdateCommandAvailability();

            // make sure that the Model Diagram has already been created or translated before persisting ZoomLevel
            if (CurrentDiagram is not EntityDesignerSurface diagram
                || DocData is not MicrosoftDataEntityDesignDocData docData
                || !docData.IsModelDiagramLoaded)
            {
                return;
            }

            // Sync grid command states on first zoom event (when diagram is loaded)
            SyncGridCommandStates(diagram);
            SyncAdvancedLayoutCommandState(diagram);

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
        ///     Gets a value indicating whether a designer instance is loaded and able to service commands.
        /// </summary>
        /// <remarks>
        ///     The floating zoom control is created with the view, which happens even for a model that never becomes
        ///     designer-safe. Until the model diagram is loaded there is nothing behind the commands to act on.
        /// </remarks>
        private bool IsDesignerAvailable
        {
            get
            {
                return CurrentDiagram is EntityDesignerSurface
                       && DocData is MicrosoftDataEntityDesignDocData docData
                       && docData.IsModelDiagramLoaded;
            }
        }

        /// <summary>
        ///     Subscribes to the doc data's model-diagram-loaded notification and evaluates availability immediately.
        /// </summary>
        /// <remarks>
        ///     The doc view's LoadView runs before the doc data's OnDocumentLoaded, so the model diagram is never
        ///     loaded yet at the point the view would naturally check. Evaluating immediately as well as on the event
        ///     covers a reload, where the diagram is already loaded by the time the view is rebuilt.
        /// </remarks>
        private void HookModelDiagramLoaded()
        {
            if (DocData is MicrosoftDataEntityDesignDocData docData)
            {
                docData.ModelDiagramLoaded -= ModelDiagramLoaded_Handler;
                docData.ModelDiagramLoaded += ModelDiagramLoaded_Handler;
            }

            UpdateCommandAvailability();
        }

        /// <summary>
        ///     Handles the doc data reporting that the model diagram finished loading.
        /// </summary>
        /// <param name="sender">The doc data raising the notification.</param>
        /// <param name="e">Unused.</param>
        private void ModelDiagramLoaded_Handler(object sender, EventArgs e)
        {
            UpdateCommandAvailability();
        }

        /// <summary>
        ///     Enables or disables the floating zoom control's commands to match whether a designer instance is loaded.
        /// </summary>
        /// <remarks>
        ///     Without this the commands stay enabled over a blank designer, and invoking one reaches code that
        ///     assumes a model diagram exists.
        /// </remarks>
        private void UpdateCommandAvailability()
        {
            if (_floatingZoomControl is null)
            {
                return;
            }

            var isAvailable = IsDesignerAvailable;

            // Record each part of the predicate, so a control that is unexpectedly disabled says which condition
            // was not met rather than leaving the next person to guess.
            VsUtils.LogToActivityLog(
                $"UpdateCommandAvailability: isAvailable={isAvailable}, "
                + $"diagramIsEntityDesignerSurface={CurrentDiagram is EntityDesignerSurface}, "
                + $"docDataIsEscherDocData={DocData is MicrosoftDataEntityDesignDocData}, "
                + $"isModelDiagramLoaded={(DocData as MicrosoftDataEntityDesignDocData)?.IsModelDiagramLoaded}");
            foreach (var command in _floatingZoomControl.Commands)
            {
                if (command is MenuCommandDefinition menuCommand)
                {
                    menuCommand.IsEnabled = isAvailable;
                }
            }

            _floatingZoomControl.IsEnabled = isAvailable;
        }

        /// <summary>
        ///     Syncs the grid toggle command states with the diagram's current settings.
        /// </summary>
        private void SyncGridCommandStates(EntityDesignerSurface diagram)
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

            if (CurrentDiagram is not EntityDesignerSurface diagram)
            {
                return;
            }

            diagram.ShowGrid = _showGridCommand.IsChecked;
            diagram.PersistShowGrid();
        }

        /// <summary>
        ///     Builds the temporary connector-routing picker.
        /// </summary>
        /// <remarks>
        ///     TEMPORARY, for comparing routing modes on a real diagram. Selecting a mode sets it on the engine and
        ///     immediately re-runs the layout, because the only reason to change it is to see the difference -
        ///     unlike the Advanced toggle, where re-arranging on click would discard the user's positions.
        /// </remarks>
        private MenuCommandDefinition CreateRoutingCommand()
        {
            var routing = new MenuCommandDefinition
            {
                Id = "ConnectorRouting",
                Label = "Routing",
                Tooltip = "Connector routing (temporary)",
                Children = []
            };

            foreach (var mode in (ConnectorRouting[])Enum.GetValues(typeof(ConnectorRouting)))
            {
                routing.Children.Add(
                    new MenuCommandDefinition
                    {
                        Id = $"ConnectorRouting.{mode}",
                        Label = mode.ToString(),
                        Tooltip = $"Route connectors: {mode}",
                        ExecuteAction = () => ApplyRouting(mode)
                    });
            }

            return routing;
        }

        /// <summary>
        ///     Sets the routing mode on the MSAGL engine and re-lays out the diagram.
        /// </summary>
        private void ApplyRouting(ConnectorRouting mode)
        {
            if (CurrentDiagram is not EntityDesignerSurface diagram
                || diagram.LayoutManager?.LayoutEngines is null)
            {
                return;
            }

            if (!diagram.LayoutManager.LayoutEngines.TryGetValue(MsAglLayoutEngine.EngineKey, out var engine)
                || engine is not MsAglLayoutEngine msagl)
            {
                return;
            }

            msagl.Routing = mode;

            // Only redraw when that engine is the one in effect; otherwise the choice is stored for when it is.
            if (ReferenceEquals(diagram.LayoutManager.Current, msagl))
            {
                diagram.AutoLayoutDiagram();
            }
        }

        /// <summary>
        ///     Handler for the Advanced Layout toggle, which selects the engine the Layout command runs.
        /// </summary>
        /// <remarks>
        ///     Toggling only changes which engine is current; it does not lay the diagram out. Re-arranging
        ///     everything the moment a toggle is clicked would throw away positions the user may have spent time
        ///     on, so the Layout button beside it stays the thing that moves shapes.
        /// </remarks>
        private void AdvancedLayoutCommand_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MenuCommandDefinition.IsChecked))
            {
                return;
            }

            if (CurrentDiagram is not EntityDesignerSurface diagram
                || diagram.LayoutManager is null)
            {
                return;
            }

            var key = _advancedLayoutCommand.IsChecked ? MsAglLayoutEngine.EngineKey : DslLayoutEngine.EngineKey;

            if (!diagram.LayoutManager.TrySetCurrent(key))
            {
                // The engine was not registered. Leave whatever is running in place and put the toggle back, so
                // the button never claims a mode the designer is not actually in.
                VsUtils.LogToActivityLog(
                    $"AdvancedLayoutCommand: no layout engine registered under '{key}'.",
                    __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING);

                SyncAdvancedLayoutCommandState(diagram);
            }
        }

        /// <summary>
        ///     Points the Advanced Layout toggle at whichever engine is actually current.
        /// </summary>
        private void SyncAdvancedLayoutCommandState(EntityDesignerSurface diagram)
        {
            if (_advancedLayoutCommand is null
                || diagram.LayoutManager?.Current is null)
            {
                return;
            }

            var isAdvanced = string.Equals(
                diagram.LayoutManager.Current.Key, MsAglLayoutEngine.EngineKey, StringComparison.OrdinalIgnoreCase);

            if (_advancedLayoutCommand.IsChecked == isAdvanced)
            {
                return;
            }

            // Unhook while loading the state, the same as the grid toggles, so reflecting what is already current
            // does not read as the user asking to change it.
            _advancedLayoutCommand.PropertyChanged -= AdvancedLayoutCommand_PropertyChanged;
            _advancedLayoutCommand.IsChecked = isAdvanced;
            _advancedLayoutCommand.PropertyChanged += AdvancedLayoutCommand_PropertyChanged;
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

            if (CurrentDiagram is not EntityDesignerSurface diagram)
            {
                return;
            }

            diagram.SnapToGrid = _snapToGridCommand.IsChecked;
            diagram.PersistSnapToGrid();
        }

    }
}
