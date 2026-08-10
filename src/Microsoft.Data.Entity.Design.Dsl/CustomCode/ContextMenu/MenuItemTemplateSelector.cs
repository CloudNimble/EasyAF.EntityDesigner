// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu
{
    /// <summary>
    /// Selects the appropriate DataTemplate for menu items based on their type.
    /// </summary>
    internal class MenuItemTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template for command items.
        /// </summary>
        public DataTemplate CommandTemplate { get; set; }

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

            if (item is MenuCommandDefinition)
            {
                return CommandTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
