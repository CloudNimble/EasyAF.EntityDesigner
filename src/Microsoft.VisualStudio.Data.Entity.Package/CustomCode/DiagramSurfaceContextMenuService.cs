// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.Data.Entity.Design.Diagrams.View;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.ContextMenu;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.Package;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.ContextMenu;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using Microsoft.VisualStudio.Data.Entity.Package;
using Microsoft.VisualStudio.Data.Entity.Package.Export;
using Microsoft.Data.Entity.Design.Diagrams.Rendering.Export;

namespace Microsoft.VisualStudio.Data.Entity.Package
{
    /// <summary>
    /// Identifies the type of element that was clicked in the diagram.
    /// </summary>
    internal enum DiagramHitTarget
    {
        /// <summary>Empty diagram surface (no element)</summary>
        Surface,
        /// <summary>An association connector</summary>
        Association,
        /// <summary>An entity type shape</summary>
        EntityType,
        /// <summary>An inheritance connector</summary>
        Inheritance,
        /// <summary>A scalar property within an entity</summary>
        ScalarProperty,
        /// <summary>A complex property within an entity</summary>
        ComplexProperty,
        /// <summary>A navigation property within an entity</summary>
        NavigationProperty,
        /// <summary>Unknown or unsupported element</summary>
        Unknown
    }

    /// <summary>
    /// Result of a hit test on the diagram.
    /// </summary>
    internal class DiagramHitResult
    {
        public DiagramHitTarget Target { get; set; }
        public ShapeElement Shape { get; set; }
        public object ModelElement { get; set; }
        /// <summary>The compartment containing the clicked property (if applicable)</summary>
        public ElementListCompartment Compartment { get; set; }
        /// <summary>The index of the clicked item within the compartment (if applicable)</summary>
        public int CompartmentItemIndex { get; set; } = -1;
    }

    /// <summary>
    /// Service that provides Windows 11-style context menus for the Entity Designer.
    /// Supports different menus for different element types (surface, association, entity, etc.).
    /// </summary>
    internal sealed class DiagramSurfaceContextMenuService : IDisposable
    {
        private readonly MicrosoftDataEntityDesignDocView _docView;
        private readonly DiagramClientView _diagramClientView;

        // Menus for different element types
        private DiagramSurfaceContextMenu _diagramSurfaceMenu;
        private DiagramSurfaceContextMenu _associationMenu;
        private DiagramSurfaceContextMenu _propertyMenu;
        private DiagramSurfaceContextMenu _navigationPropertyMenu;
        private DiagramSurfaceContextMenu _entityMenu;

        // Current association being operated on (for dynamic menu items)
        private AssociationConnector _currentAssociation;

        // Current property being operated on (for dynamic menu items)
        private DiagramHitResult _currentPropertyHit;

        // Current navigation property being operated on (for dynamic menu items)
        private DiagramHitResult _currentNavigationPropertyHit;

        // Current entity being operated on (for dynamic menu items)
        private EntityTypeShape _currentEntityShape;

        // Shared command references for diagram surface menu
        private MenuCommandDefinition _showDataTypesCommand;
        private MenuCommandDefinition _moveDiagramsToSeparateFileCommand;

        private bool _isDisposed;

        /// <summary>
        /// Creates a new context menu service for the specified diagram view.
        /// </summary>
        /// <param name="docView">The document view containing the diagram.</param>
        /// <param name="diagramClientView">The diagram client view to attach to.</param>
        public DiagramSurfaceContextMenuService(MicrosoftDataEntityDesignDocView docView, DiagramClientView diagramClientView)
        {
            _docView = docView ?? throw new ArgumentNullException(nameof(docView));
            _diagramClientView = diagramClientView ?? throw new ArgumentNullException(nameof(diagramClientView));
        }

        /// <summary>
        /// Performs a hit test at the specified position and returns what was clicked.
        /// </summary>
        /// <param name="mousePosition">The mouse position in world coordinates.</param>
        /// <returns>A DiagramHitResult indicating what was clicked.</returns>
        public DiagramHitResult HitTest(PointD mousePosition)
        {
            var result = new DiagramHitResult { Target = DiagramHitTarget.Surface };

            var diagram = _diagramClientView.Diagram;
            if (diagram == null)
            {
                return result;
            }

            // Perform hit test
            DiagramHitTestInfo hitTestInfo = new DiagramHitTestInfo(_diagramClientView);
            var hitSuccess = diagram.DoHitTest(mousePosition, hitTestInfo);

            if (!hitSuccess || hitTestInfo.HitDiagramItem == null)
            {
                return result; // Empty surface
            }

            var hitShape = hitTestInfo.HitDiagramItem.Shape;
            if (hitShape == null || hitShape == diagram)
            {
                return result; // Empty surface
            }

            // Check if we clicked on a property within a compartment
            var diagramItem = hitTestInfo.HitDiagramItem;
            if (hitShape is ElementListCompartment compartment && diagramItem.SubField is ListItemSubField listItemSubField)
            {
                var itemIndex = listItemSubField.Row;
                if (itemIndex >= 0 && itemIndex < compartment.Items.Count)
                {
                    var clickedItem = compartment.Items[itemIndex];
                    result.Compartment = compartment;
                    result.CompartmentItemIndex = itemIndex;
                    result.Shape = compartment.ParentShape;

                    if (clickedItem is Microsoft.Data.Entity.Design.Diagrams.ViewModel.ScalarProperty scalarProp)
                    {
                        result.Target = DiagramHitTarget.ScalarProperty;
                        result.ModelElement = scalarProp;
                        return result;
                    }
                    else if (clickedItem is Microsoft.Data.Entity.Design.Diagrams.ViewModel.ComplexProperty complexProp)
                    {
                        result.Target = DiagramHitTarget.ComplexProperty;
                        result.ModelElement = complexProp;
                        return result;
                    }
                    else if (clickedItem is Microsoft.Data.Entity.Design.Diagrams.ViewModel.NavigationProperty navProp)
                    {
                        result.Target = DiagramHitTarget.NavigationProperty;
                        result.ModelElement = navProp;
                        return result;
                    }
                }
            }

            // Determine the type of element clicked
            if (hitShape is AssociationConnector associationConnector)
            {
                result.Target = DiagramHitTarget.Association;
                result.Shape = associationConnector;
                result.ModelElement = associationConnector.ModelElement;
            }
            else if (hitShape is EntityTypeShape entityShape)
            {
                result.Target = DiagramHitTarget.EntityType;
                result.Shape = entityShape;
                result.ModelElement = entityShape.ModelElement;
            }
            else if (hitShape is InheritanceConnector inheritanceConnector)
            {
                result.Target = DiagramHitTarget.Inheritance;
                result.Shape = inheritanceConnector;
                result.ModelElement = inheritanceConnector.ModelElement;
            }
            else
            {
                result.Target = DiagramHitTarget.Unknown;
                result.Shape = hitShape;
            }

            return result;
        }

        /// <summary>
        /// Determines if the click location is on the diagram surface (empty space).
        /// </summary>
        public bool IsClickOnDiagramSurface(PointD mousePosition)
        {
            return HitTest(mousePosition).Target == DiagramHitTarget.Surface;
        }

