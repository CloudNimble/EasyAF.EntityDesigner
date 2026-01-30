// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Eventing;
using Microsoft.Data.Entity.Design.UI.ViewModels.Explorer;
using Microsoft.Data.Entity.Design.UI.Views.Explorer;
using Microsoft.VisualStudio.Modeling.Shell;

namespace Microsoft.Data.Entity.Design.VisualStudio.Package
{
    [Guid(PackageConstants.guidExplorerWindowString)]
    internal class EntityDesignExplorerWindow : ExplorerWindow
    {
        private const int AddComplexPropertyCommandMaxCount = 10;

        // <summary>
        //     Standard constructor for the tool window.
        // </summary>
        public EntityDesignExplorerWindow()
            : base(PackageManager.Package)
        {
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window. The resource ID correspond to the 
            // one defined in the resx file while the Index is the offset in the 
            // bitmap strip. Each image in the strip being 16x16.
            BitmapResourceID = 105;
            BitmapIndex = 0;

            Caption = Resources.ExplorerWindowCaption;
        }

        // <summary>
        //     Sets the current context on the ExplorerInfo.
        // </summary>
        protected override void SetExplorerInfo()
        {
            CurrentExplorerInfo.SetExplorerInfo(
                new EntityDesignExplorerFrame(EditingContext), new EntityDesignSelectionContainer<ExplorerSelection>(this, EditingContext));
        }

        // <summary>
        //     Gets the identifier of the context menu command.
        // </summary>
        protected override CommandID GetContextMenuCommandID()
        {
            return new CommandID(PackageConstants.guidEscherCmdSet, PackageConstants.menuidExplorer);
        }

        protected override void OnBeforeShowContextMenu()
        {
            // Creating commands for adding scalar and complex properties to complex types (one for each complex type from the model).
            // Since the scalar types can change dependent on the schemaVersion and since complex types can be added or deleted
            // we need to compute these commands each time a context menu is requested

            // only need to do this if selected is ExplorerComplexType
            var selectedElement = CurrentExplorerInfo._explorerFrame.GetSelectedExplorerEFElement();
            if (selectedElement is ExplorerComplexType)
            {
                var service = EditingContext.GetEFArtifactService();
                Debug.Assert(service != null && service.Artifact != null, "service and service.Artifact must both be non-null");
                if (service != null
                    && service.Artifact != null)
                {
                    // Creating commands for adding scalar properties to complex types (one for each primitive type)
                    var i = 0;
                    foreach (var type in ModelHelper.AllPrimitiveTypesSorted(service.Artifact.SchemaVersion))
                    {
                        CommandID cmdId = new CommandID(
                            PackageConstants.guidEscherCmdSet, PackageConstants.cmdIdExplorerAddScalarPropertyBase + i);
                        var cmd = MenuCommandService.FindCommand(cmdId);
                        if (cmd == null)
                        {
                            cmd = new DynamicStatusMenuCommand(OnStatusAddComplexTypeProperty, OnMenuAddComplexTypeProperty, cmdId);
                            cmd.Properties[PackageConstants.guidEscherCmdSet] = type;
                            MenuCommandService.AddCommand(cmd);
                        }
                        else
                        {
                            cmd.Properties[PackageConstants.guidEscherCmdSet] = type;
                        }
                        i++;
                    }

                    // set up commands for complex types
                    var conceptualModel = service.Artifact.ConceptualModel();
                    Debug.Assert(conceptualModel != null, "service.Artifact.ConceptualModel() should not be null");
                    if (conceptualModel != null)
                    {
                        List<ComplexType> complexTypes = new List<ComplexType>(conceptualModel.ComplexTypes());
                        complexTypes.Sort(EFElement.EFElementDisplayNameComparison);

                        i = 0;
                        foreach (var complexType in complexTypes)
                        {
                            // don't add an item for a ComplexType that is same as currently selected one
                            if (selectedElement.ModelItem == complexType)
                            {
                                continue;
                            }

                            // if we find an old command with the same cmdId remove it and replace
                            // with the new one to force VS to refresh the text
                            CommandID cmdId = new CommandID(
                                PackageConstants.guidEscherCmdSet, PackageConstants.cmdIdExplorerAddComplexPropertyBase + i);
                            var cmd = MenuCommandService.FindCommand(cmdId);
                            if (cmd != null)
                            {
                                MenuCommandService.RemoveCommand(cmd);
                            }
                            cmd = new DynamicStatusMenuCommand(OnStatusAddComplexTypeProperty, OnMenuAddComplexTypeProperty, cmdId);
                            cmd.Properties[PackageConstants.guidEscherCmdSet] = complexType;
                            MenuCommandService.AddCommand(cmd);

                            i++;
                            if (i >= AddComplexPropertyCommandMaxCount)
                            {
                                // break after adding 10 ComplexTypes
                                break;
                            }
                        }

                        // if some of the complex types were removed, we need to remove unnecessary commands
                        var cmd2 =
                            MenuCommandService.FindCommand(
                                new CommandID(PackageConstants.guidEscherCmdSet, PackageConstants.cmdIdExplorerAddComplexPropertyBase + i));
                        while (i < AddComplexPropertyCommandMaxCount
                               && cmd2 != null)
                        {
                            MenuCommandService.RemoveCommand(cmd2);
                            i++;
                            cmd2 =
                                MenuCommandService.FindCommand(
                                    new CommandID(
                                        PackageConstants.guidEscherCmdSet, PackageConstants.cmdIdExplorerAddComplexPropertyBase + i));
                        }
                    }
                }
            }
        }

