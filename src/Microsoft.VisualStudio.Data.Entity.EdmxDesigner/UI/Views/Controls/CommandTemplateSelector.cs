// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.ContextMenu;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.Controls
{

    /// <summary>
    /// Template selector that chooses between button, toggle button, and separator templates
    /// based on the type of the item.
    /// </summary>
    internal class CommandTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template for regular button commands.
        /// </summary>
        public DataTemplate ButtonTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for toggle button commands.
        /// </summary>
        public DataTemplate ToggleTemplate { get; set; }

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

            if (item is MenuCommandDefinition commandDef)
            {
                return commandDef.IsToggle ? ToggleTemplate : ButtonTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }

}
