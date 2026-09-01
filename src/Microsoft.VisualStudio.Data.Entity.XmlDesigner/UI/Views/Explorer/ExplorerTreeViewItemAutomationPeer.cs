// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Automation.Peers;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.Views.Explorer
{

    /// <summary>
    ///     TreeViewItemAutomationPeer is a standard WPF Automation Peer; we are overriding it for the model browser
    /// </summary>
    internal class ExplorerTreeViewItemAutomationPeer : TreeViewItemAutomationPeer
    {
        public ExplorerTreeViewItemAutomationPeer(ExplorerTreeViewItem owner)
            : base(owner)
        {
        }

        /// <summary>
        ///     This is so test framework will be able to query model browser nodes
        /// </summary>
        /// <returns></returns>
        protected override string GetAutomationIdCore()
        {
            ExplorerTreeViewItem owner = Owner as ExplorerTreeViewItem;
            Debug.Assert(owner != null, "Where is the ExplorerTreeViewItem for the automation peer?");

            if (owner != null)
            {
                // sometimes the label is stored as a string "Model2.edmx" or as an ExplorerEFElement
                var elementstr = owner.Header as string;
                if (owner.Header is ExplorerEFElement element)
                {
                    return element.Name;
                }
                else if (!String.IsNullOrEmpty(elementstr))
                {
                    return elementstr;
                }
            }

            // we will use the name for the automation id
            return base.GetNameCore();
        }

        /// <summary>
        ///     Returns back the name of the selected TreeViewItem that accessibility narration will read aloud.
        /// </summary>
        /// <returns></returns>
        protected override string GetNameCore()
        {
            ExplorerTreeViewItem owner = Owner as ExplorerTreeViewItem;
            Debug.Assert(owner != null, "Where is the ExplorerTreeViewItem for the automation peer?");

            if (owner != null)
            {
                // sometimes the label is stored as a string "Model2.edmx" or as an ExplorerEFElement
                var elementstr = owner.Header as string;
                if (owner.Header is ExplorerEFElement element)
                {
                    StringBuilder sb = new StringBuilder();

                    // if the currently selected node is not a 'ghost node' (e.g. Entity Sets), we'll also tack on the name
                    if (element.ModelItem != null)
                    {
                        sb.Append(element.ModelItem.GetType().Name);
                        sb.Append(" ");
                    }

                    sb.Append(element.Name);
                    return sb.ToString();
                }
                else if (!String.IsNullOrEmpty(elementstr))
                {
                    return elementstr;
                }
            }

            return base.GetNameCore();
        }

        /// <summary>
        ///     This is so test framework will be able to determine the IsInSearchResults property of browser nodes
        /// </summary>
        /// <returns></returns>
        protected override string GetItemStatusCore()
        {
            ExplorerTreeViewItem owner = Owner as ExplorerTreeViewItem;
            Debug.Assert(owner != null, "Where is the ExplorerTreeViewItem for the automation peer?");

            if (owner != null)
            {
                if (owner.Header is ExplorerEFElement element)
                {
                    return element.ItemStatus;
                }
            }

            return base.GetItemStatusCore();
        }

        /// <summary>
        ///     Used in a tree context as the narrator traverses up the tree, reading each parent of the currently
        ///     focused node.
        /// </summary>
        /// <returns></returns>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            ExplorerTreeViewItem owner = Owner as ExplorerTreeViewItem;
            Debug.Assert(owner != null, "Where is the ExplorerTreeViewItem for the automation peer?");
            if (owner != null)
            {
                return GetAutomationChildren(owner);
            }
            return base.GetChildrenCore();
        }

        /// <summary>
        ///     Get the immediate children of the current ExplorerTreeViewItem, create automation peers for them, and return them for
        ///     accessibility/automation
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static List<AutomationPeer> GetAutomationChildren(ExplorerTreeViewItem parent)
        {
            List<AutomationPeer> automationChildren = new List<AutomationPeer>();
            foreach (ExplorerEFElement element in parent.Items)
            {
                // get the TreeViewItem from the explorer element.
                if (parent.ItemContainerGenerator.ContainerFromItem(element) is ExplorerTreeViewItem treeViewItem)
                {
                    // create an AutomationPeer for this TreeViewItem (will call OnCreateAutomationPeer) and add that to the list to return
                    var automationChild = CreatePeerForElement(treeViewItem);
                    Debug.Assert(automationChild != null, "Every ExplorerTreeViewItem should have an automation peer");
                    if (automationChild != null)
                    {
                        automationChildren.Add(automationChild);
                    }
                }
            }
            return automationChildren;
        }
    }

}
