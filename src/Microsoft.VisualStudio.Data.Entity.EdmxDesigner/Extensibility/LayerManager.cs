// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.Extensibility;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels;
using Microsoft.VisualStudio.Modeling.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility
{

    /// <summary>
    ///     Loads, enables and unloads the designer layer extensions for one artifact, and owns the dynamic menu
    ///     commands they contribute.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A layer is a MEF extension that decorates the designer for a particular scenario. Each one gets an
    ///         Enable/Disable menu command, and whether it is enabled is persisted into the artifact's designer
    ///         options so it survives a reload.
    ///     </para>
    ///     <para>
    ///         Command IDs are allocated from two fixed ranges — one for ordinary commands and one for refactoring
    ///         commands — with a free list per range so that IDs released by unloading a layer are reused rather
    ///         than exhausting the range.
    ///     </para>
    ///     <para>
    ///         One instance per artifact, created and disposed with it.
    ///     </para>
    /// </remarks>
    internal class LayerManager
    {

        #region Fields

        private const string _propertyNameFormat = "IsLayerEnabled_{0}";

        private readonly EFArtifact _artifact;
        private readonly List<int> _commandIdFreeList = [];
        private readonly IDictionary<EntityDesignerCommand, CommandID> _commands2ids = new Dictionary<EntityDesignerCommand, CommandID>();
        private readonly IDictionary<int, EntityDesignerCommand> _intIds2commands = new Dictionary<int, EntityDesignerCommand>();
        private readonly Dictionary<IEntityDesignerLayer, LayerState> _layer2state = [];
        private readonly List<int> _refactoringCommandIdFreeList = [];

        private int _currentCommandId = PackageConstants.cmdIdLayerCommandsBase;
        private int _currentRefactoringCommandId = PackageConstants.cmdIdLayerRefactoringCommandsBase;
        private EntityDesignSelectionContainer<LayerSelection> _selectionContainer;

        #endregion

        #region Properties

        /// <summary>
        ///     The layers currently enabled for this artifact.
        /// </summary>
        internal IEnumerable<IEntityDesignerLayer> EnabledLayerExtensions
        {
            get
            {
                return from l2s in _layer2state
                       where l2s.Value.IsEnabled
                       select l2s.Key;
            }
        }

        /// <summary>
        ///     The primary selection in the designer, or <see langword="null" /> if nothing is selected.
        /// </summary>
        internal EFObject SelectedEFObject
        {
            get
            {
                var editingContext = PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(_artifact.Uri);
                if (editingContext != null)
                {
                    Selection selection = editingContext.Items.GetValue<UI.Views.EntityDesigner.EntityDesignerSelection>();
                    if (selection != null)
                    {
                        return selection.PrimarySelection;
                    }
                }
                return null;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a layer manager for one artifact.
        /// </summary>
        /// <param name="artifact">The artifact whose layers this manages. Must not be <see langword="null" />.</param>
        internal LayerManager(EFArtifact artifact)
        {
            Debug.Assert(artifact != null, "Must pass in non-null artifact to LayerManager");

            _artifact = artifact;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Filters an extension list down to those that should be active.
        /// </summary>
        /// <typeparam name="T">The extension type.</typeparam>
        /// <typeparam name="M">The extension's metadata type.</typeparam>
        /// <param name="extensionList">The extensions to filter.</param>
        /// <param name="excludeLayers">Exclude extensions belonging to a layer.</param>
        /// <param name="excludeNonLayers">Exclude extensions not belonging to any layer.</param>
        /// <returns>The extensions that survive the filter.</returns>
        /// <remarks>
        ///     An extension belonging to a layer is only included when that layer is currently enabled.
        /// </remarks>
        internal IEnumerable<Lazy<T, M>> Filter<T, M>(
            IEnumerable<Lazy<T, M>> extensionList, bool excludeLayers = false, bool excludeNonLayers = false)
        {
            return extensionList.Where(
                l =>
                    {
                        if (l.Metadata != null)
                        {
                            if (l.Metadata is IEntityDesignerLayerData layerData
                                && !String.IsNullOrWhiteSpace(layerData.LayerName))
                            {
                                return !excludeLayers && IsLayerEnabled(layerData.LayerName);
                            }
                        }
                        return !excludeNonLayers;
                    });
        }

        /// <summary>
        ///     Whether the named layer is recorded as enabled in the artifact's designer options.
        /// </summary>
        /// <param name="layerName">The layer's name.</param>
        /// <returns><see langword="true" /> if the layer is enabled.</returns>
        internal bool IsLayerEnabled(string layerName)
        {
            if (!String.IsNullOrEmpty(layerName))
            {
                return ModelHelper.GetDesignerPropertyValueFromArtifactAsBool(
                    OptionsDesignerInfo.ElementName
                    , String.Format(CultureInfo.CurrentCulture, _propertyNameFormat, layerName), false, _artifact);
            }
            return false;
        }

        /// <summary>
        ///     Pushes a layer's requested selection into the designer's editing context.
        /// </summary>
        /// <param name="sender">The layer raising the request.</param>
        /// <param name="e">Identifiers of the model items the layer wants selected.</param>
        internal void layer_EntityDesignerSelectionChanged(object sender, ChangeEntityDesignerSelectionEventArgs e)
        {
            if (PackageManager.Package.DocumentFrameMgr != null
                && PackageManager.Package.DocumentFrameMgr.EditingContextManager != null)
            {
                if (PackageManager.Package.DocumentFrameMgr.EditingContextManager.DoesContextExist(_artifact.Uri))
                {
                    var editingContext = PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(
                        _artifact.Uri);
                    Debug.Assert(editingContext != null, "EditingContext must not be null if we found that a context exists");
                    if (editingContext != null)
                    {
                        // TODO handle multiple selection at some point
                        List<EFNameableItem> selectedItems = new List<EFNameableItem>();
                        foreach (var selectionIdentifier in e.SelectionIdentifiers)
                        {
                            if (!String.IsNullOrEmpty(selectionIdentifier))
                            {
                                var nameableItem = XmlModelHelper.FindNameableItemViaIdentifier(
                                    _artifact.ConceptualModel(), selectionIdentifier);
                                if (nameableItem != null)
                                {
                                    selectedItems.Add(nameableItem);
                                }
                            }
                        }

                        if (selectedItems.Count > 0)
                        {
                            editingContext.Items.SetValue(new LayerSelection(selectedItems));
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Discovers the available layers, restores their enabled state, and registers their commands.
        /// </summary>
        internal void Load()
        {
            // Load all the layers first
            var extensions = EdmxExtensionPointManager.LoadLayerExtensions();
            if (extensions != null)
            {
                EFElement selectedEFElement = SelectedEFObject as EFElement;
                foreach (var ex in extensions)
                {
                    var layer = ex.Value;
                    if (layer != null)
                    {
                        var isLayerEnabled = IsLayerEnabled(layer.Name);
                        var addedCommand = AddEnableLayerCommand(layer, isLayerEnabled, out EntityDesignerCommand enableCommand);
                        if (addedCommand && enableCommand != null)
                        {
                            if (isLayerEnabled)
                            {
                                LoadLayer(layer, selectedEFElement?.XObject);
                            }

                            LayerState layerState = new LayerState { IsEnabled = isLayerEnabled, EnableCommand = enableCommand };
                            _layer2state.Add(layer, layerState);
                        }
                    }
                }
            }

            // TODO Now we load the commands from the command factories. At some point we should move this out of the layer manager
            // into a more global object (not tied to the lifetime of the artifact)
            foreach (var command in GetCommands())
            {
                AddCommand(command);
            }
            ListenToSelections();
        }

        /// <summary>
        ///     Forwards a committed set of XML changes to every enabled layer.
        /// </summary>
        /// <param name="xmlChanges">The changes that were committed.</param>
        internal void OnAfterTransactionCommitted(IEnumerable<Tuple<XObject, XObjectChange>> xmlChanges)
        {
            foreach (var layer in EnabledLayerExtensions)
            {
                layer.OnAfterTransactionCommitted(xmlChanges);
            }
        }

        /// <summary>
        ///     Enables a disabled layer or disables an enabled one, persisting the new state.
        /// </summary>
        /// <param name="layer">The layer to toggle.</param>
        /// <param name="selectedXObject">The currently selected XML node, passed to the layer on load.</param>
        internal void ToggleLayerEnabled(IEntityDesignerLayer layer, XObject selectedXObject)
        {
            if (_layer2state.TryGetValue(layer, out LayerState layerState))
            {
                layerState.IsEnabled = !layerState.IsEnabled;
                layerState.EnableCommand.Name = GetEnableLayerCommandText(layer.Name, layerState.IsEnabled);
            }

            Debug.Assert(layerState != null, "LayerState is null for layer '" + layer.Name + "'");
            if (layerState != null
                && layerState.IsEnabled)
            {
                PersistLayerEnabled(layer, true);
                LoadLayer(layer, selectedXObject);
            }
            else
            {
                UnloadLayer(layer);
                PersistLayerEnabled(layer, false);
            }
        }

        /// <summary>
        ///     Unloads every layer and removes all commands this manager registered.
        /// </summary>
        internal virtual void Unload()
        {
            StopListeningToSelections();
            UnloadAllLayers();

            List<EntityDesignerCommand> commandsToRemove = new List<EntityDesignerCommand>();
            foreach (var leftoverCommand in _commands2ids.Keys)
            {
                commandsToRemove.Add(leftoverCommand);
            }

            foreach (var command in commandsToRemove)
            {
                RemoveCommand(command);
            }
        }

        /// <summary>
        ///     Unloads every layer, leaving this manager's own commands in place.
        /// </summary>
        internal void UnloadAllLayers()
        {
            List<IEntityDesignerLayer> layersToRemove = new List<IEntityDesignerLayer>();
            foreach (var layer in _layer2state.Keys)
            {
                UnloadLayer(layer);
                layersToRemove.Add(layer);
            }

            foreach (var layer in layersToRemove)
            {
                _layer2state.Remove(layer);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Registers a command with the package, allocating it an ID from the appropriate range.
        /// </summary>
        /// <param name="command">The command to register.</param>
        /// <returns><see langword="true" /> if the command was registered.</returns>
        /// <remarks>
        ///     Prefers an ID from the free list so that IDs released by unloading a layer are reused. Refactoring
        ///     commands have their own range, counter and free list.
        /// </remarks>
        private bool AddCommand(EntityDesignerCommand command)
        {
            var usingIdFromFreeList = false;

            int newCommandId;

            // If this is a refactoring command, we have a separate
            // free list and counter to keep track of it since it
            // lives in a separate command ID range.
            var currentCommandId = _currentCommandId;
            var freeList = _commandIdFreeList;
            if (command.IsRefactoringCommand)
            {
                freeList = _refactoringCommandIdFreeList;
                currentCommandId = _currentRefactoringCommandId;
            }

            if (freeList.Count > 0)
            {
                // _commandIdFreeList is tracking gaps in our command IDs, so use these instead of incrementing the current Id counter
                // if available.
                newCommandId = freeList[0];
                usingIdFromFreeList = true;
            }
            else
            {
                newCommandId = currentCommandId;
            }

            CommandID commandId = new CommandID(PackageConstants.guidEscherCmdSet, newCommandId);

            if (PackageManager.Package.CommandSet.AddCommand(commandId, command, out DynamicStatusMenuCommand menuCommand))
            {
                // If we are attempting to use an id from the free list then we should
                // remove it from the free list if we've successfully added the command, or
                // just increment our current counter. This is different for refactoring commands
                // since refactoring commands live in a separate command ID range.
                if (command.IsRefactoringCommand)
                {
                    if (usingIdFromFreeList)
                    {
                        _refactoringCommandIdFreeList.RemoveAt(0);
                    }
                    else
                    {
                        _currentRefactoringCommandId = newCommandId + 1;
                    }
                }
                else
                {
                    if (usingIdFromFreeList)
                    {
                        _commandIdFreeList.RemoveAt(0);
                    }
                    else
                    {
                        _currentCommandId = newCommandId + 1;
                    }
                }

                _commands2ids.Add(command, commandId);
                _intIds2commands.Add(commandId.ID, command);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Adds the Enable/Disable menu command for one layer.
        /// </summary>
        /// <param name="layer">The layer the command toggles.</param>
        /// <param name="isAlreadyEnabled">Whether the layer is currently enabled, which sets the command's text.</param>
        /// <param name="entityDesignerCommand">Receives the command that was created.</param>
        /// <returns><see langword="true" /> if the command was registered.</returns>
        private bool AddEnableLayerCommand(
            IEntityDesignerLayer layer, bool isAlreadyEnabled, out EntityDesignerCommand entityDesignerCommand)
        {
            var menuItemText = GetEnableLayerCommandText(layer.Name, isAlreadyEnabled);
            entityDesignerCommand = new EntityDesignerCommand(menuItemText, (xel, dv, ss, pc, iss) => { ToggleLayerEnabled(layer, xel); });

            return AddCommand(entityDesignerCommand);
        }

        /// <summary>
        ///     Finds the ID a command was registered under.
        /// </summary>
        /// <param name="command">The command to look up.</param>
        /// <returns>The command's ID, or <see langword="null" /> if it is not registered.</returns>
        private CommandID GetCommandID(EntityDesignerCommand command)
        {
            return _commands2ids.Where(kvp => kvp.Key.Equals(command)).Select(kvp => kvp.Value).FirstOrDefault();
        }

        /// <summary>
        ///     Collects the commands contributed by the command factory extensions.
        /// </summary>
        /// <param name="layer">
        ///     The layer whose commands to collect, or <see langword="null" /> for the commands that belong to no
        ///     layer.
        /// </param>
        /// <returns>The commands to register.</returns>
        private static IEnumerable<EntityDesignerCommand> GetCommands(IEntityDesignerLayer layer = null)
        {
            List<EntityDesignerCommand> commandsToReturn = new List<EntityDesignerCommand>();
            var commandsForLayer = EdmxExtensionPointManager.LoadCommandExtensions(layer == null, layer != null);

            foreach (var lazyFactory in commandsForLayer)
            {
                var factory = lazyFactory.Value;
                if (factory != null)
                {
                    commandsToReturn.AddRange(factory.Commands);
                }
            }
            return commandsToReturn;
        }

        /// <summary>
        ///     Builds the menu text for a layer's Enable/Disable command.
        /// </summary>
        /// <param name="layerName">The layer's name.</param>
        /// <param name="isEnabled">Whether the layer is currently enabled.</param>
        /// <returns>The menu item text.</returns>
        private static string GetEnableLayerCommandText(string layerName, bool isEnabled)
        {
            return String.Format(
                CultureInfo.CurrentCulture, isEnabled ? EdmxDesignerResources.Layer_DisableLayer : EdmxDesignerResources.Layer_EnableLayer, layerName);
        }

        /// <summary>
        ///     Subscribes to designer selection changes so enabled layers can follow them.
        /// </summary>
        private void ListenToSelections()
        {
            var editingContext = PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(_artifact.Uri);
            editingContext.Items.Subscribe<UI.Views.EntityDesigner.EntityDesignerSelection>(OnEntityDesignerSelectionChanged);
        }

        /// <summary>
        ///     Brings one layer into service: notifies it, gives it a selection container, and adds its commands.
        /// </summary>
        /// <param name="layer">The layer to load.</param>
        /// <param name="selectedXObject">
        ///     The selected XML node to hand the layer, or <see langword="null" /> to hand it the conceptual model.
        /// </param>
        private void LoadLayer(IEntityDesignerLayer layer, XObject selectedXObject)
        {
            if (_artifact != null
                && _artifact.ConceptualModel() != null)
            {
                if (selectedXObject != null)
                {
                    layer.OnAfterLayerLoaded(selectedXObject);
                }
                else
                {
                    layer.OnAfterLayerLoaded(_artifact.ConceptualModel().XObject);
                }
            }

            if (layer.ServiceProvider != null
                && PackageManager.Package.DocumentFrameMgr != null
                && PackageManager.Package.DocumentFrameMgr.EditingContextManager != null)
            {
                if (PackageManager.Package.DocumentFrameMgr.EditingContextManager.DoesContextExist(_artifact.Uri))
                {
                    var editingContext = PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(
                        _artifact.Uri);
                    Debug.Assert(editingContext != null, "EditingContext must not be null if we found that a context exists");
                    if (editingContext != null)
                    {
                        // TODO there should be one independent selection container for each layer.
                        _selectionContainer = new EntityDesignSelectionContainer<LayerSelection>(layer.ServiceProvider, editingContext);
                    }
                }
            }

            layer.ChangeEntityDesignerSelection += layer_EntityDesignerSelectionChanged;
            foreach (var command in GetCommands(layer))
            {
                AddCommand(command);
            }
        }

        /// <summary>
        ///     Tells every loaded layer that the designer's selection changed.
        /// </summary>
        /// <param name="selection">The new selection.</param>
        private void OnEntityDesignerSelectionChanged(UI.Views.EntityDesigner.EntityDesignerSelection selection)
        {
            // We are seeing selection.PrimarySelection == null when the artifact is reloaded so add check here to prevent NRE.
            if (selection.PrimarySelection != null)
            {
                var selectedXObject = selection.PrimarySelection.XObject;
                if (selectedXObject != null)
                {
                    foreach (var layer in _layer2state.Keys)
                    {
                        layer.OnSelectionChanged(selectedXObject);
                    }
                }
            }
        }

        /// <summary>
        ///     Records a layer's enabled state in the artifact's designer options, so it survives a reload.
        /// </summary>
        /// <param name="layer">The layer whose state to record.</param>
        /// <param name="enable">The state to record.</param>
        private void PersistLayerEnabled(IEntityDesignerLayer layer, bool enable)
        {
            var editingContextMgr = PackageManager.Package.DocumentFrameMgr.EditingContextManager;
            Debug.Assert(editingContextMgr.DoesContextExist(_artifact.Uri), "There should be an existing editing context");
            if (editingContextMgr.DoesContextExist(_artifact.Uri))
            {
                var txname = string.Format(
                    CultureInfo.CurrentCulture, enable ? EdmxDesignerResources.Tx_LayerEnable : EdmxDesignerResources.Tx_LayerDisable, layer.Name);
                CommandProcessorContext cpc = new CommandProcessorContext(
                    editingContextMgr.GetNewOrExistingContext(_artifact.Uri), EfiTransactionOriginator.EntityDesignerOriginatorId, txname);
                var cmd = ModelHelper.CreateSetDesignerPropertyValueCommandFromArtifact(
                    cpc.Artifact, OptionsDesignerInfo.ElementName
                    , string.Format(CultureInfo.CurrentCulture, _propertyNameFormat, layer.Name)
                    , enable.ToString());
                if (cmd != null)
                {
                    CommandProcessor.InvokeSingleCommand(cpc, cmd);
                }
            }
        }

        /// <summary>
        ///     Unregisters a command and returns its ID to the appropriate free list.
        /// </summary>
        /// <param name="command">The command to remove.</param>
        private void RemoveCommand(EntityDesignerCommand command)
        {
            var commandID = GetCommandID(command);
            if (commandID != null)
            {
                if (PackageManager.Package.CommandSet.RemoveCommand(commandID))
                {
                    _commands2ids.Remove(command);

                    var indexToRemove = commandID.ID;
                    Debug.Assert(indexToRemove >= 0, "The index to remove is less than zero");
                    if (indexToRemove >= 0)
                    {
                        _intIds2commands.Remove(indexToRemove);

                        // Add the Id of the command we remove to the free list, so that we can fill the command Id gaps
                        // when we next load a dynamic command. The list differs for a refactoring command since
                        // those commands live in a different Command ID range.
                        var freeList = _commandIdFreeList;
                        if (command.IsRefactoringCommand)
                        {
                            freeList = _refactoringCommandIdFreeList;
                        }

                        freeList.Add(indexToRemove);
                        freeList.Sort();
                    }
                }
            }
        }

        /// <summary>
        ///     Unsubscribes from designer selection changes.
        /// </summary>
        private void StopListeningToSelections()
        {
            var editingContext = PackageManager.Package.DocumentFrameMgr.EditingContextManager.GetNewOrExistingContext(_artifact.Uri);
            editingContext.Items.Unsubscribe<UI.Views.EntityDesigner.EntityDesignerSelection>(OnEntityDesignerSelectionChanged);
        }

        /// <summary>
        ///     Takes one layer out of service: notifies it, removes its commands, and drops its selection container.
        /// </summary>
        /// <param name="layer">The layer to unload.</param>
        private void UnloadLayer(IEntityDesignerLayer layer)
        {
            if (_artifact != null
                && _artifact.ConceptualModel() != null)
            {
                layer.OnBeforeLayerUnloaded(_artifact.ConceptualModel().XObject);
            }

            foreach (var command in GetCommands(layer))
            {
                RemoveCommand(command);
            }

            layer.ChangeEntityDesignerSelection -= layer_EntityDesignerSelectionChanged;
            _selectionContainer?.Dispose();
            _selectionContainer = null;
        }

        #endregion

    }

}