        /// <summary>
        /// Shows the appropriate context menu based on what was clicked.
        /// </summary>
        /// <param name="diagram">The diagram.</param>
        /// <param name="mousePosition">The mouse position in world coordinates.</param>
        /// <returns>True if a context menu was shown, false otherwise.</returns>
        public bool ShowContextMenu(EntityDesignerSurface diagram, PointD mousePosition)
        {
            var hitResult = HitTest(mousePosition);

            switch (hitResult.Target)
            {
                case DiagramHitTarget.Surface:
                    ShowDiagramSurfaceContextMenu(diagram, mousePosition);
                    return true;

                case DiagramHitTarget.Association:
                    ShowAssociationContextMenu(diagram, hitResult.Shape as AssociationConnector, mousePosition);
                    return true;

                case DiagramHitTarget.ScalarProperty:
                case DiagramHitTarget.ComplexProperty:
                    ShowPropertyContextMenu(diagram, hitResult, mousePosition);
                    return true;

                case DiagramHitTarget.NavigationProperty:
                    ShowNavigationPropertyContextMenu(diagram, hitResult, mousePosition);
                    return true;

                case DiagramHitTarget.EntityType:
                    ShowEntityContextMenu(diagram, hitResult.Shape as EntityTypeShape, mousePosition);
                    return true;

                // TODO: Add support for other element types
                case DiagramHitTarget.Inheritance:
                default:
                    return false; // Fall back to default VS context menu
            }
        }

        /// <summary>
        /// Shows the diagram surface context menu at the specified position.
        /// </summary>
        private void ShowDiagramSurfaceContextMenu(EntityDesignerSurface diagram, PointD mousePosition)
        {
            // Create context menu if needed
            if (_diagramSurfaceMenu == null)
            {
                _diagramSurfaceMenu = new DiagramSurfaceContextMenu();
                _diagramSurfaceMenu.ActionExecuted += OnDiagramSurfaceMenuActionExecuted;
                PopulateDiagramSurfaceMenu();
            }

            // Update command states based on current diagram
            UpdateDiagramSurfaceCommandStates(diagram);

            // Show the menu
            ShowMenuAtPosition(_diagramSurfaceMenu, diagram, mousePosition);
        }

        /// <summary>
        /// Shows the association context menu at the specified position.
        /// </summary>
        private void ShowAssociationContextMenu(EntityDesignerSurface diagram, AssociationConnector connector, PointD mousePosition)
        {
            _currentAssociation = connector;

            // Create context menu if needed
            if (_associationMenu == null)
            {
                _associationMenu = new DiagramSurfaceContextMenu();
                _associationMenu.ActionExecuted += OnAssociationMenuActionExecuted;
            }

            // Populate the menu with dynamic items based on the association
            PopulateAssociationMenu(connector);

            // Show the menu
            ShowMenuAtPosition(_associationMenu, diagram, mousePosition);
        }

        /// <summary>
        /// Shows a context menu at the specified position.
        /// </summary>
        private void ShowMenuAtPosition(DiagramSurfaceContextMenu menu, EntityDesignerSurface diagram, PointD mousePosition)
        {
            // Convert world coordinates to screen coordinates
            var clientPoint = _diagramClientView.WorldToDevice(mousePosition);
            var screenPoint = _diagramClientView.PointToScreen(new System.Drawing.Point((int)clientPoint.X, (int)clientPoint.Y));
            var wpfScreenPoint = new Point(screenPoint.X, screenPoint.Y);

            menu.Show(diagram, wpfScreenPoint);
        }

        /// <summary>
        /// Shows the custom context menu at the specified position.
        /// </summary>
        /// <param name="diagram">The diagram.</param>
        /// <param name="mousePosition">The mouse position in world coordinates.</param>
        [Obsolete("Use ShowContextMenu instead")]
        public void ShowCustomContextMenu(EntityDesignerSurface diagram, PointD mousePosition)
        {
            ShowDiagramSurfaceContextMenu(diagram, mousePosition);
        }

        #region Diagram Surface Menu

        /// <summary>
        /// Populates the diagram surface context menu with commands.
        /// </summary>
        private void PopulateDiagramSurfaceMenu()
        {
            // Top bar commands - 5 icon buttons (64px each + 4 separators = 324px total)
            _diagramSurfaceMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "UpdateModelFromDatabase",
                "Update",
                KnownMonikers.DatabaseModelRefresh,
                "Update the model from the database"));

