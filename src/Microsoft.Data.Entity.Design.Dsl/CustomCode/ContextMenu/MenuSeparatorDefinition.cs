// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.ContextMenu
{
    /// <summary>
    /// Represents a separator in the menu items list.
    /// Used as a marker in the MenuItems collection to render a horizontal separator.
    /// </summary>
    public class MenuSeparatorDefinition
    {
        /// <summary>
        /// Gets a singleton instance of the separator definition.
        /// </summary>
        public static MenuSeparatorDefinition Instance { get; } = new MenuSeparatorDefinition();

        private MenuSeparatorDefinition()
        {
        }
    }
}
