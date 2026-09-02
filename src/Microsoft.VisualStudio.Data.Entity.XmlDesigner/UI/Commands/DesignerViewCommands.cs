// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Windows.Input;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.Commands
{
    internal class DesignerViewCommands
    {
        public static readonly RoutedUICommand ChangeCenter =
            new RoutedUICommand(XmlDesignerResources.DesignerViewCommandsText, "ChangeCenter", typeof(DesignerViewCommands));
    }
}