            _diagramSurfaceMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "GenerateDatabaseFromModel",
                "Generate",
                KnownMonikers.DatabaseScript,
                "Generate database scripts from the model"));

            _diagramSurfaceMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Export",
                "Export",
                KnownMonikers.SaveAs,
                "Export diagram as..."));

            _diagramSurfaceMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Validate",
                "Validate",
                KnownMonikers.ValidateModel,
                "Validate the model"));

            _diagramSurfaceMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "OpenXmlEditor",
                "XML",
                KnownMonikers.XMLFile,
                "Open the model in the XML editor"));

            // Add New submenu
            var addNewMenu = new MenuCommandDefinition(
                "AddNew",
                "Add New",
                KnownMonikers.AddItem,
                "Add new items to the model");

            addNewMenu.Children.Add(new MenuCommandDefinition(
                "AddEntity",
                "Entity...",
                KnownMonikers.AddEntity,
                "Add a new entity"));

            addNewMenu.Children.Add(new MenuCommandDefinition(
                "AddComplexType",
                "Complex Type",
                KnownMonikers.EntityContainer,
                "Add a new complex type"));

            addNewMenu.Children.Add(new MenuCommandDefinition(
                "AddEnumType",
                "Enum Type...",
                KnownMonikers.EnumerationPublic,
                "Add a new enum type"));

            addNewMenu.Children.Add(MenuSeparatorDefinition.Instance);

            addNewMenu.Children.Add(new MenuCommandDefinition(
                "AddAssociation",
                "Association...",
                KnownMonikers.AssociationRelationship,
                "Add a new association between entities"));

            addNewMenu.Children.Add(new MenuCommandDefinition(
                "AddInheritance",
                "Inheritance...",
                KnownMonikers.Inheritance,
                "Add an inheritance relationship"));

            addNewMenu.Children.Add(MenuSeparatorDefinition.Instance);

            addNewMenu.Children.Add(new MenuCommandDefinition(
                "AddFunctionImport",
                "Function Import...",
                KnownMonikers.Method,
                "Add a function import"));

            addNewMenu.Children.Add(new MenuCommandDefinition(
                "AddCodeGenerationItem",
                "Code Generation...",
                KnownMonikers.ModifyClass,
                "Add a code generation template"));

            _diagramSurfaceMenu.MenuItems.Add(addNewMenu);

            // Show Data Types toggle
            _showDataTypesCommand = new MenuCommandDefinition(
                "ShowDataTypes",
                "Show Data Types",
                KnownMonikers.Type,
                "Show data types alongside property names")
            {
                IsToggle = true,
                IsChecked = false
            };
            _diagramSurfaceMenu.MenuItems.Add(_showDataTypesCommand);

            // Move Diagrams to Separate File. This shares the group above rather than getting its own: the item is
            // hidden once the diagrams already live in a .edmx.diagram file, and a dedicated group would leave two
            // adjacent separators behind whenever that happens.
            _moveDiagramsToSeparateFileCommand = new MenuCommandDefinition(
                "MoveDiagramsToSeparateFile",
                "Move Diagrams to Separate File",
                KnownMonikers.MoveToFolder,
                "Move the diagram layout out of the EDMX and into a separate .edmx.diagram file")
            {
                IsVisible = false
            };
            _diagramSurfaceMenu.MenuItems.Add(_moveDiagramsToSeparateFileCommand);

            // Separator
            _diagramSurfaceMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Select All
            _diagramSurfaceMenu.MenuItems.Add(new MenuCommandDefinition(
                "SelectAll",
                "Select All",
                KnownMonikers.SelectAll,
                "Select all entities")
            {
                KeyboardShortcut = "Ctrl+A"
            });

            // Separator
            _diagramSurfaceMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Show group
            _diagramSurfaceMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowMappingDetails",
                "Show Mapping Details",
                KnownMonikers.SchemaProperty,
                "Show mapping details window"));

            _diagramSurfaceMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowModelBrowser",
                "Show Model Browser",
                KnownMonikers.ShowDiagramPane,
                "Show model browser window"));

            // Properties (standard VS command)
            _diagramSurfaceMenu.MenuItems.Add(new MenuCommandDefinition(
                "Properties",
                "Show Properties",
                KnownMonikers.Property,
                "Show the Properties window")
            {
                KeyboardShortcut = "F4"
            });

        }

        /// <summary>
        /// Updates command states for the diagram surface menu based on the current diagram state.
        /// </summary>
        private void UpdateDiagramSurfaceCommandStates(EntityDesignerSurface diagram)
        {
            if (diagram == null)
            {
                return;
            }

            int entityCount = diagram.ModelElement?.EntityTypes?.Count ?? 0;

            // Update top bar command states
            foreach (var cmd in _diagramSurfaceMenu.TopBarCommands)
            {
                if (cmd.Id == "AddAssociation")
                {
                    cmd.IsEnabled = entityCount >= 1;
                }
            }

            // Update menu item states recursively
            UpdateMenuItemStates(_diagramSurfaceMenu.MenuItems, diagram, entityCount);
        }

        private void UpdateMenuItemStates(System.Collections.ObjectModel.ObservableCollection<object> items, EntityDesignerSurface diagram, int entityCount)
        {
            foreach (var item in items)
            {
                if (item is MenuCommandDefinition cmd)
                {
                    switch (cmd.Id)
                    {
                        case "AddAssociation":
                            cmd.IsEnabled = entityCount >= 1;
                            break;

                        case "AddInheritance":
                            cmd.IsEnabled = entityCount >= 2;
                            break;
                    }

                    // Recursively update children
                    if (cmd.HasChildren)
                    {
                        UpdateMenuItemStates(cmd.Children, diagram, entityCount);
                    }
                }
            }

            // Update Show Data Types toggle state
            if (_showDataTypesCommand != null)
            {
                _showDataTypesCommand.IsChecked = diagram.DisplayNameAndType;
            }

            // Shares its eligibility rules with the Model Browser command so the two never disagree about whether the
            // move is on offer.
            if (_moveDiagramsToSeparateFileCommand != null)
            {
                var artifact = diagram.GetModel()?.EditingContext?.GetEFArtifactService()?.Artifact as EntityDesignArtifact;
                _moveDiagramsToSeparateFileCommand.IsVisible =
                    MicrosoftDataEntityDesignCommandSet.CanMoveDiagramsToSeparateFile(artifact, out _);
            }
        }

        private void OnDiagramSurfaceMenuActionExecuted(object sender, MenuActionEventArgs e)
        {
            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram == null)
            {
                return;
            }

            // Execute the appropriate command
            switch (e.ActionName)
            {
                // Add New commands
                case "AddEntity":
                    ExecuteAddEntity(diagram);
                    break;

                case "AddComplexType":
                    ExecuteAddComplexType(diagram);
                    break;

                case "AddEnumType":
                    ExecuteAddEnumType(diagram);
                    break;

                case "AddAssociation":
                    ExecuteAddAssociation(diagram);
                    break;

                case "AddInheritance":
                    ExecuteAddInheritance(diagram);
                    break;

                case "AddFunctionImport":
                    ExecuteAddFunctionImport(diagram);
                    break;

                // Diagram commands
                case "Layout":
                    ExecuteLayout(diagram);
                    break;

                case "Export":
                    ExecuteExport(diagram);
                    break;

                case "CollapseAll":
                    ExecuteCollapseAll(diagram);
                    break;

                case "ExpandAll":
                    ExecuteExpandAll(diagram);
                    break;

                // Zoom commands (ZoomToFit is still in the top bar)
                case "ZoomToFit":
                    ExecuteZoomToFit(diagram);
                    break;

                // Show Data Types toggle
                case "ShowDataTypes":
                    ExecuteToggleShowDataTypes(diagram);
                    break;

                case "MoveDiagramsToSeparateFile":
                    ExecuteMoveDiagramsToSeparateFile(diagram);
                    break;

                // Select All
                case "SelectAll":
                    ExecuteSelectAll(diagram);
                    break;

                // Show commands
                case "ShowMappingDetails":
                    ExecuteShowMappingDetails();
                    break;

                case "ShowModelBrowser":
                    ExecuteShowModelBrowser();
                    break;

                // Model/Database interaction commands
                case "UpdateModelFromDatabase":
                    ExecuteUpdateModelFromDatabase(diagram);
                    break;

                case "GenerateDatabaseFromModel":
                    ExecuteGenerateDatabaseFromModel(diagram);
                    break;

                case "AddCodeGenerationItem":
                    ExecuteAddCodeGenerationItem(diagram);
                    break;

                // Validate
                case "Validate":
                    ExecuteValidate(diagram);
                    break;

                // Open in XML Editor
                case "OpenXmlEditor":
                    ExecuteOpenXmlEditor(diagram);
                    break;

                // Properties
                case "Properties":
                    ExecuteProperties();
                    break;
            }
        }

        #endregion

        #region Association Menu

        /// <summary>
        /// Populates the association context menu with commands based on the selected association.
        /// </summary>
        private void PopulateAssociationMenu(AssociationConnector connector)
        {
            // Clear existing items (menu is rebuilt each time for dynamic content)
            _associationMenu.TopBarCommands.Clear();
            _associationMenu.MenuItems.Clear();

            var association = connector.ModelElement;
            if (association == null)
            {
                return;
            }

            // Top bar commands - 2 icon buttons
            _associationMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Rename",
                "Rename",
                KnownMonikers.Rename,
                "Rename this association"));

            _associationMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Delete",
                "Delete",
                KnownMonikers.DeleteListItem,
                "Delete this association from the model"));

            // Select commands - Entity 1 and Property 1
            var sourceEntity = association.SourceEntityType;
            var sourceNavProp = association.SourceNavigationProperty;

            if (sourceEntity != null)
            {
                _associationMenu.MenuItems.Add(new MenuCommandDefinition(
                    "SelectSourceEntity",
                    $"Select {sourceEntity.Name}",
                    KnownMonikers.Class,
                    $"Select the {sourceEntity.Name} entity"));
            }

            if (sourceNavProp != null)
            {
                _associationMenu.MenuItems.Add(new MenuCommandDefinition(
                    "SelectSourceProperty",
                    $"Select {sourceNavProp.Name}",
                    KnownMonikers.Property,
                    $"Select the {sourceNavProp.Name} navigation property"));
            }

            // Separator between the two ends
            _associationMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Select commands - Entity 2 and Property 2
            var targetEntity = association.TargetEntityType;
            var targetNavProp = association.TargetNavigationProperty;

            if (targetEntity != null)
            {
                _associationMenu.MenuItems.Add(new MenuCommandDefinition(
                    "SelectTargetEntity",
                    $"Select {targetEntity.Name}",
                    KnownMonikers.Class,
                    $"Select the {targetEntity.Name} entity"));
            }

            if (targetNavProp != null)
            {
                _associationMenu.MenuItems.Add(new MenuCommandDefinition(
                    "SelectTargetProperty",
                    $"Select {targetNavProp.Name}",
                    KnownMonikers.Property,
                    $"Select the {targetNavProp.Name} navigation property"));
            }

            // Separator before Show section
            _associationMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Show commands
            _associationMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInModelBrowser",
                "Show in Model Browser",
                KnownMonikers.ShowDiagramPane,
                "Show this association in the Model Browser"));

            _associationMenu.MenuItems.Add(new MenuCommandDefinition(
                "Properties",
                "Show Properties",
                KnownMonikers.Property,
                "Show the Properties window")
            {
                KeyboardShortcut = "F4"
            });
        }

        /// <summary>
        /// Handles menu action events for the association context menu.
        /// </summary>
        private void OnAssociationMenuActionExecuted(object sender, MenuActionEventArgs e)
        {
            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram == null || _currentAssociation == null)
            {
                return;
            }

            var association = _currentAssociation.ModelElement;

            switch (e.ActionName)
            {
                case "Rename":
                    ExecuteRenameAssociation(_currentAssociation);
                    break;

                case "Delete":
                    ExecuteDeleteAssociation(diagram, _currentAssociation);
                    break;

                case "SelectSourceEntity":
                    ExecuteSelectShape(diagram, association?.SourceEntityType);
                    break;

                case "SelectSourceProperty":
                    ExecuteSelectProperty(diagram, association?.SourceEntityType, association?.SourceNavigationProperty);
                    break;

                case "SelectTargetEntity":
                    ExecuteSelectShape(diagram, association?.TargetEntityType);
                    break;

                case "SelectTargetProperty":
                    ExecuteSelectProperty(diagram, association?.TargetEntityType, association?.TargetNavigationProperty);
                    break;

                case "ShowInModelBrowser":
                    ExecuteShowInModelBrowser(_currentAssociation);
                    break;

                case "Properties":
                    ExecuteProperties();
                    break;
            }
        }

        #endregion

        #region Property Menu

        /// <summary>
        /// Shows the property context menu at the specified position.
        /// </summary>
        private void ShowPropertyContextMenu(EntityDesignerSurface diagram, DiagramHitResult hitResult, PointD mousePosition)
        {
            _currentPropertyHit = hitResult;

            // Create context menu if needed
            if (_propertyMenu == null)
            {
                _propertyMenu = new DiagramSurfaceContextMenu();
                _propertyMenu.ActionExecuted += OnPropertyMenuActionExecuted;
            }

            // Populate the menu with dynamic items based on the property
            PopulatePropertyMenu(hitResult);

            // Show the menu
            ShowMenuAtPosition(_propertyMenu, diagram, mousePosition);
        }

        /// <summary>
        /// Populates the property context menu with commands based on the selected property.
        /// </summary>
        private void PopulatePropertyMenu(DiagramHitResult hitResult)
        {
            // Clear existing items (menu is rebuilt each time for dynamic content)
            _propertyMenu.TopBarCommands.Clear();
            _propertyMenu.MenuItems.Clear();

            var isScalarProperty = hitResult.Target == DiagramHitTarget.ScalarProperty;
            var scalarProperty = hitResult.ModelElement as Microsoft.Data.Entity.Design.Diagrams.ViewModel.ScalarProperty;

            // Top bar commands - Cut | Copy | Paste | Rename | Delete
            _propertyMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Cut",
                "Cut",
                KnownMonikers.Cut,
                "Cut this property")
            {
                KeyboardShortcut = "Ctrl+X"
            });

            _propertyMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Copy",
                "Copy",
                KnownMonikers.Copy,
                "Copy this property")
            {
                KeyboardShortcut = "Ctrl+C"
            });

            _propertyMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Paste",
                "Paste",
                KnownMonikers.Paste,
                "Paste property")
            {
                KeyboardShortcut = "Ctrl+V"
            });

            _propertyMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Rename",
                "Rename",
                KnownMonikers.Rename,
                "Rename this property"));

            _propertyMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Delete",
                "Delete",
                KnownMonikers.DeleteListItem,
                "Delete this property from the model"));

            // Entity Key toggle (only for scalar properties)
            if (isScalarProperty && scalarProperty != null)
            {
                _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                    "EntityKey",
                    "Entity Key",
                    default,
                    "Toggle whether this property is part of the entity key")
                {
                    IsToggle = true,
                    IsChecked = scalarProperty.EntityKey
                });

                _propertyMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);
            }

            // Move operations
            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveToTop",
                "Move to Top",
                KnownMonikers.GoToTop,
                "Move this property to the top")
            {
                KeyboardShortcut = "Alt+Home"
            });

            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveUp",
                "Move Up",
                KnownMonikers.MoveUp,
                "Move this property up")
            {
                KeyboardShortcut = "Alt+\u2191"
            });

            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveDown",
                "Move Down",
                KnownMonikers.MoveDown,
                "Move this property down")
            {
                KeyboardShortcut = "Alt+\u2193"
            });

            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveToBottom",
                "Move to Bottom",
                KnownMonikers.GoToBottom,
                "Move this property to the bottom")
            {
                KeyboardShortcut = "Alt+End"
            });

            // Move to New Complex Type (only for scalar properties)
            if (isScalarProperty)
            {
                _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                    "MoveToNewComplexType",
                    "Move to New Complex Type",
                    KnownMonikers.MoveClass,
                    "Move this property to a new complex type"));
            }

            _propertyMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Show commands
            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInTableMapping",
                "Show in Table Mapping",
                KnownMonikers.SchemaProperty,
                "Show this property in table mapping"));

            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInStoredProcedureMapping",
                "Show in Stored Procedure Mapping",
                KnownMonikers.StoredProcedure,
                "Show this property in stored procedure mapping"));

            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInModelBrowser",
                "Show in Model Browser",
                KnownMonikers.ShowDiagramPane,
                "Show this property in the Model Browser"));

            _propertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "Properties",
                "Show Properties",
                KnownMonikers.Property,
                "Show the Properties window")
            {
                KeyboardShortcut = "F4"
            });
        }

        /// <summary>
        /// Handles menu action events for the property context menu.
        /// </summary>
        private void OnPropertyMenuActionExecuted(object sender, MenuActionEventArgs e)
        {
            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram == null || _currentPropertyHit == null)
            {
                return;
            }

            switch (e.ActionName)
            {
                case "Cut":
                    ExecutePropertyCut();
                    break;

                case "Copy":
                    ExecutePropertyCopy();
                    break;

                case "Paste":
                    ExecutePropertyPaste();
                    break;

                case "Rename":
                    ExecutePropertyRename();
                    break;

                case "Delete":
                    ExecutePropertyDelete();
                    break;

                case "EntityKey":
                    ExecuteToggleEntityKey();
                    break;

                case "MoveUp":
                    ExecutePropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyUp);
                    break;

                case "MoveDown":
                    ExecutePropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyDown);
                    break;

                case "MoveToTop":
                    ExecutePropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyTop);
                    break;

                case "MoveToBottom":
                    ExecutePropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyBottom);
                    break;

                case "MoveToNewComplexType":
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.CreateComplexType);
                    break;

                case "ShowInTableMapping":
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.TableMappings);
                    break;

                case "ShowInStoredProcedureMapping":
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.SprocMappings);
                    break;

                case "ShowInModelBrowser":
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.ShowInEdmExplorer);
                    break;

                case "Properties":
                    ExecuteProperties();
                    break;
            }
        }

        #endregion

        #region Navigation Property Menu

        /// <summary>
        /// Shows the navigation property context menu at the specified position.
        /// </summary>
        private void ShowNavigationPropertyContextMenu(EntityDesignerSurface diagram, DiagramHitResult hitResult, PointD mousePosition)
        {
            _currentNavigationPropertyHit = hitResult;

            // Create context menu if needed
            if (_navigationPropertyMenu == null)
            {
                _navigationPropertyMenu = new DiagramSurfaceContextMenu();
                _navigationPropertyMenu.ActionExecuted += OnNavigationPropertyMenuActionExecuted;
            }

            // Populate the menu with dynamic items based on the navigation property
            PopulateNavigationPropertyMenu(hitResult);

            // Show the menu
            ShowMenuAtPosition(_navigationPropertyMenu, diagram, mousePosition);
        }

        /// <summary>
        /// Populates the navigation property context menu with commands.
        /// </summary>
        private void PopulateNavigationPropertyMenu(DiagramHitResult hitResult)
        {
            // Clear existing items (menu is rebuilt each time for dynamic content)
            _navigationPropertyMenu.TopBarCommands.Clear();
            _navigationPropertyMenu.MenuItems.Clear();

            // Top bar commands - same as Association: Rename | Delete
            _navigationPropertyMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Rename",
                "Rename",
                KnownMonikers.Rename,
                "Rename this navigation property"));

            _navigationPropertyMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Delete",
                "Delete",
                KnownMonikers.DeleteListItem,
                "Delete this navigation property from the model"));

            // Select Association command (instead of Entity Key)
            _navigationPropertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "SelectAssociation",
                "Select Association",
                KnownMonikers.AssociationRelationship,
                "Select the association for this navigation property"));

            _navigationPropertyMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Move operations
            _navigationPropertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveToTop",
                "Move to Top",
                KnownMonikers.GoToTop,
                "Move this property to the top")
            {
                KeyboardShortcut = "Alt+Home"
            });

            _navigationPropertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveUp",
                "Move Up",
                KnownMonikers.MoveUp,
                "Move this property up")
            {
                KeyboardShortcut = "Alt+\u2191"
            });

            _navigationPropertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveDown",
                "Move Down",
                KnownMonikers.MoveDown,
                "Move this property down")
            {
                KeyboardShortcut = "Alt+\u2193"
            });

            _navigationPropertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveToBottom",
                "Move to Bottom",
                KnownMonikers.GoToBottom,
                "Move this property to the bottom")
            {
                KeyboardShortcut = "Alt+End"
            });

            _navigationPropertyMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Show commands (no Table Mapping or Stored Procedure Mapping for nav properties)
            _navigationPropertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInModelBrowser",
                "Show in Model Browser",
                KnownMonikers.ShowDiagramPane,
                "Show this property in the Model Browser"));

            _navigationPropertyMenu.MenuItems.Add(new MenuCommandDefinition(
                "Properties",
                "Show Properties",
                KnownMonikers.Property,
                "Show the Properties window")
            {
                KeyboardShortcut = "F4"
            });
        }

        /// <summary>
        /// Handles menu action events for the navigation property context menu.
        /// </summary>
        private void OnNavigationPropertyMenuActionExecuted(object sender, MenuActionEventArgs e)
        {
            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram == null || _currentNavigationPropertyHit == null)
            {
                return;
            }

            switch (e.ActionName)
            {
                case "Rename":
                    ExecuteNavigationPropertyRename();
                    break;

                case "Delete":
                    ExecuteNavigationPropertyDelete();
                    break;

                case "SelectAssociation":
                    ExecuteSelectAssociationForNavigationProperty(diagram);
                    break;

                case "MoveUp":
                    ExecuteNavigationPropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyUp);
                    break;

                case "MoveDown":
                    ExecuteNavigationPropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyDown);
                    break;

                case "MoveToTop":
                    ExecuteNavigationPropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyTop);
                    break;

                case "MoveToBottom":
                    ExecuteNavigationPropertyMove(MicrosoftDataEntityDesignCommands.MovePropertyBottom);
                    break;

                case "ShowInModelBrowser":
                    SelectCurrentNavigationProperty();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.ShowInEdmExplorer);
                    break;

                case "Properties":
                    ExecuteProperties();
                    break;
            }
        }

        #endregion

        #region Entity Menu

        /// <summary>
        /// Shows the entity context menu at the specified position.
        /// </summary>
        private void ShowEntityContextMenu(EntityDesignerSurface diagram, EntityTypeShape entityShape, PointD mousePosition)
        {
            _currentEntityShape = entityShape;

            // Create context menu if needed
            if (_entityMenu == null)
            {
                _entityMenu = new DiagramSurfaceContextMenu();
                _entityMenu.ActionExecuted += OnEntityMenuActionExecuted;
            }

            // Populate the menu with dynamic items based on the entity
            PopulateEntityMenu(entityShape);

            // Show the menu
            ShowMenuAtPosition(_entityMenu, diagram, mousePosition);
        }

        /// <summary>
        /// Populates the entity context menu with commands.
        /// </summary>
        private void PopulateEntityMenu(EntityTypeShape entityShape)
        {
            // Clear existing items (menu is rebuilt each time for dynamic content)
            _entityMenu.TopBarCommands.Clear();
            _entityMenu.MenuItems.Clear();

            var isExpanded = entityShape.IsExpanded;

            // Top bar commands - Cut | Copy | Paste | Rename | Delete
            _entityMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Cut",
                "Cut",
                KnownMonikers.Cut,
                "Cut this entity")
            {
                KeyboardShortcut = "Ctrl+X"
            });

            _entityMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Copy",
                "Copy",
                KnownMonikers.Copy,
                "Copy this entity")
            {
                KeyboardShortcut = "Ctrl+C"
            });

            _entityMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Paste",
                "Paste",
                KnownMonikers.Paste,
                "Paste")
            {
                KeyboardShortcut = "Ctrl+V"
            });

            _entityMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Rename",
                "Rename",
                KnownMonikers.Rename,
                "Rename this entity"));

            _entityMenu.TopBarCommands.Add(new MenuCommandDefinition(
                "Delete",
                "Delete",
                KnownMonikers.DeleteListItem,
                "Delete this entity from the model"));

            // Add New submenu
            var addNewCommand = new MenuCommandDefinition(
                "AddNew",
                "Add New",
                KnownMonikers.AddItem,
                "Add a new item to this entity");

            addNewCommand.Children.Add(new MenuCommandDefinition(
                "AddScalarProperty",
                "Scalar Property",
                KnownMonikers.NewProperty,
                "Add a new scalar property"));

            addNewCommand.Children.Add(new MenuCommandDefinition(
                "AddNavigationProperty",
                "Navigation Property",
                KnownMonikers.NavigationProperty,
                "Add a new navigation property"));

            addNewCommand.Children.Add(new MenuCommandDefinition(
                "AddComplexProperty",
                "Complex Property",
                KnownMonikers.ComplexProperty,
                "Add a new complex property"));

            addNewCommand.Children.Add(new MenuCommandDefinition(
                "AddAssociation",
                "Association...",
                KnownMonikers.AssociationRelationship,
                "Add a new association"));

            addNewCommand.Children.Add(new MenuCommandDefinition(
                "AddInheritance",
                "Inheritance...",
                KnownMonikers.Inheritance,
                "Add inheritance"));

            addNewCommand.Children.Add(new MenuCommandDefinition(
                "AddFunctionImport",
                "Function Import...",
                KnownMonikers.NewMethod,
                "Add a function import"));

            _entityMenu.MenuItems.Add(addNewCommand);

            _entityMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Collapse/Expand (dynamic based on state)
            if (isExpanded)
            {
                _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                    "Collapse",
                    "Collapse",
                    KnownMonikers.CollapseAll,
                    "Collapse this entity shape"));
            }
            else
            {
                _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                    "Expand",
                    "Expand",
                    KnownMonikers.ExpandAll,
                    "Expand this entity shape"));
            }

            _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                "IncludeRelated",
                "Include Related",
                KnownMonikers.AddRelationship,
                "Include related entities on the diagram"));

            _entityMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                "MoveToNewDiagram",
                "Move to New Diagram",
                KnownMonikers.NewDiagram,
                "Move this entity to a new diagram"));

            _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                "RemoveFromDiagram",
                "Remove from Diagram",
                KnownMonikers.Cancel,
                "Remove this entity from the diagram (does not delete from model)")
            {
                KeyboardShortcut = "Shift+Del"
            });

            _entityMenu.MenuItems.Add(MenuSeparatorDefinition.Instance);

            // Show commands
            _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInTableMapping",
                "Show in Table Mapping",
                KnownMonikers.SchemaProperty,
                "Show this entity in table mapping"));

            _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInStoredProcedureMapping",
                "Show in Stored Procedure Mapping",
                KnownMonikers.StoredProcedure,
                "Show this entity in stored procedure mapping"));

            _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                "ShowInModelBrowser",
                "Show in Model Browser",
                KnownMonikers.ShowDiagramPane,
                "Show this entity in the Model Browser"));

            _entityMenu.MenuItems.Add(new MenuCommandDefinition(
                "Properties",
                "Show Properties",
                KnownMonikers.Property,
                "Show the Properties window")
            {
                KeyboardShortcut = "F4"
            });
        }

        /// <summary>
        /// Handles menu action events for the entity context menu.
        /// </summary>
        private void OnEntityMenuActionExecuted(object sender, MenuActionEventArgs e)
        {
            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram == null || _currentEntityShape == null)
            {
                return;
            }

            switch (e.ActionName)
            {
                case "Cut":
                    ExecuteEntityCut();
                    break;

                case "Copy":
                    ExecuteEntityCopy();
                    break;

                case "Paste":
                    ExecuteEntityPaste();
                    break;

                case "Rename":
                    ExecuteEntityRename();
                    break;

                case "Delete":
                    ExecuteEntityDelete();
                    break;

                case "AddScalarProperty":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddScalarProperty);
                    break;

                case "AddNavigationProperty":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddNavigationProperty);
                    break;

                case "AddComplexProperty":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddComplexProperty);
                    break;

                case "AddAssociation":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddAssociation);
                    break;

                case "AddInheritance":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddInheritance);
                    break;

                case "AddFunctionImport":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddFunctionImport);
                    break;

                case "Collapse":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.CollapseEntityTypeShape);
                    break;

                case "Expand":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.ExpandEntityTypeShape);
                    break;

                case "IncludeRelated":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.IncludeRelatedEntityType);
                    break;

                case "MoveToNewDiagram":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.MoveToNewDiagram);
                    break;

                case "RemoveFromDiagram":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.RemoveFromDiagram);
                    break;

                case "ShowInTableMapping":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.TableMappings);
                    break;

                case "ShowInStoredProcedureMapping":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.SprocMappings);
                    break;

                case "ShowInModelBrowser":
                    SelectCurrentEntity();
                    ExecuteVsCommand(MicrosoftDataEntityDesignCommands.ShowInEdmExplorer);
                    break;

                case "Properties":
                    ExecuteProperties();
                    break;
            }
        }

        #endregion

        #region Command Execution

        private void ExecuteAddEntity(EntityDesignerSurface diagram)
        {
            // Use the existing AddNewEntityType method with a default position
            var dropPoint = GetCenterPoint();
            diagram.AddNewEntityType(dropPoint);
        }

        private void ExecuteAddComplexType(EntityDesignerSurface diagram)
        {
            // Execute the Add Complex Type command via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddComplexType);
        }

        private void ExecuteAddEnumType(EntityDesignerSurface diagram)
        {
            // Execute the Add Enum Type command via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddEnumType);
        }

        private void ExecuteAddAssociation(EntityDesignerSurface diagram)
        {
            // Check if there are enough entities to create an association
            if (diagram.ModelElement.EntityTypes.Count < 1)
            {
                VsUtils.ShowMessageBox(
                    PackageManager.Package,
                    Resources.ContextMenu_AddAssociation_NoEntities,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_INFO);
                return;
            }

            diagram.AddNewAssociation(null);
        }

        private void ExecuteAddInheritance(EntityDesignerSurface diagram)
        {
            // Check if there are enough entities
            if (diagram.ModelElement.EntityTypes.Count < 2)
            {
                VsUtils.ShowMessageBox(
                    PackageManager.Package,
                    Resources.ContextMenu_AddInheritance_NotEnoughEntities,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_INFO);
                return;
            }

            diagram.AddNewInheritance(null);
        }

        private void ExecuteAddFunctionImport(EntityDesignerSurface diagram)
        {
            diagram.AddNewFunctionImport(null);
        }

        private void ExecuteLayout(EntityDesignerSurface diagram)
        {
            diagram.AutoLayoutDiagram();
        }

        private void ExecuteExport(EntityDesignerSurface diagram)
        {
            // Show the export dialog directly
            var modelName = diagram.ModelElement?.Namespace ?? "EntityModel";
            var diagramShowsTypes = diagram.DisplayNameAndType;

            var dialog = new ExportDiagramDialog(modelName, diagramShowsTypes);

            // Get the VS main window handle via IVsUIShell
            IServiceProvider sp = PackageManager.Package;
            if (sp != null)
            {
                var uiShell = sp.GetService(typeof(SVsUIShell)) as IVsUIShell;
                if (uiShell != null)
                {
                    uiShell.GetDialogOwnerHwnd(out IntPtr hwndOwner);
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(dialog);
                    hwnd.Owner = hwndOwner;
                }
            }

            if (dialog.ShowDialog() == true)
            {
                var options = dialog.CreateExportOptions();
                var exportManager = new ExportManager(new ShellRasterExporter());
                exportManager.Export(diagram, options);
            }
        }

        private void ExecuteCollapseAll(EntityDesignerSurface diagram)
        {
            diagram.CollapseAllEntityTypeShapes();
        }

        private void ExecuteExpandAll(EntityDesignerSurface diagram)
        {
            diagram.ExpandAllEntityTypeShapes();
        }

        private void ExecuteZoomToFit(EntityDesignerSurface diagram)
        {
            diagram.ZoomToFit();
        }

        private void ExecuteToggleShowDataTypes(EntityDesignerSurface diagram)
        {
            diagram.DisplayNameAndType = !diagram.DisplayNameAndType;
            // Update the toggle state
            if (_showDataTypesCommand != null)
            {
                _showDataTypesCommand.IsChecked = diagram.DisplayNameAndType;
            }
        }

        private void ExecuteSelectAll(EntityDesignerSurface diagram)
        {
            // Select all shapes on the diagram
            if (diagram.ActiveDiagramView != null)
            {
                var selection = diagram.ActiveDiagramView.Selection;
                selection.Clear();
                foreach (var shape in diagram.NestedChildShapes)
                {
                    if (shape is NodeShape)
                    {
                        selection.Add(new DiagramItem(shape));
                    }
                }
            }
        }

        private void ExecuteMoveDiagramsToSeparateFile(EntityDesignerSurface diagram)
        {
            var artifact = diagram.GetModel()?.EditingContext?.GetEFArtifactService()?.Artifact;
            if (artifact is null)
            {
                return;
            }

            // Calls the command set directly rather than going through the shell command table: the OLE command's
            // status handler is scoped to the Diagrams node in the Model Browser, so it reports the command hidden
            // when the click came from the designer surface.
            MicrosoftDataEntityDesignCommandSet.MoveDiagramsToSeparateFile(artifact.Uri.LocalPath);
        }

        private void ExecuteShowMappingDetails()
        {
            // Execute the View Mapping Details command via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.ShowMappingDesigner);
        }

        private void ExecuteShowModelBrowser()
        {
            // Execute the View Model Browser command via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.ShowEdmExplorer);
        }

        private void ExecuteProperties()
        {
            // Show the Properties tool window directly via IVsUIShell
            // Properties window GUID: {EEFA5220-E298-11D0-8F78-00A0C9110057}
            IServiceProvider sp = PackageManager.Package;
            if (sp != null)
            {
                var uiShell = sp.GetService(typeof(SVsUIShell)) as IVsUIShell;
                if (uiShell != null)
                {
                    var propertiesWindowGuid = new Guid("{EEFA5220-E298-11D0-8F78-00A0C9110057}");
                    uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref propertiesWindowGuid, out IVsWindowFrame frame);
                    frame?.Show();
                }
            }
        }

        private void ExecuteUpdateModelFromDatabase(EntityDesignerSurface diagram)
        {
            // Execute the Update Model from Database wizard via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.RefreshFromDatabase);
        }

        private void ExecuteGenerateDatabaseFromModel(EntityDesignerSurface diagram)
        {
            // Execute the Generate Database from Model wizard via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.GenerateDatabaseScriptFromModel);
        }

        private void ExecuteAddCodeGenerationItem(EntityDesignerSurface diagram)
        {
            // Execute the Add Code Generation Item wizard via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.AddNewTemplate);
        }

        private void ExecuteValidate(EntityDesignerSurface diagram)
        {
            // Execute the Validate command via VS command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.Validate);
        }

        private void ExecuteVsCommand(System.ComponentModel.Design.CommandID commandId)
        {
            IServiceProvider sp = PackageManager.Package;
            if (sp != null)
            {
                var menuCommandService = sp.GetService(typeof(System.ComponentModel.Design.IMenuCommandService)) as System.ComponentModel.Design.IMenuCommandService;
                menuCommandService?.GlobalInvoke(commandId);
            }
        }

        private void ExecuteOpenXmlEditor(EntityDesignerSurface diagram)
        {
            var artifact = diagram.GetModel()?.EditingContext?.GetEFArtifactService()?.Artifact;
            if (artifact == null)
            {
                return;
            }

            IServiceProvider sp = PackageManager.Package;
            if (sp != null)
            {
                Microsoft.VisualStudio.Shell.VsShellUtilities.OpenDocumentWithSpecificEditor(
                    sp,
                    artifact.Uri.LocalPath,
                    CommonPackageConstants.xmlEditorGuid,
                    VSConstants.LOGVIEWID_Primary,
                    out _,
                    out _,
                    out IVsWindowFrame frame);

                frame?.Show();
            }
        }

        private PointD GetCenterPoint()
        {
            // Return a reasonable default position - center of the visible viewport
            // For simplicity, use a fixed point that will be visible
            return new PointD(100, 100);
        }

        // Association-specific commands

        private void ExecuteRenameAssociation(AssociationConnector connector)
        {
            // Trigger in-place rename on the connector
            if (connector != null && _docView.CurrentDesigner?.DiagramClientView != null)
            {
                var diagramItem = new DiagramItem(connector);
                _docView.CurrentDesigner.DiagramClientView.Selection.Set(diagramItem);

                // Execute the Rename command
                ExecuteVsCommand(MicrosoftDataEntityDesignCommands.Rename);
            }
        }

        private void ExecuteDeleteAssociation(EntityDesignerSurface diagram, AssociationConnector connector)
        {
            if (connector?.ModelElement == null)
            {
                return;
            }

            // Select the connector first
            var diagramItem = new DiagramItem(connector);
            diagram.ActiveDiagramView?.Selection.Set(diagramItem);

            // Execute delete via the standard VS Edit.Delete command
            var deleteCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Delete);
            ExecuteVsCommand(deleteCommand);
        }

        private void ExecuteSelectShape(EntityDesignerSurface diagram, EntityType entityType)
        {
            if (entityType == null || diagram.ActiveDiagramView == null)
            {
                return;
            }

            // Find the shape for this entity type
            foreach (var shape in diagram.NestedChildShapes)
            {
                if (shape is EntityTypeShape entityShape && entityShape.ModelElement == entityType)
                {
                    var diagramItem = new DiagramItem(entityShape);
                    diagram.ActiveDiagramView.Selection.Set(diagramItem);
                    break;
                }
            }
        }

        private void ExecuteSelectProperty(EntityDesignerSurface diagram, EntityType entityType, NavigationProperty navProperty)
        {
            if (entityType == null || navProperty == null || diagram.ActiveDiagramView == null)
            {
                return;
            }

            // Find the entity shape, then select the navigation property within it
            foreach (var shape in diagram.NestedChildShapes)
            {
                if (shape is EntityTypeShape entityShape && entityShape.ModelElement == entityType)
                {
                    // Find the compartment containing the navigation property
                    foreach (var nestedShape in entityShape.NestedChildShapes)
                    {
                        if (nestedShape is ElementListCompartment compartment)
                        {
                            var items = compartment.Items;
                            for (int i = 0; i < items.Count; i++)
                            {
                                if (items[i] == navProperty)
                                {
                                    var diagramItem = new DiagramItem(compartment, compartment.ListField, new ListItemSubField(i));
                                    diagram.ActiveDiagramView.Selection.Set(diagramItem);
                                    return;
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void ExecuteShowInModelBrowser(AssociationConnector connector)
        {
            if (connector?.ModelElement == null)
            {
                return;
            }

            // Select the connector first so ShowInEdmExplorer knows what to show
            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram?.ActiveDiagramView != null)
            {
                var diagramItem = new DiagramItem(connector);
                diagram.ActiveDiagramView.Selection.Set(diagramItem);
            }

            // Execute the Show in Model Browser command
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.ShowInEdmExplorer);
        }

        // Property-specific commands

        private void SelectCurrentProperty()
        {
            if (_currentPropertyHit?.Compartment == null || _currentPropertyHit.CompartmentItemIndex < 0)
            {
                return;
            }

            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram?.ActiveDiagramView == null)
            {
                return;
            }

            var compartment = _currentPropertyHit.Compartment;
            var diagramItem = new DiagramItem(compartment, compartment.ListField, new ListItemSubField(_currentPropertyHit.CompartmentItemIndex));
            diagram.ActiveDiagramView.Selection.Set(diagramItem);
        }

        private void ExecutePropertyCut()
        {
            SelectCurrentProperty();
            var cutCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Cut);
            ExecuteVsCommand(cutCommand);
        }

        private void ExecutePropertyCopy()
        {
            SelectCurrentProperty();
            var copyCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Copy);
            ExecuteVsCommand(copyCommand);
        }

        private void ExecutePropertyPaste()
        {
            SelectCurrentProperty();
            var pasteCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Paste);
            ExecuteVsCommand(pasteCommand);
        }

        private void ExecutePropertyRename()
        {
            SelectCurrentProperty();
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.Rename);
        }

        private void ExecutePropertyDelete()
        {
            SelectCurrentProperty();
            var deleteCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Delete);
            ExecuteVsCommand(deleteCommand);
        }

        private void ExecuteToggleEntityKey()
        {
            if (_currentPropertyHit?.ModelElement is Microsoft.Data.Entity.Design.Diagrams.ViewModel.ScalarProperty scalarProperty)
            {
                scalarProperty.ChangeEntityKey();
            }
        }

        private void ExecutePropertyMove(System.ComponentModel.Design.CommandID moveCommand)
        {
            SelectCurrentProperty();
            ExecuteVsCommand(moveCommand);
        }

        // Navigation property-specific commands

        private void SelectCurrentNavigationProperty()
        {
            if (_currentNavigationPropertyHit?.Compartment == null || _currentNavigationPropertyHit.CompartmentItemIndex < 0)
            {
                return;
            }

            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram?.ActiveDiagramView == null)
            {
                return;
            }

            var compartment = _currentNavigationPropertyHit.Compartment;
            var diagramItem = new DiagramItem(compartment, compartment.ListField, new ListItemSubField(_currentNavigationPropertyHit.CompartmentItemIndex));
            diagram.ActiveDiagramView.Selection.Set(diagramItem);
        }

        private void ExecuteNavigationPropertyRename()
        {
            SelectCurrentNavigationProperty();
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.Rename);
        }

        private void ExecuteNavigationPropertyDelete()
        {
            SelectCurrentNavigationProperty();
            var deleteCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Delete);
            ExecuteVsCommand(deleteCommand);
        }

        private void ExecuteNavigationPropertyMove(System.ComponentModel.Design.CommandID moveCommand)
        {
            SelectCurrentNavigationProperty();
            ExecuteVsCommand(moveCommand);
        }

        private void ExecuteSelectAssociationForNavigationProperty(EntityDesignerSurface diagram)
        {
            if (_currentNavigationPropertyHit?.ModelElement is Microsoft.Data.Entity.Design.Diagrams.ViewModel.NavigationProperty navProp)
            {
                // Find the association connector for this navigation property
                var association = navProp.Association;
                if (association != null)
                {
                    // Find the connector shape for this association
                    foreach (var shape in diagram.NestedChildShapes)
                    {
                        if (shape is AssociationConnector connector && connector.ModelElement == association)
                        {
                            var diagramItem = new DiagramItem(connector);
                            diagram.ActiveDiagramView?.Selection.Set(diagramItem);
                            break;
                        }
                    }
                }
            }
        }

        // Entity-specific commands

        private void SelectCurrentEntity()
        {
            if (_currentEntityShape == null)
            {
                return;
            }

            var diagram = _docView.CurrentDiagram as EntityDesignerSurface;
            if (diagram?.ActiveDiagramView == null)
            {
                return;
            }

            var diagramItem = new DiagramItem(_currentEntityShape);
            diagram.ActiveDiagramView.Selection.Set(diagramItem);
        }

        private void ExecuteEntityCut()
        {
            SelectCurrentEntity();
            var cutCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Cut);
            ExecuteVsCommand(cutCommand);
        }

        private void ExecuteEntityCopy()
        {
            SelectCurrentEntity();
            var copyCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Copy);
            ExecuteVsCommand(copyCommand);
        }

        private void ExecuteEntityPaste()
        {
            SelectCurrentEntity();
            var pasteCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Paste);
            ExecuteVsCommand(pasteCommand);
        }

        private void ExecuteEntityRename()
        {
            SelectCurrentEntity();
            ExecuteVsCommand(MicrosoftDataEntityDesignCommands.Rename);
        }

        private void ExecuteEntityDelete()
        {
            SelectCurrentEntity();
            var deleteCommand = new System.ComponentModel.Design.CommandID(
                VSConstants.GUID_VSStandardCommandSet97,
                (int)VSConstants.VSStd97CmdID.Delete);
            ExecuteVsCommand(deleteCommand);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_diagramSurfaceMenu != null)
            {
                _diagramSurfaceMenu.ActionExecuted -= OnDiagramSurfaceMenuActionExecuted;
                _diagramSurfaceMenu.Dispose();
                _diagramSurfaceMenu = null;
            }

            if (_associationMenu != null)
            {
                _associationMenu.ActionExecuted -= OnAssociationMenuActionExecuted;
                _associationMenu.Dispose();
                _associationMenu = null;
            }

            if (_propertyMenu != null)
            {
                _propertyMenu.ActionExecuted -= OnPropertyMenuActionExecuted;
                _propertyMenu.Dispose();
                _propertyMenu = null;
            }

            if (_navigationPropertyMenu != null)
            {
                _navigationPropertyMenu.ActionExecuted -= OnNavigationPropertyMenuActionExecuted;
                _navigationPropertyMenu.Dispose();
                _navigationPropertyMenu = null;
            }

            if (_entityMenu != null)
            {
                _entityMenu.ActionExecuted -= OnEntityMenuActionExecuted;
                _entityMenu.Dispose();
                _entityMenu = null;
            }
        }

        #endregion
    }
}
