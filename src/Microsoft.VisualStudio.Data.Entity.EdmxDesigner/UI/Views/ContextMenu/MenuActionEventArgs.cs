// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.ContextMenu
{

    /// <summary>
    /// Event arguments for menu action events.
    /// </summary>
    internal class MenuActionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the ID of the command that was executed.
        /// </summary>
        public string ActionName { get; }

        public MenuActionEventArgs(string actionName)
        {
            ActionName = actionName;
        }
    }

}