        private void OnStatusAddComplexTypeProperty(object sender, EventArgs e)
        {
            if (sender is DynamicStatusMenuCommand cmd)
            {
                cmd.Visible = cmd.Enabled = false;
                if (CurrentExplorerInfo != null
                    && CurrentExplorerInfo._explorerFrame != null)
                {
                    var selectedElement = CurrentExplorerInfo._explorerFrame.GetSelectedExplorerEFElement();
                    if (selectedElement is ExplorerComplexType)
                    {
                        if (cmd.Properties.Contains(PackageConstants.guidEscherCmdSet))
                        {
                            string typeName = null;
                            if (cmd.Properties[PackageConstants.guidEscherCmdSet] is ComplexType complexType)
                            {
                                typeName = complexType.LocalName.Value;
                            }
                            else
                            {
                                typeName = cmd.Properties[PackageConstants.guidEscherCmdSet] as string;
                            }

                            if (!String.IsNullOrEmpty(typeName))
                            {
                                cmd.Text = typeName;
                                cmd.Visible = cmd.Enabled = true;
                            }
                        }
                    }
                }
            }
        }

        private void OnMenuAddComplexTypeProperty(object sender, EventArgs e)
        {
            if (sender is MenuCommand cmd)
            {
                if (CurrentExplorerInfo != null
                    && CurrentExplorerInfo._explorerFrame != null)
                {
                    if (CurrentExplorerInfo._explorerFrame.GetSelectedExplorerEFElement() is ExplorerComplexType explorerComplexType)
                    {
                        if (cmd.Properties.Contains(PackageConstants.guidEscherCmdSet))
                        {
                            EfiTransactionContext context = new EfiTransactionContext();
                            CommandProcessorContext cpc = new CommandProcessorContext(
                                EditingContext, EfiTransactionOriginator.ExplorerWindowOriginatorId, Resources.Tx_CreateScalarProperty, null,
                                context);
                            Property createdProperty = null;
                            if (cmd.Properties[PackageConstants.guidEscherCmdSet] is string type)
                            {
                                createdProperty = CreateComplexTypePropertyCommand.CreateDefaultProperty(
                                    cpc, explorerComplexType.ModelItem as ComplexType, type);
                            }
                            else
                            {
                                ComplexType complexType = cmd.Properties[PackageConstants.guidEscherCmdSet] as ComplexType;
                                Debug.Assert(complexType != null, "Unexpected property type");
                                if (complexType != null)
                                {
                                    createdProperty = CreateComplexTypePropertyCommand.CreateDefaultProperty(
                                        cpc, explorerComplexType.ModelItem as ComplexType, complexType);
                                }
                            }
                            if (createdProperty != null)
                            {
                                var info = CurrentExplorerInfo;
                                if (info != null
                                    && info._explorerFrame != null)
                                {
                                    EntityDesignExplorerFrame frame = info._explorerFrame as EntityDesignExplorerFrame;
                                    Debug.Assert(frame != null, "Could not get Explorer frame");
                                    frame?.NavigateToElementAndPutInRenameMode(createdProperty);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}