// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Microsoft.Data.Entity.Design.UI.Views.Explorer
{
    internal static class ExplorerUtility
    {
        internal static ScrollBar FindFirstVerticalScrollBar(Visual element)
        {
            var childScrollBars = GetTypeDescendents(element, typeof(ScrollBar));
            foreach (var childScrollBar in childScrollBars)
            {
                if (childScrollBar is ScrollBar scrollBar
                    && scrollBar.Orientation == Orientation.Vertical)
                {
                    return scrollBar;
                }
            }
            return null;
        }

        internal static IEnumerable<Visual> GetTypeDescendents(Visual element, Type type)
        {
            foreach (var child in GetChildren(element))
            {
                if (child.GetType() == type)
                {
                    yield return child;
                }
            }
            foreach (var child in GetChildren(element))
            {
                foreach (var descendentOfType in GetTypeDescendents(child, type))
                {
                    yield return descendentOfType;
                }
            }
        }

        internal static IEnumerable<Visual> GetChildren(Visual element)
        {
            var count = VisualTreeHelper.GetChildrenCount(element);
            for (var i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(element, i) is Visual child)
                {
                    yield return child;
                }
            }
        }

        internal static T FindLogicalAncestorOfType<T>(FrameworkElement element) where T : FrameworkElement
        {
            DependencyObject e = element;
            while (e != null)
            {
                if (e is T returnValue)
                {
                    return (T)e;
                }
                e = LogicalTreeHelper.GetParent(e);
            }
            return null;
        }

        internal static T FindVisualAncestorOfType<T>(FrameworkElement element) where T : FrameworkElement
        {
            DependencyObject e = element;
            while (e != null)
            {
                if (e is T returnValue)
                {
                    return (T)e;
                }
                e = VisualTreeHelper.GetParent(e);
            }
            return null;
        }

        internal static FrameworkElement GetTreeViewItemPartHeader(TreeViewItem treeViewItem)
        {
            if (treeViewItem != null)
            {
                if (VisualTreeHelper.GetChild(treeViewItem, 0) is Grid grid)
                {
                    if (VisualTreeHelper.GetChild(grid, 0) is Border border
                        && border.Name == "PART_Header")
                    {
                        StackPanel panel = VisualTreeHelper.GetChild(border, 0) as StackPanel;
                        return panel;
                    }
                }
            }

            Debug.Assert(false, "Should have returned PART_Header's child panel");
            return null;
        }
    }
}
